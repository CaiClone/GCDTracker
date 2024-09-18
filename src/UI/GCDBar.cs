using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using GCDTracker.Data;
using GCDTracker.UI.Components;
using GCDTracker.Utils;

namespace GCDTracker.UI {
    public unsafe class GCDBar : IWindow {
        private readonly Configuration conf;
        private readonly GCDHelper helper;
        private readonly AbilityManager abilityManager;    

        private readonly QueueLock queueLock;
        private readonly SlideCast slideCast;

        private string shortCastCachedSpellName;
        private Vector4 bgCache;

        public GCDBar(Configuration conf, GCDHelper helper, AbilityManager abilityManager) {
            this.conf = conf;
            this.helper = helper;
            this.abilityManager = abilityManager;
            queueLock = new(BarInfo.Instance, BarVertices.Instance, conf, BarDecisionHelper.Instance);
            slideCast = new(BarInfo.Instance, BarVertices.Instance, conf, BarDecisionHelper.Instance);
        }

        public void Draw(PluginUI ui) {
            if (conf.CastBarEnabled && GameState.IsCasting())
                DrawCastBar(ui);
            else 
                DrawGCDBar(ui);
        }

        private void DrawGCDBar(PluginUI ui) {
            float gcdTotal = helper.TotalGCD;
            float gcdTime = helper.lastElapsedGCD;

            if (GameState.IsCasting() && DataStore.Action->ElapsedCastTime >= gcdTotal && !GameState.IsCastingTeleport())
                gcdTime = gcdTotal;
            if (gcdTotal < 0.1f) return;
            
            if (!conf.WheelEnabled)
                helper.MiscEventChecker();

            DrawBarElements(
                ui,
                false,
                helper.shortCastFinished,
                false,
                gcdTime / gcdTotal,
                gcdTime,
                gcdTotal, gcdTotal
            );

            // Gonna re-do this, but for now, we flag when we need to carryover from the castbar to the GCDBar
            // and dump all the crap here to draw on top. 
            if (helper.shortCastFinished) {
                string abilityNameOutput = shortCastCachedSpellName;
                if (!string.IsNullOrWhiteSpace(helper.queuedAbilityName) && conf.CastBarShowQueuedSpell)
                    abilityNameOutput += " -> " + helper.queuedAbilityName;
                if (!string.IsNullOrEmpty(abilityNameOutput))
                    DrawBarText(ui, abilityNameOutput);
            }
            if (conf.ShowQueuedSpellNameGCD && !helper.shortCastFinished) {
                if (gcdTime / gcdTotal < 0.8f)
                    helper.queuedAbilityName = " ";
                if (!string.IsNullOrWhiteSpace(helper.queuedAbilityName))
                    DrawBarText(ui, " -> " + helper.queuedAbilityName);
            }
        }

        

