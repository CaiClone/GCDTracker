﻿//#define debug
using Dalamud.Game;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using GCDTracker.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tests")]
namespace GCDTracker {
    public record AbilityTiming(float AnimationLock, bool IsCasted);

    public unsafe class GCDWheel {
        public Dictionary<float, AbilityTiming> ogcds = [];
        public float TotalGCD;
        public int idleTimer;
        public bool lastActionTP;
        private DateTime lastGCDEnd;
        private float lastElapsedGCD;
        private float lastClipDelta;
        private ulong targetBuffer;
        private bool lastActionCast;
        private bool clippedGCD;
        private bool checkClip;
        private bool abcOnThisGCD;
        private bool abcOnLastGCD;
        private bool isRunning;
        private bool isHardCast;
        
        #if debug
        private DateTime isRunEnd;
        private int maxIdleTimer;
        private string debugtext;
        private string debugtext2;
        private string debugtext3;
        private string debugtext4;
        #endif

        public GCDWheel() {
            TotalGCD = 3.5f;
            lastGCDEnd = DateTime.Now;
            lastActionCast = false;
            lastClipDelta = 0f;
            clippedGCD = false;
            checkClip = false;
            abcOnThisGCD = false;
            abcOnLastGCD = false;
            targetBuffer = 1;
        }

        public void OnActionUse(byte ret, ActionManager* actionManager, ActionType actionType, uint actionID, ulong targetedActorID, uint param, uint useType, int pvp) {
            var act = DataStore.Action;
            var isWeaponSkill = HelperMethods.IsWeaponSkill(actionType, actionID);
            var addingToQueue = HelperMethods.IsAddingToQueue(isWeaponSkill, act) && useType != 1;
            var executingQueued = act->InQueue && !addingToQueue;
            if (ret != 1) {
                if (executingQueued && Math.Abs(act->ElapsedCastTime-act->TotalCastTime)<0.0001f && isWeaponSkill)
                    ogcds.Clear();
                return;
            }
            //check to make sure that the player is targeting something, so that if they are spamming an action
            //button after the mob dies it won't update the targetBuffer and trigger an ABC
            if (DataStore.ClientState.LocalPlayer?.TargetObject != null)
                targetBuffer = DataStore.ClientState.LocalPlayer.TargetObjectId;
            if (addingToQueue) {
                AddToQueue(act, isWeaponSkill);
            } else {
                if (isWeaponSkill) {
                    EndCurrentGCD(TotalGCD);
                    //Store GCD in a variable in order to cache it when it goes back to 0
                    TotalGCD = act->TotalGCD;
                    AddWeaponSkill(act);
                } else if (!executingQueued) {
                    ogcds[act->ElapsedGCD] = new(act->AnimationLock, false);
                }
            }
        }

        private void AddToQueue(Data.Action* act, bool isWeaponSkill) {
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
            ogcds[timings.Max()] = new(0.64f, false);
        }

        private void AddWeaponSkill(Data.Action* act) {
            if (act->IsCast) {
                lastActionCast = true;
                ogcds[0f] = new(0.1f, false);
                ogcds[act->TotalCastTime] = new(0.1f, true);
            } else {
                ogcds[0f] = new(act->AnimationLock, false);
            }
        }

        public void Update(IFramework framework, Configuration conf) {
            if (DataStore.ClientState.LocalPlayer == null)
                return;
            CleanFailedOGCDs();
            GCDTimeoutHelper(conf);
            if (lastActionCast && !HelperMethods.IsCasting())
                HandleCancelCast();
            else if (DataStore.Action->ElapsedGCD < lastElapsedGCD)
                EndCurrentGCD(lastElapsedGCD);
            else if (DataStore.Action->ElapsedGCD < 0.0001f)
                SlideGCDs((float)(framework.UpdateDelta.TotalMilliseconds * 0.001), false);
            lastElapsedGCD = DataStore.Action->ElapsedGCD;
            
            #if debug
                if (isRunning)
                    isRunEnd = System.DateTime.Now;
                if (!isRunning)
                    maxIdleTimer = idleTimer;
                if (isHardCast)
                    debugtext = "IHC_TRANSITION:" + string.Format("{0:D2}", idleTimer) ;
                debugtext3 = " MAXIDLE:" + string.Format("{0:D3}", maxIdleTimer);
            #endif
        
        }

