using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game;
using GCDTracker.Data;
using GCDTracker.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using static GCDTracker.EventType;
using static GCDTracker.EventCause;
using static GCDTracker.EventSource;

[assembly: InternalsVisibleTo("Tests")]
namespace GCDTracker {
    public class AbilityManager {
        public record AbilityTiming(float AnimationLock, bool IsCasted);
        private static AbilityManager instance;
        public Dictionary<float, AbilityTiming> ogcds { get; private set; }
        
        private AbilityManager() {
            ogcds = [];
        }
        public static AbilityManager Instance {
            get {
                instance ??= new AbilityManager();
                return instance;
            }
        }

        public void UpdateOGCDs(Dictionary<float, AbilityTiming> newOgcds) {
            ogcds = newOgcds;
        }
    }

    public unsafe class GCDHelper {
        private readonly Configuration conf;
        private readonly AbilityManager abilityManager;
        private readonly AlertManager notify;
        public float TotalGCD = 3.5f;
        private DateTime lastGCDEnd = DateTime.Now;
        private readonly Dictionary<string, bool> helperAlerts = [];

        public float lastElapsedGCD;
        private float lastClipDelta;
        private ulong targetBuffer = 1;

        public int idleTimerAccum;
        public int GCDTimeoutBuffer;
        public bool abcBlocker;
        public bool lastActionTP;

        private bool idleTimerReset = true;
        private bool idleTimerDone;
        private bool lastActionCast;

        private bool checkClip;
        private bool checkABC;
        public bool ClippedOnThisGCD;
        public bool ClippedOnLastGCD;
        public bool ABCOnThisGCD;
        public bool ABCOnLastGCD;
        public bool IsRunning;
        public bool IsHardCast;
        private float remainingCastTime;
        public string remainingCastTimeString;
        public string queuedAbilityName = " ";

        public GCDHelper(Configuration conf) {
            this.conf = conf;
            abilityManager = AbilityManager.Instance;
            notify = AlertManager.Instance;
            helperAlerts = [];
        }

        public void OnActionUse(byte ret, ActionManager* actionManager, ActionType actionType, uint actionID, ulong targetedActorID, uint param, uint useType, int pvp) {
            var act = DataStore.Action;
            var isWeaponSkill = HelperMethods.IsWeaponSkill(actionType, actionID);
            var addingToQueue = HelperMethods.IsAddingToQueue(isWeaponSkill, act) && useType != 1;
            var executingQueued = act->InQueue && !addingToQueue;
            if (ret != 1) {
                if (executingQueued && Math.Abs(act->ElapsedCastTime-act->TotalCastTime) < 0.0001f && isWeaponSkill)
                    abilityManager.ogcds.Clear();
                return;
            }
            //check to make sure that the player is targeting something, so that if they are spamming an action
            //button after the mob dies it won't update the targetBuffer and trigger an ABC
            if (DataStore.ClientState.LocalPlayer?.TargetObject != null)
                targetBuffer = DataStore.ClientState.LocalPlayer.TargetObjectId;

            if (addingToQueue) {
                AddToQueue(act, isWeaponSkill);
                queuedAbilityName = HelperMethods.GetAbilityName(actionID, actionType);
            } else {
                queuedAbilityName = " ";
                
                // this triggers for me whenever I press a button on my bard, reguardless of 
                // the outcome of the action.  If it was too early to queue a skill at all,
                // and the action fails, this still triggers and clears all of the animation
                // locks from my AbilityManager dictionary resulting in an empty bar.
                // is this intended or a bug?
                
                if (isWeaponSkill) {
                    EndCurrentGCD(TotalGCD);
                    //Store GCD in a variable in order to cache it when it goes back to 0
                    TotalGCD = act->TotalGCD;
                    AddWeaponSkill(act);
                } else if (!executingQueued) {
                    abilityManager.ogcds[act->ElapsedGCD] = new(act->AnimationLock, false);
                }
            }
        }

        public void AddToQueue(Data.Action* act, bool isWeaponSkill) {
            var timings = new List<float>() {
                isWeaponSkill ? act->TotalGCD : 0, // Weapon skills
            };
            if (!act->IsCast) {
                // Add OGCDs
                timings.Add(act->ElapsedGCD + act->AnimationLock);
            } else if (act->ElapsedCastTime < act->TotalGCD) {
                // Add Casts
                timings.Add(act->TotalCastTime + 0.1f);
            } else {
                // Add Casts after 1 whole GCD of casting
                timings.Add(act->TotalCastTime - act->ElapsedCastTime + 0.1f);
            }
            abilityManager.ogcds[timings.Max()] = new(0.64f, false);
        }