        private void DrawBarElements(
            PluginUI ui, 
            bool isCastBar, 
            bool isShortCast,
            bool isNonAbility,
            float castBarCurrentPos, 
            float gcdTime_slidecastStart, 
            float gcdTotal_slidecastEnd,
            float totalBarTime) {
            
            var bar = BarInfo.Instance;
            bar.Update(
                conf,
                ui.w_size.X,
                ui.w_cent.X,
                ui.w_size.Y,
                ui.w_cent.Y,
                castBarCurrentPos,
                gcdTime_slidecastStart, 
                gcdTotal_slidecastEnd,
                totalBarTime,
                conf.triangleSize,
                isCastBar, 
                isShortCast,
                isNonAbility
            );

            var go = BarDecisionHelper.Instance;
            go.Update(
                bar, 
                conf, 
                helper, 
                DataStore.ActionManager->CastActionType, 
                DataStore.ClientState?.LocalPlayer?.TargetObject?.ObjectKind ?? ObjectKind.None
            );

            var notify = GCDEventHandler.Instance;
            notify.Update(bar, conf, ui);

            var bar_v = BarVertices.Instance;
            bar_v.Update(bar, go, notify);
            slideCast.Update(bar_v);
            queueLock.Update(bar_v);

            float barGCDClipTime = 0;
            // in both modes:
            // draw the background
            if (bar.CurrentPos < 0.2f)
                bgCache = helper.BackgroundColor();
            ui.DrawRectFilledNoAA(bar_v.Rect.LB(), bar_v.Rect.RT(), bgCache, conf.BarBgGradMode, conf.BarBgGradientMul);

            // in both modes:
            // draw cast/gcd progress (main) bar
            if(bar.CurrentPos > 0.001f){
                var progressBarColor = notify.ProgressPulseColor;
                ui.DrawRectFilledNoAA(bar_v.Rect.LT(), bar_v.ProgressVertex, progressBarColor, conf.BarGradMode, conf.BarGradientMul);
            }
            // in Castbar mode:
            // draw the slidecast bar
            slideCast.Draw(ui);
            // in GCDBar mode:
            // draw oGCDs and clips

            if (!isCastBar) {
                float gcdTime = gcdTime_slidecastStart;
                float gcdTotal = gcdTotal_slidecastEnd;

                foreach (var (ogcd, (anlock, iscast)) in abilityManager.ogcds) {
                    var isClipping = helper.CheckClip(iscast, ogcd, anlock, gcdTotal, gcdTime);
                    float ogcdStart = (conf.BarRollGCDs && gcdTotal - ogcd < 0.2f) ? 0 + barGCDClipTime : ogcd;
                    float ogcdEnd = ogcdStart + anlock;

                    // Ends next GCD
                    if (conf.BarRollGCDs && ogcdEnd > gcdTotal) {
                        ogcdEnd = gcdTotal;
                        barGCDClipTime += ogcdStart + anlock - gcdTotal;
                        //prevent red bar when we "clip" a hard-cast ability
                        if (!helper.IsHardCast) {
                            // create end vertex
                            Vector2 clipEndVector = new(
                                bar_v.ProgToScreen(barGCDClipTime / gcdTotal),
                                bar_v.Rect.Top
                            );
                            // Draw the clipped part at the beginning
                            ui.DrawRectFilledNoAA(bar_v.Rect.LB(), clipEndVector, conf.clipCol);
                        }
                    }
                    Vector2 oGCDStartVector = new(
                        bar_v.ProgToScreen(ogcdStart / gcdTotal),
                        bar_v.Rect.Top
                    );
                    Vector2 oGCDEndVector = new(
                        bar_v.ProgToScreen(ogcdEnd / gcdTotal),
                        bar_v.Rect.Bottom
                    );

                    if(!helper.shortCastFinished || isClipping) {
                        ui.DrawRectFilledNoAA(oGCDStartVector, oGCDEndVector, isClipping ? conf.clipCol : conf.anLockCol);
                        if (!iscast && (!isClipping || ogcdStart > 0.01f)) {
                            var clipPos = bar_v.ProgToScreen(ogcdStart / gcdTotal);
                            ui.DrawRectFilledNoAA(
                                new Vector2(clipPos, bar_v.Rect.Top + bar_v.BorderWidth),
                                new Vector2(clipPos + 2f * ui.Scale, bar_v.Rect.Bottom - bar_v.BorderWidth),
                                conf.ogcdCol);
                            
                        }
                    }
                }
            }

            //in both modes:
            //draw the queuelock (if enabled)
            queueLock.Draw(ui);

            // in both modes:
            // draw borders
            if (bar.BorderSize > 0) {
                ui.DrawRect(
                    bar_v.Rect.LB() - new Vector2(bar.HalfBorderSize, bar.HalfBorderSize),
                    bar_v.Rect.RT() + new Vector2(bar.HalfBorderSize, bar.HalfBorderSize),
                    conf.backColBorder, bar.BorderSize);
            }
        }
        
