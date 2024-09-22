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
        private readonly BarDecisionHelper go;
        private readonly BarInfo info;
        private readonly BarVertices bar_v;

        private readonly QueueLock queueLock;
        private readonly SlideCast slideCast;

        private readonly Bar background;
        private readonly Bar progressBar;

        private string shortCastCachedSpellName;
        private Vector4 bgCache;

        public GCDBar(Configuration conf, GCDHelper helper, AbilityManager abilityManager) {
            this.conf = conf;
            this.helper = helper;
            this.abilityManager = abilityManager;
            go = new BarDecisionHelper();
            info = new BarInfo();
            bar_v = new BarVertices();
            queueLock = new(info, bar_v, conf, go);
            slideCast = new(info, bar_v, conf, go);

            background = new Bar(info, bar_v);
            progressBar = new Bar(info, bar_v);

            slideCast.OnSlideStartReached += TriggerSlideAlert;
            queueLock.OnQueueLockReached += TriggerQueueAlert;
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
            if (gcdTotal < 0.1f) return;

            //When cancel casting there is a frame where gcdTime still shows castTime, so check if previous frame was longCast
            if (go.CurrentState == BarState.LongCast)
                gcdTime = 0;
            
            if (!conf.WheelEnabled)
                helper.MiscEventChecker();

            DrawBarElements(
                ui,
                false,
                helper.shortCastFinished,
                false,
                gcdTime / gcdTotal,
                gcdTotal
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
            float totalBarTime) {
            
            info.Update(
                conf,
                ui.w_size.X,
                ui.w_cent.X,
                ui.w_size.Y,
                ui.w_cent.Y,
                castBarCurrentPos,
                totalBarTime,
                isCastBar, 
                isShortCast,
                isNonAbility
            );

            go.Update(
                info, 
                helper, 
                DataStore.ActionManager->CastActionType, 
                DataStore.ClientState?.LocalPlayer?.TargetObject?.ObjectKind ?? ObjectKind.None
            );

            var notify = GCDEventHandler.Instance;
            notify.Update(info, conf, ui);

            bar_v.Update(info, go, notify);
            slideCast.Update(bar_v);
            queueLock.Update(bar_v);

            if (info.CurrentPos < 0.2f)
                bgCache = helper.BackgroundColor();
            background.Update(bar_v.Rect.Left, bar_v.Rect.Right);
            background.Draw(ui, bgCache, conf.BarBgGradMode, conf.BarBgGradientMul);

            // in both modes:
            // draw cast/gcd progress (main) bar
            if(info.CurrentPos > 0.001f){
                var progressBarColor = notify.ProgressPulseColor;
                progressBar.Update(bar_v.Rect.Left, bar_v.ProgToScreen(info.CurrentPos) +  bar_v.BorderWidth);
                progressBar.Draw(ui, progressBarColor, conf.BarGradMode, conf.BarGradientMul);
            }

            slideCast.Draw(ui);

            float barGCDClipTime = 0;
            // in GCDBar mode:
            // draw oGCDs and clips
            if (!isCastBar) {
                float gcdTime = helper.lastElapsedGCD;
                float gcdTotal = helper.TotalGCD;

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
                            // Draw the clipped part at the beginning
                            var sclip = new Bar(info, bar_v);
                            sclip.Update(bar_v.Rect.Left, bar_v.ProgToScreen(barGCDClipTime / gcdTotal));
                            sclip.Draw(ui, conf.clipCol);
                        }
                    }
                    if(!helper.shortCastFinished || isClipping) {
                        var clip = new Bar(info, bar_v);
                        clip.Update(bar_v.ProgToScreen(ogcdStart / gcdTotal), bar_v.ProgToScreen(ogcdEnd / gcdTotal));
                        clip.Draw(ui, isClipping ? conf.clipCol : conf.anLockCol);
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
            queueLock.Draw(ui);
            background.DrawBorder(ui, conf.backColBorder);
        }
        
        public void DrawCastBar (PluginUI ui) {
            float gcdTotal = DataStore.Action->TotalGCD;
            float castTotal = DataStore.Action->TotalCastTime;
            float castElapsed = DataStore.Action->ElapsedCastTime;
            float castbarProgress = castElapsed / castTotal;
            float castbarEnd = 1f;
            
            bool isTeleport = GameState.IsCastingTeleport();
            // handle short casts
            if (gcdTotal > castTotal) {
                castbarEnd = GameState.CastingNonAbility() ? 1f : castTotal / gcdTotal;
            }

            DrawBarElements(
                ui,
                true,
                gcdTotal > castTotal,
                // Maybe we don't need the gcdTotal < 0.01f anymore?
                GameState.CastingNonAbility() || isTeleport || gcdTotal < 0.01f,
                castbarProgress * castbarEnd,
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

        private void TriggerSlideAlert() {
            go.ActivateAlertIfNeeded(EventType.BarColorPulse, conf.pulseBarColorAtSlide, EventCause.Slidecast);
            go.ActivateAlertIfNeeded(EventType.BarWidthPulse, conf.pulseBarWidthAtSlide, EventCause.Slidecast);
            go.ActivateAlertIfNeeded(EventType.BarHeightPulse, conf.pulseBarHeightAtSlide, EventCause.Slidecast);
        }

        private void TriggerQueueAlert() {
            go.ActivateAlertIfNeeded(EventType.BarColorPulse, conf.pulseBarColorAtQueue, EventCause.Queuelock);
            go.ActivateAlertIfNeeded(EventType.BarWidthPulse, conf.pulseBarWidthAtQueue, EventCause.Queuelock);
            go.ActivateAlertIfNeeded(EventType.BarHeightPulse, conf.pulseBarHeightAtQueue, EventCause.Queuelock);
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