        private void CleanFailedOGCDs() {
            if (DataStore.Action->AnimationLock == 0 && ogcds.Count > 0) {
                ogcds = ogcds
                    .Where(x => x.Key > DataStore.Action->ElapsedGCD || x.Key + x.Value.AnimationLock < DataStore.Action->ElapsedGCD)
                    .ToDictionary(x => x.Key, x => x.Value);
            }
        }

        private void GCDTimeoutHelper(Configuration conf){
            //create isRunning bool and use it to create an idle "timer"
            isRunning = (DataStore.Action->ElapsedGCD != DataStore.Action->TotalGCD) || HelperMethods.IsCasting();
            if (!isRunning && idleTimer < 25 * conf.GCDTimeout) idleTimer++;

            //FFXIV seems to apply a 0.1s animation lock at the end of every spell.  For instant cast spells,
            //or spells where the GCD is greater than the cast time, this "caster tax" is applied during the
            //GCD and doesn't matter for our purposes.  However, when the cast time is longer than the GCD,
            //there is a 0.1s delay before the next action begins.  A 2.8s ability effectively takes 2.9s.
            //This logic allows us to delay the ABC alert by 11 iterations so hard-casted abilites don't 
            //always trigger an ABC. (11 + 1 (min conf setting) = 12 iterations = ~0.1s [pc dependent])
            if (!isHardCast && HelperMethods.IsCasting() && DataStore.Action->TotalCastTime - 0.1f >= DataStore.Action->TotalGCD)
                isHardCast = true;
            if (isHardCast && idleTimer > (conf.abcDelayMul + 11))
                isHardCast = false;

            //reset state for the wheel/bar background after the GCDTimeout
            if (idleTimer == 25 * conf.GCDTimeout){
                clippedGCD = false;
                checkClip = false;
                abcOnLastGCD = false;
                abcOnThisGCD = false;
                lastActionTP = false;
            }
            //reset idleTimer when we aren't casting.
            if (isRunning)
                idleTimer = 0;
        }

        private void HandleCancelCast() {
            lastActionCast = false;
            EndCurrentGCD(DataStore.Action->TotalCastTime);
        }

        /// <summary>
        /// This function slides all the GCDs forward by a delta and deletes the ones that reach 0
        /// </summary>
        internal void SlideGCDs(float delta, bool isOver) {
            if (delta <= 0) return; //avoid problem with float precision
            var ogcdsNew = new Dictionary<float, AbilityTiming>();
            foreach (var (k, (v,vt)) in ogcds) {
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
            ogcds = ogcdsNew;
        }

        private void FlagAlerts(PluginUI ui, Configuration conf){
            bool inCombat = DataStore.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat];
            if(conf.clipAlertEnabled && (!conf.HideAlertsOutOfCombat || inCombat)){
                if (checkClip && ShouldStartClip()) {
                    ui.StartAlert(true, lastClipDelta);
                    lastClipDelta = 0;
                }
            }
            bool ShouldStartClip() {
                checkClip = false;
                clippedGCD = lastClipDelta > 0.01f;
                return clippedGCD;
            }
            if (conf.abcAlertEnabled && (!conf.HideAlertsOutOfCombat || inCombat)){
                if (!clippedGCD && ShowABCAlert()) {
                    ui.StartAlert(false, 0);
                    abcOnThisGCD = true;
                }
            }
            bool ShowABCAlert() {

                #if debug
                    debugtext4 = "cachedID:" + targetBuffer.ToString() + " realtimeID:" + DataStore.ClientState.LocalPlayer.TargetObjectId.ToString();
                #endif

                // compare cached target object ID at the time of action use to the current target object ID
                if (DataStore.ClientState.LocalPlayer.TargetObjectId == targetBuffer)
                    // Flag for alert
                    if (!isHardCast && idleTimer == conf.abcDelayMul) {
                        
                        #if debug
                            debugtext2 = "NC" + idleTimer.ToString("D2") + " " + string.Format("{0:0.000}", (float)(DateTime.Now - isRunEnd).TotalSeconds);
                        #endif
                        
                        return true;
                    }
                    if (isHardCast && idleTimer == conf.abcDelayMul + 11) {
                        
                        #if debug
                        debugtext2 = "HC" + idleTimer.ToString("D2")+ " " + string.Format("{0:0.000}", (float)(DateTime.Now - isRunEnd).TotalSeconds);
                        #endif
                        
                        return true;
                    }
                return false;
            }
        }