        public void AddWeaponSkill(Data.Action* act) {
            if (act->IsCast) {
                lastActionCast = true;
                abilityManager.ogcds[0f] = new(0.1f, false);
                abilityManager.ogcds[act->TotalCastTime] = new(0.1f, true);
            } else {
                abilityManager.ogcds[0f] = new(act->AnimationLock, false);
            }
        }

        public void Update(IFramework framework) {
            if (DataStore.ClientState.LocalPlayer == null)
                return;
            CleanFailedOGCDs();
            GCDTimeoutHelper(framework);
            remainingCastTime = DataStore.Action->TotalCastTime - DataStore.Action->ElapsedCastTime;
            remainingCastTimeString = remainingCastTime == 0 ? "" : remainingCastTime.ToString("F1");
            if (lastActionCast && !GameState.IsCasting())
                HandleCancelCast();
            else if (DataStore.Action->ElapsedGCD < lastElapsedGCD)
                EndCurrentGCD(lastElapsedGCD);
            else if (DataStore.Action->ElapsedGCD < 0.0001f)
                SlideGCDs((float)(framework.UpdateDelta.TotalMilliseconds * 0.001), false);
            lastElapsedGCD = DataStore.Action->ElapsedGCD;
        }

        public void CleanFailedOGCDs() {
            if (DataStore.Action->AnimationLock == 0 && abilityManager.ogcds.Count > 0) {
                var ogcdsNew = abilityManager.ogcds
                    .Where(x => x.Key > DataStore.Action->ElapsedGCD || x.Key + x.Value.AnimationLock < DataStore.Action->ElapsedGCD)
                    .ToDictionary(x => x.Key, x => x.Value);

                abilityManager.UpdateOGCDs(ogcdsNew);
            }
        }

        public void GCDTimeoutHelper(IFramework framework) {
            // Determine if we are running
            IsRunning = (DataStore.Action->ElapsedGCD != DataStore.Action->TotalGCD) || GameState.IsCasting();
            // Detect Teleports for when the carbar is off
            if (conf.ShowOnlyGCDRunning && GameState.IsCastingTeleport()) {
                lastActionTP = true;
            }
            // Reset idleTimer when we start casting
            if (IsRunning && idleTimerReset) {
                idleTimerAccum = 0;
                IsHardCast = false;
                idleTimerReset = false;
                idleTimerDone = false;
                abcBlocker = false;
                lastActionTP = false;
                GCDTimeoutBuffer = (int)(1000 * conf.GCDTimeout);
                helperAlerts.Clear();
            }
            if (!IsRunning && !idleTimerDone) {
                idleTimerAccum += framework.UpdateDelta.Milliseconds;
                idleTimerReset = true;
            }
            // Handle caster tax
            if (!IsHardCast && GameState.IsCasting() && DataStore.Action->TotalCastTime - 0.1f >= DataStore.Action->TotalGCD)
                IsHardCast = true;
            checkABC = !abcBlocker && (idleTimerAccum >= (IsHardCast ? (conf.abcDelay + 120) : conf.abcDelay));
            // Reset state after the GCDTimeout
            if (idleTimerAccum >= GCDTimeoutBuffer) {
                checkABC = false;
                ABCOnLastGCD = false;
                ABCOnThisGCD = false;
                checkClip = false;
                ClippedOnLastGCD = false;
                ClippedOnThisGCD = false;
                lastActionTP = false;
                idleTimerDone = true;
            }
        }

        public void HandleCancelCast() {
            lastActionCast = false;
            EndCurrentGCD(DataStore.Action->TotalCastTime);
        }

        /// <summary>
        /// This function slides all the GCDs forward by a delta and deletes the ones that reach 0
        /// </summary>
        internal void SlideGCDs(float delta, bool isOver) {
            if (delta <= 0) return; //avoid problem with float precision
            var ogcdsNew = new Dictionary<float, AbilityManager.AbilityTiming>();
            foreach (var (k, (v,vt)) in abilityManager.ogcds) {
                if (k < -0.1) { } //remove from dictionary
                else if (k < delta && v > delta) {
                    ogcdsNew[k] = new(v - delta, vt);
                } else if (k > delta) {
                    ogcdsNew[k - delta] = new(v, vt);
                } else if (isOver && k + v > TotalGCD) {
                    ogcdsNew[0] = new(k + v - delta, vt);
                    if (k < delta - 0.02f) // Ignore things that are queued or queued + cast end animation lock
                        lastClipDelta = k + v - delta;
                }
            }
            abilityManager.UpdateOGCDs(ogcdsNew);
        }

        public bool ShouldStartClip() {
            checkClip = false;
            ClippedOnThisGCD = lastClipDelta > 0.01f;
            return ClippedOnThisGCD;
        }