        public void DrawCastBar (PluginUI ui) {
            float gcdTotal = DataStore.Action->TotalGCD;
            float castTotal = DataStore.Action->TotalCastTime;
            float castElapsed = DataStore.Action->ElapsedCastTime;
            float castbarProgress = castElapsed / castTotal;
            float castbarEnd = 1f;
            float slidecastStart = Math.Max((castTotal - conf.SlidecastDelay) / castTotal, 0f);
            float slidecastEnd = castbarEnd;
            bool isTeleport = GameState.IsCastingTeleport();
            // handle short casts
            if (gcdTotal > castTotal) {
                castbarEnd = GameState.CastingNonAbility() ? 1f : castTotal / gcdTotal;
                slidecastStart = Math.Max((castTotal - conf.SlidecastDelay) / gcdTotal, 0f);
                slidecastEnd = conf.SlideCastFullBar ? 1f : castbarEnd;
            }

            DrawBarElements(
                ui,
                true,
                gcdTotal > castTotal,
                // Maybe we don't need the gcdTotal < 0.01f anymore?
                GameState.CastingNonAbility() || isTeleport || gcdTotal < 0.01f,
                castbarProgress * castbarEnd,
                slidecastStart,
                slidecastEnd,
                castbarEnd
            );

            var castName = GameState.GetCastbarContents();
            if (!string.IsNullOrEmpty(castName)) {
                if (castbarEnd - castbarProgress <= 0.01f && gcdTotal > castTotal) {
                    helper.shortCastFinished = true;
                    shortCastCachedSpellName = castName;
                }
                string abilityNameOutput = castName;
                if (conf.castTimePosition == 0 && conf.CastTimeEnabled)
                    abilityNameOutput += " (" + helper.remainingCastTimeString + ")";
                if (helper.queuedAbilityName != " " && conf.CastBarShowQueuedSpell)
                    abilityNameOutput += " -> " + helper.queuedAbilityName;
                    
                DrawBarText(ui, abilityNameOutput);
            }
        }

        private void DrawBarText(PluginUI ui, string abilityName){
            int barWidth = (int)(ui.w_size.X * conf.BarWidthRatio);
            string combinedText = abilityName + helper.remainingCastTimeString + "!)/|";
            Vector2 spellNamePos = new(ui.w_cent.X - ((float)barWidth / 2.05f), ui.w_cent.Y);
            Vector2 spellTimePos = new(ui.w_cent.X + ((float)barWidth / 2.05f), ui.w_cent.Y);

            if (conf.EnableCastText) {
                if (!string.IsNullOrEmpty(abilityName))
                    ui.DrawCastBarText(abilityName, combinedText, spellNamePos, conf.CastBarTextSize, false);
                if (!string.IsNullOrEmpty(helper.remainingCastTimeString) && conf.castTimePosition == 1 && conf.CastTimeEnabled)
                    ui.DrawCastBarText(helper.remainingCastTimeString, combinedText, spellTimePos, conf.CastBarTextSize, true);
            }
        }

        public bool ShouldDraw(bool inCombat, bool noUI) {
            bool shouldShowBar = conf.BarEnabled && !noUI;
            conf.EnabledGBJobs.TryGetValue(DataStore.ClientState.LocalPlayer.ClassJob.Id, out var enabledJobGB);
            bool showBarInCombat = enabledJobGB && (conf.ShowOutOfCombat || inCombat);
            bool showBarWhenGCDNotRunning = !conf.ShowOnlyGCDRunning || 
                                            (helper.idleTimerAccum < helper.GCDTimeoutBuffer);
            bool showCastBarOrNoLastActionTP = conf.CastBarEnabled || !helper.lastActionTP;

            return shouldShowBar && 
                (IsMoveable || 
                (showBarInCombat && 
                showBarWhenGCDNotRunning && 
                showCastBarOrNoLastActionTP));
        }
        public string WindowName => "GCDTracker_Bar";
        public bool IsMoveable => conf.BarWindowMoveable;
    }
}