        private void InvokeAlerts(float relx, float rely, PluginUI ui, Configuration conf){
            if (conf.clipAlertEnabled && clippedGCD)
                ui.DrawAlert(relx, rely, conf.ClipTextSize, conf.ClipTextColor, conf.ClipBackColor, conf.ClipAlertPrecision);
            if (conf.abcAlertEnabled && (abcOnThisGCD || abcOnLastGCD))
                ui.DrawAlert(relx, rely, conf.abcTextSize, conf.abcTextColor, conf.abcBackColor, 3);
           }

        public Vector4 BackgroundColor(Configuration conf){
            var bg = conf.backCol;  
            if (conf.ColorClipEnabled && clippedGCD)  
                bg = conf.clipCol;  
            if (conf.ColorABCEnabled && (abcOnLastGCD || abcOnThisGCD))  
                bg = conf.abcCol;
            return bg;
        }

        public void DrawGCDWheel(PluginUI ui, Configuration conf) {
            float gcdTotal = TotalGCD;
            float gcdTime = lastElapsedGCD;
            if (conf.HideIfTP && HelperMethods.IsTeleport(DataStore.Action->CastId)) {
                lastActionTP = true;
                return;
            }
            if (HelperMethods.IsCasting() && DataStore.Action->ElapsedCastTime >= gcdTotal && !HelperMethods.IsTeleport(DataStore.Action->CastId))
                gcdTime = gcdTotal;
            if (gcdTotal < 0.1f) return;
            FlagAlerts(ui, conf);
            InvokeAlerts(0.5f, 0, ui, conf);
            // Background
            ui.DrawCircSegment(0f, 1f, 6f * ui.Scale, conf.backColBorder); 
            ui.DrawCircSegment(0f, 1f, 3f * ui.Scale, BackgroundColor(conf));
            if (conf.QueueLockEnabled) {
                ui.DrawCircSegment(0.8f, 1, 9f * ui.Scale, conf.backColBorder); 
                ui.DrawCircSegment(0.8f, 1, 6f * ui.Scale, BackgroundColor(conf));
            }
            ui.DrawCircSegment(0f, Math.Min(gcdTime / gcdTotal, 1f), 20f * ui.Scale, conf.frontCol);
            foreach (var (ogcd, (anlock, iscast)) in ogcds) {
                var isClipping = CheckClip(iscast, ogcd, anlock, gcdTotal, gcdTime);
                ui.DrawCircSegment(ogcd / gcdTotal, (ogcd + anlock) / gcdTotal, 21f * ui.Scale, isClipping ? conf.clipCol : conf.anLockCol);
                if (!iscast) ui.DrawCircSegment(ogcd / gcdTotal, (ogcd + 0.04f) / gcdTotal, 23f * ui.Scale, conf.ogcdCol);
            }
        }

