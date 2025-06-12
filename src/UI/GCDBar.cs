using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;
using GCDTracker.Data;
using GCDTracker.UI.Components;

namespace GCDTracker.UI {
    public unsafe class GCDBar : IWindow {
        private readonly Configuration conf;
        private readonly GCDHelper helper;
        private readonly AbilityManager abilityManager;
        private readonly BarDecisionHelper go;
        private readonly BarVertices bar_v;
        private readonly GCDEventHandler notify;

        public readonly QueueLockComponent QueueLock;
        public readonly SlideCastComponent SlideCast;

        private readonly Bar background;
        private readonly Bar progressBar;

        private string shortCastCachedSpellName;
        private Vector4 bgCache;

        public GCDBar(Configuration conf, GCDHelper helper, AbilityManager abilityManager) {
            this.conf = conf;
            this.helper = helper;
            this.abilityManager = abilityManager;
            notify = GCDEventHandler.Instance;
            go = new BarDecisionHelper(conf);
            bar_v = new BarVertices(conf);
            QueueLock = new(bar_v, go, conf, this);
            SlideCast = new(bar_v, go, conf);

            background = new Bar(bar_v);
            progressBar = new Bar(bar_v);

            SlideCast.OnSlideStartReached += TriggerSlideAlert;
            QueueLock.OnQueueLockReached += TriggerQueueAlert;
        }

        public void Update(IFramework _) {
            go.Update(helper,
                DataStore.ActionManager->CastActionType,
                DataStore.ClientState?.LocalPlayer?.TargetObject?.ObjectKind ?? ObjectKind.None);
            SlideCast.Update(bar_v);
            QueueLock.Update(bar_v);
        }

        public void Draw(PluginUI ui) {
            bar_v.Update(ui, notify);
            if (go.IsCastBar) {
                DrawCastBar(ui);
            } else {
                if (!conf.WheelEnabled)
                    helper.MiscEventChecker();
                DrawGCDBar(ui);
            }
            notify.Update(bar_v, conf, ui);
        }

        private void DrawGCDBar(PluginUI ui) {
            float gcdTotal = helper.TotalGCD;
            float gcdTime = helper.lastElapsedGCD;
            if (gcdTotal < 0.1f) return;

            DrawBackground(ui);
            DrawProgress(ui);
            if (go.CurrentState != BarState.ShortCast) {
                DrawOGCDs(ui);
            } else {
                SlideCast.Draw(ui);
            }
            QueueLock.Draw(ui);
            DrawBackgroundBorder(ui);
            DrawTextGCDBar(ui);
        }

        private void DrawTextGCDBar(PluginUI ui) {
            if (go.CurrentState == BarState.Idle) return;
            bool hasQueuedSpell = !string.IsNullOrWhiteSpace(helper.queuedAbilityName);

            // We need to handle the case where we just switched from the castbar because it ended, we want to still keep it until
            // the next GCD
            if (go.CurrentState == BarState.ShortCast) {
                var text = shortCastCachedSpellName;
                if (conf.CastBarShowQueuedSpell && hasQueuedSpell)
                    text += $" -> {helper.queuedAbilityName}";
                DrawBarText(ui, text);
                return;
            }
            if (conf.ShowQueuedSpellNameGCD && hasQueuedSpell && go.CurrentPos >= QueueLock.LockPos) {
                DrawBarText(ui, $" -> {helper.queuedAbilityName}");
            }
        }


        public void DrawCastBar(PluginUI ui) {
            float gcdTotal = DataStore.Action->TotalGCD;
            float castTotal = DataStore.Action->TotalCastTime;
            float castElapsed = DataStore.Action->ElapsedCastTime;
            float castbarProgress = castElapsed / castTotal;

            DrawBackground(ui);
            DrawProgress(ui);
            SlideCast.Draw(ui);
            QueueLock.Draw(ui);
            DrawBackgroundBorder(ui);

            var castName = GameState.GetCastbarContents();
            if (!string.IsNullOrEmpty(castName)) {
                shortCastCachedSpellName = castName;
                string abilityNameOutput = castName;
                if (conf.castTimePosition == 0 && conf.CastTimeEnabled)
                    abilityNameOutput += " (" + helper.remainingCastTimeString + ")";
                if (helper.queuedAbilityName != " " && conf.CastBarShowQueuedSpell)
                    abilityNameOutput += " -> " + helper.queuedAbilityName;

                DrawBarText(ui, abilityNameOutput);
            }
        }

        private void DrawBackground(PluginUI ui) {
            if (go.CurrentPos < 0.2f)
                bgCache = helper.BackgroundColor();
            background.Update(bar_v.Rect.Left, bar_v.Rect.Right);
            background.Draw(ui, bgCache, conf.BarBgGradMode, conf.BarBgGradientMul);
        }

        private void DrawBackgroundBorder(PluginUI ui) => background.DrawBorder(ui, conf.backColBorder);

        private void DrawProgress(PluginUI ui) {
            if (go.CurrentPos > 0.001f) {
                var progressBarColor = notify.ProgressPulseColor;
                progressBar.Update(bar_v.Rect.Left, bar_v.ProgToScreen(go.CurrentPos) + bar_v.BorderSize);
                progressBar.Draw(ui, progressBarColor, conf.BarGradMode, conf.BarGradientMul);
            }
        }

        private void DrawOGCDs(PluginUI ui) {
            float gcdTotal = helper.TotalGCD;
            float gcdTime = helper.lastElapsedGCD;
            float barGCDClipTime = 0;

            // draw oGCDs and clips
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
                        var sclip = new Bar(bar_v);
                        sclip.Update(bar_v.Rect.Left, bar_v.ProgToScreen(barGCDClipTime / gcdTotal));
                        sclip.Draw(ui, conf.clipCol);
                    }
                }
                if (go.CurrentState != BarState.ShortCast || isClipping) {
                    var clip = new Bar(bar_v);
                    clip.Update(bar_v.ProgToScreen(ogcdStart / gcdTotal), bar_v.ProgToScreen(ogcdEnd / gcdTotal));
                    clip.Draw(ui, isClipping ? conf.clipCol : conf.anLockCol);
                    if (!iscast && (!isClipping || ogcdStart > 0.01f)) {
                        var clipPos = bar_v.ProgToScreen(ogcdStart / gcdTotal);
                        ui.DrawRectFilledNoAA(
                            new Vector2(clipPos, bar_v.Rect.Top + bar_v.BorderSize),
                            new Vector2(clipPos + 2f * ui.Scale, bar_v.Rect.Bottom - bar_v.BorderSize),
                            conf.ogcdCol);
                    }
                }
            }
        }

        private void DrawBarText(PluginUI ui, string abilityName) {
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

        public Vector2 GetBarSize() => new(bar_v.Width, bar_v.Height);

        public bool ShouldDraw(bool inCombat, bool noUI) {
            bool shouldShowBar = conf.BarEnabled && !noUI;
            conf.EnabledGBJobs.TryGetValue(DataStore.ClientState.LocalPlayer.ClassJob.RowId, out var enabledJobGB);
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