        public bool ShouldStartABC() {
            abcBlocker = true;
            // compare cached target object ID at the time of action use to the current target object ID
            return DataStore.ClientState?.LocalPlayer?.TargetObjectId == targetBuffer;
        }

        public void MiscEventChecker(){
            bool inCombat = DataStore.Condition?[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat] ?? false;

            // Check and flag Clip Alert
            if(conf.ClipAlertEnabled && (!conf.HideAlertsOutOfCombat || inCombat)){
                if (checkClip && ShouldStartClip()) {
                    notify.ActivateAlert(FlyOutAlert, Clip, Bar, lastClipDelta);
                    MarkAlert(FlyOutAlert, Clip);
                    lastClipDelta = 0;
                }
            }

            // Check and flag ABC Alert
            var clipInQueue = CheckAlert(FlyOutAlert, Clip);
            if (conf.abcAlertEnabled && (!conf.HideAlertsOutOfCombat || inCombat) && !clipInQueue){
                if (!(ClippedOnThisGCD || ClippedOnLastGCD) && checkABC && !abcBlocker && ShouldStartABC()) {
                    notify.ActivateAlert(FlyOutAlert, ABC, Bar);
                    MarkAlert(FlyOutAlert, ABC);
                    ABCOnThisGCD = true;
                }
            }
        }

        public void WheelCheckQueueEvent(Configuration conf, float wheelPos) {
            if (wheelPos >= 0.8f - 0.025f && wheelPos > 0.2f) {
                if (conf.QueueLockEnabled) {
                    if (conf.pulseWheelAtQueue && !CheckAlert(WheelPulse, Queuelock)) {
                        notify.ActivateAlert(WheelPulse, Queuelock, Wheel);
                        MarkAlert(WheelPulse, Queuelock);
                    }
                }
            }
        }

        public Vector4 BackgroundColor(){
            var bg = conf.backCol;
            if (conf.ColorClipEnabled && (ClippedOnLastGCD || ClippedOnThisGCD))
                bg = conf.clipCol;
            if (conf.ColorABCEnabled && (ABCOnLastGCD || ABCOnThisGCD))
                bg = conf.abcCol;
            return bg;
        }

        public bool CheckClip(bool iscast, float ogcd, float anlock, float gcdTotal, float gcdTime) =>
            !iscast && !IsHardCast && DateTime.Now > lastGCDEnd + TimeSpan.FromMilliseconds(50)  &&
            (
                (ogcd < (gcdTotal - 0.05f) && ogcd + anlock > gcdTotal) // You will clip next GCD
                || (gcdTime < 0.001f && ogcd < 0.001f && (anlock > (lastActionCast? 0.125:0.025))) // anlock when no gcdRolling nor CastEndAnimation
            );

        public void EndCurrentGCD(float GCDtime) {
            SlideGCDs(GCDtime, true);
            if (lastElapsedGCD > 0) checkClip = true;
            lastElapsedGCD = DataStore.Action->ElapsedGCD;
            lastGCDEnd = DateTime.Now;
            //I'm sure there's a better way to accomplish this
            ClippedOnLastGCD = ClippedOnThisGCD;
            ClippedOnThisGCD = false;
            ABCOnLastGCD = ABCOnThisGCD;
            ABCOnThisGCD = false;
        }

        public void UpdateAnlock(float oldLock, float newLock) {
            if (oldLock == newLock) return; //Ignore autoattacks
            if (abilityManager.ogcds.Count == 0) return;
            if (oldLock == 0) { //End of cast
                lastActionCast = false;
                return;
            }
            var ctime = DataStore.Action->ElapsedGCD;

            var items = abilityManager.ogcds.Where(x => x.Key <= ctime && ctime < x.Key + x.Value.AnimationLock);
            if (!items.Any()) return;
            var item = items.First(); //Should always be one

            abilityManager.ogcds[item.Key] = new(ctime - item.Key + newLock, item.Value.IsCasted);
            var diff = newLock - oldLock;
            var toSlide = abilityManager.ogcds.Where(x => x.Key > ctime).ToList();
            foreach (var ogcd in toSlide)
                abilityManager.ogcds[ogcd.Key + diff] = ogcd.Value;
            foreach (var ogcd in toSlide)
                abilityManager.ogcds.Remove(ogcd.Key);
        }

        private bool CheckAlert(EventType type, EventCause cause) {
            string key = $"{type}-{cause}";
            return helperAlerts.ContainsKey(key) && helperAlerts[key];
        }

        private void MarkAlert(EventType type, EventCause cause) {
            string key = $"{type}-{cause}";
            helperAlerts[key] = true;
        }
    }
}