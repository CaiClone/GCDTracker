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
using System.Collections.Specialized;
using System.Reflection.PortableExecutable;


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

    public class BarDecisionHelper {
        private static BarDecisionHelper instance;
        public bool Queue_VerticalBar { get; private set; }
        public bool Queue_Triangle { get; private set; }
        public bool SlideStart_VerticalBar { get; private set; }
        public bool SlideEnd_VerticalBar { get; private set; }
        public bool SlideStart_LeftTri { get; private set; }
        public bool SlideStart_RightTri { get; private set; }
        public bool SlideEnd_RightTri { get; private set; }
        public bool Slide_Background { get; private set; }
        public float Queue_Lock_Start { get; private set; }
        public float Slide_Bar_Start { get; private set; }
        public float Slide_Bar_End { get; private set; }
        private readonly Dictionary<string, bool> triggeredAlerts = [];
        private float previousPos = 1f;
        static readonly float epsilon = 0.02f;
        
        private BarDecisionHelper() {
            triggeredAlerts = [];
         }
        public static BarDecisionHelper Instance {
            get {
                instance ??= new BarDecisionHelper();
                return instance;
            }
        }
        public enum BarState {
            GCDOnly,
            ShortCast,
            LongCast,
            NonAbilityCast,
            NoSlideAbility,
            Idle
        }
        public BarState currentState;

        public void Update(BarInfo bar, Configuration conf, GCDHelper helper, ActionType actionType, ObjectKind objectKind) {                
            if (bar.CurrentPos > (epsilon / bar.TotalBarTime) && bar.CurrentPos < previousPos - epsilon) {
                // Reset
                previousPos = 0f;
                ResetBar(conf);

                // Handle Castbar
                if(bar.IsCastBar){
                    Slide_Bar_Start = bar.GCDTime_SlidecastStart;
                    Slide_Bar_End = conf.SlideCastFullBar ? 1f : bar.GCDTotal_SlidecastEnd;
                    if (bar.IsNonAbility) {
                        Queue_Lock_Start = 0f;
                        Queue_VerticalBar = false;
                        Queue_Triangle = false;
                        Slide_Bar_End = 1f;
                        currentState = objectKind switch
                        {
                            ObjectKind.EventObj 
                            or ObjectKind.EventNpc
                            or ObjectKind.Aetheryte => BarState.NoSlideAbility,
                            _ => actionType switch
                            {
                                ActionType.Mount => BarState.NoSlideAbility,
                                _ => BarState.NonAbilityCast,
                            }
                        };
                    }
                    else if (bar.IsShortCast) {
                        Queue_Lock_Start = bar.QueueLockStart;
                        if (Math.Abs(Slide_Bar_End - Queue_Lock_Start) < epsilon)
                            Slide_Bar_End = Queue_Lock_Start;
                        currentState = BarState.ShortCast;
                    }
                    else if (!bar.IsShortCast) {
                        Queue_Lock_Start = bar.QueueLockStart;
                        currentState = BarState.LongCast;
                    }
                }
                // Handle GCDBar
                else if (!bar.IsCastBar && !bar.IsShortCast) {
                    Queue_Lock_Start = bar.QueueLockStart;
                    currentState = BarState.GCDOnly;
                }
            }

            // Idle State
            else if (!helper.isRunning)
                currentState = BarState.Idle;

            previousPos = Math.Max(previousPos, bar.CurrentPos);
            
            switch (currentState) {
                case BarState.GCDOnly:
                    if (conf.QueueLockEnabled)
                        HandleGCDOnly(bar, conf);
                    break;

                case BarState.NonAbilityCast:
                    if (conf.SlideCastEnabled)
                        HandleNonAbilityCast(bar, conf);
                    break;

                case BarState.NoSlideAbility:
                    if (conf.SlideCastEnabled)
                        HandleMount();
                    break;

                case BarState.ShortCast:
                    if (conf.SlideCastEnabled)
                        HandleCastBarShort(bar, conf);
                    else if (conf.QueueLockEnabled)
                        HandleGCDOnly(bar, conf);
                    break;

                case BarState.LongCast:
                    if (conf.SlideCastEnabled)
                        HandleCastBarLong(bar, conf);
                    else if (conf.QueueLockEnabled)
                        HandleGCDOnly(bar, conf);
                    break;

                default:
                    ResetBar(conf);
                    break;
            }
        }

        private void HandleGCDOnly(BarInfo bar, Configuration conf) {            
            // draw line
            Queue_VerticalBar = true;      

            // draw triangles
            Queue_Triangle = conf.ShowQueuelockTriangles;

            // activate alerts
            AlertCheckerQueue(bar, conf);

            // move lines
            if (conf.BarQueueLockSlide)
                Queue_Lock_Start = Math.Max(Queue_Lock_Start, bar.CurrentPos);
        }

        private void HandleNonAbilityCast(BarInfo bar, Configuration conf) {
            // draw lines
            SlideStart_VerticalBar = true;
            SlideStart_LeftTri = conf.ShowSlidecastTriangles && conf.ShowTrianglesOnHardCasts;
            SlideStart_RightTri = conf.ShowSlidecastTriangles && conf.ShowTrianglesOnHardCasts;

            // move lines
            Slide_Bar_Start = Math.Max(Slide_Bar_Start, bar.CurrentPos);

            // draw slidecast bar
            Slide_Background = conf.SlideCastBackground;      
        }

        private void HandleMount() {
            Queue_Lock_Start = 0f;
            Queue_VerticalBar = false;
            Queue_Triangle = false;

            Slide_Bar_Start = 0f;
            Slide_Bar_End = 0f;
            SlideStart_VerticalBar = false;
            SlideEnd_VerticalBar = false;
            SlideStart_LeftTri = false;
            SlideStart_RightTri = false;
            SlideEnd_RightTri = false;
            Slide_Background = false;
        }

        private void HandleCastBarShort(BarInfo bar, Configuration conf) {            
            // draw lines
            SlideStart_VerticalBar = true;
            SlideEnd_VerticalBar = !conf.SlideCastFullBar;

            // draw triangles
            SlideStart_LeftTri = conf.ShowSlidecastTriangles;
            SlideStart_RightTri = conf.ShowSlidecastTriangles && conf.SlideCastFullBar;
            SlideEnd_RightTri = conf.ShowSlidecastTriangles && !conf.SlideCastFullBar;

            // invoke Queuelock
            if (conf.QueueLockEnabled)
                HandleGCDOnly(bar, conf);

            // activate alerts
            AlertCheckerSlide(bar, conf);

            // move lines
            Slide_Bar_Start = Math.Max(Slide_Bar_Start, Math.Min(bar.CurrentPos, Queue_Lock_Start));
            Slide_Bar_End = Math.Max(Slide_Bar_End, Math.Min(bar.CurrentPos, Queue_Lock_Start));

            // draw slidecast bar
            Slide_Background = conf.SlideCastBackground;
        }

        private void HandleCastBarLong(BarInfo bar, Configuration conf) {          
            // draw line
            SlideStart_VerticalBar = true;
            
            // draw triangles
            SlideStart_LeftTri = conf.ShowSlidecastTriangles && conf.ShowTrianglesOnHardCasts;
            SlideStart_RightTri = conf.ShowSlidecastTriangles && conf.ShowTrianglesOnHardCasts;

            // invoke Queuelock
            if (conf.QueueLockEnabled)
                HandleGCDOnly(bar, conf);

            // activate alerts
            AlertCheckerSlide(bar, conf);

            // move lines
            Slide_Bar_Start = Math.Max(Slide_Bar_Start, bar.CurrentPos);
            Queue_Lock_Start = Math.Max(Queue_Lock_Start, Math.Min(bar.CurrentPos, Slide_Bar_Start));

            // draw slidecast bar
            Slide_Background = conf.SlideCastBackground;                
        }

        private void ResetBar(Configuration conf) {
            Queue_Lock_Start = (conf.QueueLockEnabled && conf.BarQueueLockWhenIdle)
                ? 0.8f
                : 0f;
            Queue_VerticalBar = conf.QueueLockEnabled && conf.BarQueueLockWhenIdle;
            Queue_Triangle = Queue_VerticalBar && conf.ShowQueuelockTriangles;

            Slide_Bar_Start = 0f;
            Slide_Bar_End = 0f;
            SlideStart_VerticalBar = false;
            SlideEnd_VerticalBar = false;
            SlideStart_LeftTri = false;
            SlideStart_RightTri = false;
            SlideEnd_RightTri = false;
            Slide_Background = false;
            triggeredAlerts.Clear();
        }

        private bool CheckAlert(EventType type, EventCause cause) {
            string key = $"{type}-{cause}";
            return triggeredAlerts.ContainsKey(key) && triggeredAlerts[key];
        }

        private void MarkAlert(EventType type, EventCause cause) {
            string key = $"{type}-{cause}";
            triggeredAlerts[key] = true;
        }

        private void AlertCheckerSlide(BarInfo bar, Configuration conf){
            var notify = AlertManager.Instance;
            if (bar.CurrentPos >= Slide_Bar_Start - 0.025f && bar.CurrentPos > 0.2f) {
                if (conf.SlideCastEnabled) {
                    if (conf.pulseBarColorAtSlide && !notify.AlertExists(BarColorPulse, Slidecast) && !CheckAlert(BarColorPulse, Slidecast)) {
                        notify.AddAlert(BarColorPulse, Slidecast, Bar, 0f, 0f, bar.CurrentPos, bar.QueueLockScaleFactor);
                        MarkAlert(BarColorPulse, Slidecast);
                    }

                    if (conf.pulseBarWidthAtSlide && !notify.AlertExists(BarWidthPluse, Slidecast) && !CheckAlert(BarWidthPluse, Slidecast)) {
                        notify.AddAlert(BarWidthPluse, Slidecast, Bar, 0f, 0f, bar.CurrentPos, bar.QueueLockScaleFactor);
                        MarkAlert(BarWidthPluse, Slidecast);
                    }

                    if (conf.pulseBarHeightAtSlide && !notify.AlertExists(BarHeightPulse, Slidecast) && !CheckAlert(BarHeightPulse, Slidecast)) {
                        notify.AddAlert(BarHeightPulse, Slidecast, Bar, 0f, 0f, bar.CurrentPos, bar.QueueLockScaleFactor);
                        MarkAlert(BarHeightPulse, Slidecast);
                    }
                }
            }
        }

        private void AlertCheckerQueue(BarInfo bar, Configuration conf){
            var notify = AlertManager.Instance;
            if (bar.CurrentPos >= Queue_Lock_Start - 0.025f && bar.CurrentPos > 0.2f) {
                if (conf.QueueLockEnabled) {
                    if (conf.pulseBarColorAtQueue && !notify.AlertExists(BarColorPulse, Queuelock) && !CheckAlert(BarColorPulse, Queuelock)) {
                        notify.AddAlert(BarColorPulse, Queuelock, Bar, 0f, 0f, bar.CurrentPos, bar.QueueLockScaleFactor);
                    }

                    if (conf.pulseBarWidthAtQueue && !notify.AlertExists(BarWidthPluse, Queuelock) && !CheckAlert(BarWidthPluse, Queuelock)) {
                        notify.AddAlert(BarWidthPluse, Queuelock, Bar, 0f, 0f, bar.CurrentPos, bar.QueueLockScaleFactor);
                        MarkAlert(BarWidthPluse, Queuelock);
                    }

                    if (conf.pulseBarHeightAtQueue && !notify.AlertExists(BarHeightPulse, Queuelock) && !CheckAlert(BarHeightPulse, Queuelock)) {
                        notify.AddAlert(BarHeightPulse, Queuelock, Bar, 0f, 0f, bar.CurrentPos, bar.QueueLockScaleFactor);
                        MarkAlert(BarHeightPulse, Queuelock);
                    }
                }
            }
        }
    }

    public unsafe class GCDHelper {
        private readonly Configuration conf;
        private readonly AbilityManager abilityManager;
        private readonly AlertManager notify;
        public float TotalGCD = 3.5f;
        private DateTime lastGCDEnd = DateTime.Now;

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
        public bool clippedOnThisGCD;
        public bool clippedOnLastGCD;
        public bool abcOnThisGCD;
        public bool abcOnLastGCD;
        public bool isRunning;
        public bool isHardCast;
        private float remainingCastTime;
        public string remainingCastTimeString;
        public string queuedAbilityName = " ";
        public bool shortCastFinished = false;

        public GCDHelper(Configuration conf) {
            this.conf = conf;
            abilityManager = AbilityManager.Instance;
            notify = AlertManager.Instance;
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
            isRunning = (DataStore.Action->ElapsedGCD != DataStore.Action->TotalGCD) || GameState.IsCasting();
            // Detect Teleports for when the carbar is off
            if (conf.ShowOnlyGCDRunning && GameState.IsCastingTeleport()) {
                lastActionTP = true;
            }
            // Reset idleTimer when we start casting
            if (isRunning && idleTimerReset) {
                idleTimerAccum = 0;
                isHardCast = false;
                idleTimerReset = false;
                idleTimerDone = false;
                abcBlocker = false;
                lastActionTP = false;
                GCDTimeoutBuffer = (int)(1000 * conf.GCDTimeout);
            }
            if (!isRunning && !idleTimerDone) {
                idleTimerAccum += framework.UpdateDelta.Milliseconds;
                idleTimerReset = true;
            }
            // Handle caster tax
            if (!isHardCast && GameState.IsCasting() && DataStore.Action->TotalCastTime - 0.1f >= DataStore.Action->TotalGCD)
                isHardCast = true;
            checkABC = !abcBlocker && (idleTimerAccum >= (isHardCast ? (conf.abcDelay + 120) : conf.abcDelay));
            // Reset state after the GCDTimeout
            if (idleTimerAccum >= GCDTimeoutBuffer) {
                checkABC = false;
                abcOnLastGCD = false;
                abcOnThisGCD = false;
                checkClip = false;
                clippedOnLastGCD = false;
                clippedOnThisGCD = false;
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
            clippedOnThisGCD = lastClipDelta > 0.01f;
            return clippedOnThisGCD;
        }

        public bool ShouldStartABC() {
            abcBlocker = true;
            // compare cached target object ID at the time of action use to the current target object ID
            return DataStore.ClientState?.LocalPlayer?.TargetObjectId == targetBuffer;
        }

        public void FlagAlerts(PluginUI ui){
            bool inCombat = DataStore.Condition?[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat] ?? false;
            if(conf.ClipAlertEnabled && (!conf.HideAlertsOutOfCombat || inCombat)){
                if (checkClip && ShouldStartClip()) {
                    ui.StartAlert(true, lastClipDelta);
                    lastClipDelta = 0;
                }
            }
            if (conf.abcAlertEnabled && (!conf.HideAlertsOutOfCombat || inCombat)){
                if (!(clippedOnThisGCD || clippedOnLastGCD) && checkABC && !abcBlocker && ShouldStartABC()) {
                    ui.StartAlert(false, 0);
                    abcOnThisGCD = true;
                }
            }
        }

        public void InvokeAlerts(float relx, float rely, EventSource source, PluginUI ui){
            if (conf.ClipAlertEnabled && clippedOnThisGCD)
                notify.AddAlert(FlyOutAlert, Clipped, source, relx, rely, 0f, 0f);
            if (conf.abcAlertEnabled && (abcOnThisGCD || abcOnLastGCD))
                notify.AddAlert(FlyOutAlert, ABC, source, relx, rely, 0f, 0f);
           }

        public Vector4 BackgroundColor(){
            var bg = conf.backCol;
            if (conf.ColorClipEnabled && (clippedOnLastGCD || clippedOnThisGCD))
                bg = conf.clipCol;
            if (conf.ColorABCEnabled && (abcOnLastGCD || abcOnThisGCD))
                bg = conf.abcCol;
            return bg;
        }
        
        public float GetWheelScale(float uiScale) {
            float wheelPos = lastElapsedGCD / TotalGCD;
            if (wheelPos <= 0.78f || !conf.pulseWheelAtQueue)
                return uiScale;
            float targetScale = uiScale * 1.6f;

            if (wheelPos < 0.82f) {
                float factor = (wheelPos - 0.78f) / 0.04f;
                return Lerp(uiScale, targetScale, factor);
            }
            else if (wheelPos < 0.86f) {
                return targetScale;
            }
            else if (wheelPos < 0.9f) {
                float factor = (wheelPos - 0.86f) / 0.04f;
                return Lerp(targetScale, uiScale, factor);
            }
            else {
                return uiScale;
            }
        }

        private float Lerp(float start, float end, float factor) {
            return start + factor * (end - start);
        }

        public bool CheckClip(bool iscast, float ogcd, float anlock, float gcdTotal, float gcdTime) =>
            !iscast && !isHardCast && DateTime.Now > lastGCDEnd + TimeSpan.FromMilliseconds(50)  &&
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
            clippedOnLastGCD = clippedOnThisGCD;
            clippedOnThisGCD = false;
            abcOnLastGCD = abcOnThisGCD;
            abcOnThisGCD = false;
            shortCastFinished = false;
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
    }
}