        public void DrawGCDBar(PluginUI ui, Configuration conf) {
            float gcdTotal = TotalGCD;
            float gcdTime = lastElapsedGCD;
            float barHeight = ui.w_size.Y * conf.BarHeightRatio;
            float barWidth = ui.w_size.X * conf.BarWidthRatio;
            float borderSize = conf.BarBorderSize;
            float barGCDClipTime = 0;
            Vector2 start = new(ui.w_cent.X - barWidth / 2, ui.w_cent.Y - barHeight / 2);
            Vector2 end = new(ui.w_cent.X + barWidth / 2, ui.w_cent.Y + barHeight / 2);

            #if debug
            ui.DrawDebugText((conf.BarWidthRatio + 1) / 2.1f, -1f, conf.abcTextSize, conf.abcTextColor, conf.abcBackColor, debugtext + " " + debugtext2 + " " + debugtext3 + " " + debugtext4);
            #endif

            if (conf.HideIfTP && HelperMethods.IsTeleport(DataStore.Action->CastId)) {
                lastActionTP = true;
                return;
            }
            if (HelperMethods.IsCasting() && DataStore.Action->ElapsedCastTime >= gcdTotal && !HelperMethods.IsTeleport(DataStore.Action->CastId))
                gcdTime = gcdTotal;
            if (gcdTotal < 0.1f) return;
            FlagAlerts(ui, conf);
            InvokeAlerts((conf.BarWidthRatio + 1) / 2.1f, -0.3f, ui, conf);
            // Background
            ui.DrawBar(0f, 1f, barWidth, barHeight, BackgroundColor(conf));
            ui.DrawBar(0f, Math.Min(gcdTime / gcdTotal, 1f), barWidth, barHeight, conf.frontCol);

            foreach (var (ogcd, (anlock, iscast)) in ogcds) {
                var isClipping = CheckClip(iscast, ogcd, anlock, gcdTotal, gcdTime);
                float ogcdStart = (conf.BarRollGCDs && gcdTotal - ogcd < 0.2f) ? 0 + barGCDClipTime : ogcd;
                float ogcdEnd = ogcdStart + anlock;
                // Ends next GCD
                if (conf.BarRollGCDs && ogcdEnd > gcdTotal) {
                    ogcdEnd = gcdTotal;
                    barGCDClipTime += ogcdStart + anlock - gcdTotal;
                    //prevent red bar when we "clip" a hard-cast ability
                    if (!isHardCast){
                        // Draw the clipped part at the beggining
                        ui.DrawBar(0, barGCDClipTime/gcdTotal, barWidth, barHeight, conf.clipCol);
                    }
                }
                ui.DrawBar(ogcdStart / gcdTotal, ogcdEnd / gcdTotal, barWidth, barHeight, isClipping ? conf.clipCol : conf.anLockCol);
                if (!iscast && (!isClipping || ogcdStart > 0.01f)) {
                    Vector2 clipPos = new(
                        ui.w_cent.X + (ogcdStart / gcdTotal * barWidth) - (barWidth / 2),
                        ui.w_cent.Y - (barHeight / 2) + 1f
                    );
                    ui.DrawRectFilled(clipPos,
                        clipPos + new Vector2(2f*ui.Scale, barHeight-2f),
                        conf.ogcdCol);
                }
            }
            //borders last so they're on top of all elements
            if (conf.QueueLockEnabled) {
                Vector2 queueLock = new(
                    ui.w_cent.X + (0.8f * barWidth) - (barWidth / 2),
                    ui.w_cent.Y - (barHeight / 2) - (borderSize / 2)
                );
                ui.DrawRectFilled(queueLock,
                    queueLock + new Vector2(borderSize, barHeight + (borderSize / 2)),
                    conf.BarBackColBorder);
            }
            if (borderSize > 0) {
                ui.DrawRect(
                    start - new Vector2(borderSize, borderSize)/2,
                    end + new Vector2(borderSize, borderSize)/2,
                    conf.BarBackColBorder, borderSize);
            }
        }

        private bool CheckClip(bool iscast, float ogcd, float anlock, float gcdTotal, float gcdTime) =>
            !iscast && !isHardCast && DateTime.Now > lastGCDEnd + TimeSpan.FromMilliseconds(50)  &&
            (
                (ogcd < (gcdTotal - 0.05f) && ogcd + anlock > gcdTotal) // You will clip next GCD
                || (gcdTime < 0.001f && ogcd < 0.001f && (anlock > (lastActionCast? 0.125:0.025))) // anlock when no gcdRolling nor CastEndAnimation
            );
        
        private void EndCurrentGCD(float GCDtime) {
            SlideGCDs(GCDtime, true);
            if (lastElapsedGCD > 0 && !isHardCast) checkClip = true;
            lastElapsedGCD = DataStore.Action->ElapsedGCD;
            lastGCDEnd = DateTime.Now;
            //I'm sure there's a better way to accomplish this
            abcOnLastGCD = abcOnThisGCD;
            abcOnThisGCD = false;
        }

        public void UpdateAnlock(float oldLock, float newLock) {
            if (oldLock == newLock) return; //Ignore autoattacks
            if (ogcds.Count == 0) return;
            if (oldLock == 0) { //End of cast
                lastActionCast = false;
                return;
            }
            var ctime = DataStore.Action->ElapsedGCD;

            var items = ogcds.Where(x => x.Key <= ctime && ctime < x.Key + x.Value.AnimationLock);
            if (!items.Any()) return;
            var item = items.First(); //Should always be one

            ogcds[item.Key] = new(ctime - item.Key + newLock, item.Value.IsCasted);
            var diff = newLock - oldLock;
            var toSlide = ogcds.Where(x => x.Key > ctime).ToList();
            foreach (var ogcd in toSlide)
                ogcds[ogcd.Key + diff] = ogcd.Value;
            foreach (var ogcd in toSlide)
                ogcds.Remove(ogcd.Key);
        }
    }

}