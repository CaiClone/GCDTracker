
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

        private DateTime lastGCDEnd;
        private float lastElapsedGCD;
        private bool lastActionCast;
        private float lastClipDelta;
        private bool clippedGCD;
        private bool checkClip;
        private bool showABCAlert;
        private ulong targetBuffer;
        private ulong hackBuffer;
        public float SecondsSinceGCDEnd =>
            lastElapsedGCD > 0 ? 0 : (float)(DateTime.Now - lastGCDEnd).TotalSeconds;
        public GCDWheel() {
            TotalGCD = 3.5f;
            lastGCDEnd = DateTime.Now;
            lastActionCast = false;
            lastClipDelta = 0f;
            clippedGCD = false;
            checkClip = false;
            showABCAlert = false; 
            targetBuffer = 1;
        }

        public void OnActionUse(byte ret, ActionManager* actionManager, ActionType actionType, uint actionID, ulong targetedActorID, uint param, uint useType, int pvp) {
            var act = DataStore.Action;

            var isWeaponSkill = HelperMethods.IsWeaponSkill(actionType, actionID);
            var addingToQueue = HelperMethods.IsAddingToQueue(isWeaponSkill, act) && useType != 1;
            var executingQueued = act->InQueue && !addingToQueue;
            
            targetBuffer = DataStore.ClientState.LocalPlayer.TargetObjectId;
            if (ret != 1) {
                if (executingQueued && Math.Abs(act->ElapsedCastTime-act->TotalCastTime)<0.0001f && isWeaponSkill)
                    ogcds.Clear();
                return;
            }
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

        public void Update(IFramework framework) {
            if (DataStore.ClientState.LocalPlayer == null)
                return;

            CleanFailedOGCDs();
            if (lastActionCast && !HelperMethods.IsCasting())
                HandleCancelCast();
            else if (DataStore.Action->ElapsedGCD < lastElapsedGCD)
                EndCurrentGCD(lastElapsedGCD);
            else if (DataStore.Action->ElapsedGCD < 0.0001f)
                SlideGCDs((float)(framework.UpdateDelta.TotalMilliseconds * 0.001), false);

            //someone who knows how to code can probably come up with a way better solution to this problem.
            //we cache whatever the targetid was when we called OnActionUse and compare it to whatever it is
            //now.  hackBuffer because I couldn't get conditional to be true without it (something to do
            //with the type cast going on to ulong, probably)
            hackBuffer = DataStore.ClientState.LocalPlayer.TargetObjectId;
            if (hackBuffer == targetBuffer)
                //flag for alert if more than 50ms but less than 100ms have passed with no GCD in queue
                showABCAlert = SecondsSinceGCDEnd >= 0.05f && SecondsSinceGCDEnd < 0.1f;

            lastElapsedGCD = DataStore.Action->ElapsedGCD;
        }

        private void CleanFailedOGCDs() {
            if (DataStore.Action->AnimationLock == 0 && ogcds.Count > 0) {
                ogcds = ogcds
                    .Where(x => x.Key > DataStore.Action->ElapsedGCD || x.Key + x.Value.AnimationLock < DataStore.Action->ElapsedGCD)
                    .ToDictionary(x => x.Key, x => x.Value);
            }
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

        private bool ShouldStartClip() {
            checkClip = false;
            clippedGCD = lastClipDelta > 0.01f;
            return clippedGCD;
        }

        public void DrawGCDWheel(PluginUI ui, Configuration conf) {
            float gcdTotal = TotalGCD;
            float gcdTime = lastElapsedGCD;
            if (HelperMethods.IsCasting() && DataStore.Action->ElapsedCastTime >= gcdTotal && !HelperMethods.IsTeleport(DataStore.Action->CastId))
                gcdTime = gcdTotal;
            if (gcdTotal < 0.1f) return;
            if (checkClip && ShouldStartClip()) {
                ui.StartClip(lastClipDelta);
                lastClipDelta = 0;
            }
            if (showABCAlert && !clippedGCD) {
                ui.StartABC();
                showABCAlert = false;
            }
            if (clippedGCD && lastGCDEnd + TimeSpan.FromSeconds(4) < DateTime.Now)
                clippedGCD = false;

            var backgroundCol = clippedGCD && conf.ColorClipEnabled ? conf.clipCol : conf.backCol;
            // Background
            ui.DrawCircSegment(0f, 1f, 6f * ui.Scale, conf.backColBorder); 
            ui.DrawCircSegment(0f, 1f, 3f * ui.Scale, backgroundCol);
            if (conf.WheelQueueLockEnabled) {
                ui.DrawCircSegment(0.8f, 1, 9f * ui.Scale, conf.backColBorder); 
                ui.DrawCircSegment(0.8f, 1, 6f * ui.Scale, backgroundCol);
            }
            if (conf.ClipAlertEnabled)
                ui.DrawClip(0.5f, 0, conf.ClipTextSize, conf.ClipTextColor, conf.ClipBackColor, conf.ClipAlertPrecision);
            if (conf.abcAlertEnabled)
                ui.DrawABC(0.8f, 0, conf.abcTextSize, conf.abcTextColor, conf.abcBackColor);

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
            if (HelperMethods.IsCasting() && DataStore.Action->ElapsedCastTime >= gcdTotal && !HelperMethods.IsTeleport(DataStore.Action->CastId))
                gcdTime = gcdTotal;
            if (gcdTotal < 0.1f) return;
            if (checkClip && ShouldStartClip()) {
                ui.StartClip(lastClipDelta);
                lastClipDelta = 0;
            }
            if (showABCAlert && !clippedGCD) {
                ui.StartABC();
                showABCAlert = false;
            }
            if (clippedGCD && lastGCDEnd + TimeSpan.FromSeconds(4) < DateTime.Now)
                clippedGCD = false;
                
            var backgroundCol = clippedGCD && conf.BarColorClipEnabled ? conf.BarclipCol : conf.BarBackCol;
            float barHeight = ui.w_size.Y * conf.BarHeightRatio;
            float barWidth = ui.w_size.X * conf.BarWidthRatio;
            float borderSize = conf.BarBorderSize;

            Vector2 start = new(ui.w_cent.X - barWidth / 2, ui.w_cent.Y - barHeight / 2);
            Vector2 end = new(ui.w_cent.X + barWidth / 2, ui.w_cent.Y + barHeight / 2);
            // Background
            ui.DrawBar(0f, 1f, barWidth, barHeight, backgroundCol);
            if (conf.BarClipAlertEnabled)
                ui.DrawClip((conf.BarWidthRatio + 1) / 2.1f, -0.3f, conf.BarClipTextSize, conf.BarClipTextColor, conf.BarClipBackColor, conf.BarClipAlertPrecision);
            if (conf.BarABCAlertEnabled)
                ui.DrawABC(((conf.BarWidthRatio + 1) / 1.9f) - conf.BarWidthRatio, -0.3f, conf.BarABCTextSize, conf.BarABCTextColor, conf.BarABCBackColor);
            ui.DrawBar(0f, Math.Min(gcdTime / gcdTotal, 1f), barWidth, barHeight, conf.BarFrontCol);

            float barGCDClipTime = 0;
            foreach (var (ogcd, (anlock, iscast)) in ogcds) {
                var isClipping = CheckClip(iscast, ogcd, anlock, gcdTotal, gcdTime);
                float ogcdStart = (conf.BarRollGCDs && gcdTotal - ogcd < 0.2f) ? 0 + barGCDClipTime : ogcd;
                float ogcdEnd = ogcdStart + anlock;
                // Ends next GCD
                if (conf.BarRollGCDs && ogcdEnd > gcdTotal) {
                    ogcdEnd = gcdTotal;
                    barGCDClipTime += ogcdStart + anlock - gcdTotal;
                    
                    // Draw the clipped part at the beggining
                    ui.DrawBar(0, barGCDClipTime/gcdTotal, barWidth, barHeight, conf.BarclipCol);
                }
                
                ui.DrawBar(ogcdStart / gcdTotal, ogcdEnd / gcdTotal, barWidth, barHeight, isClipping ? conf.BarclipCol : conf.BarAnLockCol);
                if (!iscast && (!isClipping || ogcdStart > 0.01f)) {
                    Vector2 clipPos = new(
                        ui.w_cent.X + (ogcdStart / gcdTotal * barWidth) - (barWidth / 2),
                        ui.w_cent.Y - (barHeight / 2) + 1f
                    );
                    ui.DrawRectFilled(clipPos,
                        clipPos + new Vector2(2f*ui.Scale, barHeight-2f),
                        conf.BarOgcdCol);
                }
            }
            //borders last so they're on top of all elements
            if (borderSize > 0) {
                ui.DrawRect(
                    start - new Vector2(borderSize, borderSize)/2,
                    end + new Vector2(borderSize, borderSize)/2,
                    conf.BarBackColBorder, borderSize);
            }
            if (conf.BarQueueLockEnabled) {
                Vector2 queueLock = new(
                    ui.w_cent.X + (0.8f * barWidth) - (barWidth / 2),
                    ui.w_cent.Y - (barHeight / 2) - (borderSize / 2)
                );
                ui.DrawRectFilled(queueLock,
                    queueLock + new Vector2(borderSize, barHeight + (borderSize / 2)),
                    conf.BarBackColBorder);
            }
        }

        private bool CheckClip(bool iscast, float ogcd, float anlock, float gcdTotal, float gcdTime) =>
            !iscast && DateTime.Now > lastGCDEnd + TimeSpan.FromMilliseconds(50)  &&
            (
                (ogcd < (gcdTotal - 0.05f) && ogcd + anlock > gcdTotal) // You will clip next GCD
                || (gcdTime < 0.001f && ogcd < 0.001f && (anlock > (lastActionCast? 0.125:0.025))) // anlock when no gcdRolling nor CastEndAnimation
            );
        

        private void EndCurrentGCD(float GCDtime) {
            SlideGCDs(GCDtime, true);
            if (lastElapsedGCD > 0) checkClip = true;
            lastElapsedGCD = DataStore.Action->ElapsedGCD;
            lastGCDEnd = DateTime.Now;
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
