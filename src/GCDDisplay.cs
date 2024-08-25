using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using GCDTracker.Data;
using GCDTracker.UI;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tests")]
namespace GCDTracker {
    public unsafe class GCDDisplay {
        private readonly Configuration conf;
        private readonly IDataManager dataManager;
        private readonly GCDHelper helper;
        private readonly AbilityManager abilityManager;
        string shortCastCachedSpellName;
        Vector4 bgCache;

        public GCDDisplay (Configuration conf, IDataManager dataManager, GCDHelper helper) {
            this.conf = conf;
            this.dataManager = dataManager;
            this.helper = helper;
            abilityManager = AbilityManager.Instance;
        }

        public void DrawGCDWheel(PluginUI ui) {
            float gcdTotal = helper.TotalGCD;
            float gcdTime = helper.lastElapsedGCD;

            if (GameState.IsCasting() && DataStore.Action->ElapsedCastTime >= gcdTotal && !GameState.IsCastingTeleport())
                gcdTime = gcdTotal;
            if (gcdTotal < 0.1f) return;
            helper.FlagAlerts(ui);
            helper.InvokeAlerts(0.5f, 0, ui);
            // Background
            ui.DrawCircSegment(0f, 1f, 6f * ui.Scale, conf.backColBorder);
            ui.DrawCircSegment(0f, 1f, 3f * ui.Scale, helper.BackgroundColor());
            if (conf.QueueLockEnabled) {
                ui.DrawCircSegment(0.8f, 1, 9f * ui.Scale, conf.backColBorder);
                ui.DrawCircSegment(0.8f, 1, 6f * ui.Scale, helper.BackgroundColor());
            }
            ui.DrawCircSegment(0f, Math.Min(gcdTime / gcdTotal, 1f), 20f * ui.Scale, conf.frontCol);
            foreach (var (ogcd, (anlock, iscast)) in abilityManager.ogcds) {
                var isClipping = helper.CheckClip(iscast, ogcd, anlock, gcdTotal, gcdTime);
                ui.DrawCircSegment(ogcd / gcdTotal, (ogcd + anlock) / gcdTotal, 21f * ui.Scale, isClipping ? conf.clipCol : conf.anLockCol);
                if (!iscast) ui.DrawCircSegment(ogcd / gcdTotal, (ogcd + 0.04f) / gcdTotal, 23f * ui.Scale, conf.ogcdCol);
            }
        }

        public void DrawGCDBar(PluginUI ui) {
            float gcdTotal = helper.TotalGCD;
            float gcdTime = helper.lastElapsedGCD;

            if (GameState.IsCasting() && DataStore.Action->ElapsedCastTime >= gcdTotal && !GameState.IsCastingTeleport())
                gcdTime = gcdTotal;
            if (gcdTotal < 0.1f) return;
            helper.FlagAlerts(ui);
            helper.InvokeAlerts((conf.BarWidthRatio + 1) / 2.1f, -0.3f, ui);

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
                ui.w_size.X,
                ui.w_cent.X,
                conf.BarWidthRatio,
                ui.w_size.Y,
                ui.w_cent.Y,
                conf.BarHeightRatio,
                conf.BarBorderSizeInt,
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
            var sc_sv = SlideCastStartVertices.Instance;
            sc_sv.Update(bar, go);
            var sc_ev = SlideCastEndVertices.Instance;
            sc_ev.Update(bar, go);
            var ql_v = QueueLockVertices.Instance;
            ql_v.Update (bar, go); 

            float barGCDClipTime = 0;
            
            // in both modes:
            // draw the background
            if (bar.CurrentPos < 0.2f)
                bgCache = helper.BackgroundColor();
            ui.DrawRectFilledNoAA(bar.StartVertex, bar.EndVertex, bgCache, conf.BarBgGradMode, conf.BarBgGradientMul);

            // in both modes:
            // draw cast/gcd progress (main) bar
            if(bar.CurrentPos > 0.001f)
                ui.DrawRectFilledNoAA(bar.StartVertex, bar.ProgressVertex, conf.frontCol, conf.BarGradMode, conf.BarGradientMul);
            
            // in Castbar mode:
            // draw the slidecast bar
            if (conf.SlideCastEnabled)
                DrawSlideCast(ui, sc_sv, sc_ev, go);

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
                        if (!helper.isHardCast) {
                            // create end vertex
                            Vector2 clipEndVector = new(
                                (int)(bar.CenterX + ((barGCDClipTime / gcdTotal) * bar.Width) - bar.HalfWidth),
                                (int)(bar.CenterY + bar.HalfHeight)
                            );
                            // Draw the clipped part at the beginning
                            ui.DrawRectFilledNoAA(bar.StartVertex, clipEndVector, conf.clipCol);
                        }
                    }

                    Vector2 oGCDStartVector = new(
                        (int)(bar.CenterX + ((ogcdStart / gcdTotal) * bar.Width) - bar.RawHalfWidth),
                        (int)(bar.CenterY - bar.RawHalfHeight)
                    );
                    Vector2 oGCDEndVector = new(
                        (int)(bar.CenterX + ((ogcdEnd / gcdTotal) * bar.Width) - bar.HalfWidth),
                        (int)(bar.CenterY + bar.HalfHeight)
                    );

                    ui.DrawRectFilledNoAA(oGCDStartVector, oGCDEndVector, isClipping ? conf.clipCol : conf.anLockCol);
                    if (!iscast && (!isClipping || ogcdStart > 0.01f)) {
                        Vector2 clipPos = new(
                            bar.CenterX + (ogcdStart / gcdTotal * bar.Width) - bar.RawHalfWidth,
                            bar.CenterY - bar.RawHalfHeight + 1f
                        );
                        ui.DrawRectFilledNoAA(clipPos,
                            clipPos + new Vector2(2f * ui.Scale, bar.Height - 2f),
                            conf.ogcdCol);
                    }
                }
            }

            //in both modes:
            //draw the queuelock (if enabled)
            if (conf.QueueLockEnabled)
                DrawQueueLock(ui, ql_v, go);

            // in both modes:
            // draw borders
            if (bar.BorderSize > 0) {
                ui.DrawRect(
                    bar.StartVertex - new Vector2(bar.HalfBorderSize, bar.HalfBorderSize),
                    bar.EndVertex + new Vector2(bar.HalfBorderSize, bar.HalfBorderSize),
                    conf.backColBorder, bar.BorderSize);
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

        private void DrawSlideCast(PluginUI ui, SlideCastStartVertices sc_sv, SlideCastEndVertices sc_ev, BarDecisionHelper go){
            // draw slidecast bar
            if (go.Slide_Background)
                ui.DrawRectFilledNoAA(sc_sv.TL_C, sc_ev.BR_C, conf.slideCol);
            // draw sidecast (start) vertical line
            if (go.SlideStart_VerticalBar)
                ui.DrawRectFilledNoAA(sc_sv.TL_C, sc_sv.BR_C, conf.backColBorder);
            //draw sidlecast (end) vertical line
            if (go.SlideEnd_VerticalBar)
                ui.DrawRectFilledNoAA(sc_ev.TL_C, sc_ev.BR_C, conf.backColBorder);
            //bottom left
            if (go.SlideStart_LeftTri)
                ui.DrawRightTriangle(sc_sv.BL_C, sc_sv.BL_X, sc_sv.BL_Y, conf.backColBorder);
            //bottom right
            if (go.SlideStart_RightTri)
                ui.DrawRightTriangle(sc_sv.BR_C, sc_sv.BR_X, sc_sv.BR_Y, conf.backColBorder);
            //end right
            if (go.SlideEnd_RightTri)
                ui.DrawRightTriangle(sc_ev.BR_C, sc_ev.BR_X, sc_ev.BR_Y, conf.backColBorder);
        }

        private void DrawQueueLock(PluginUI ui, QueueLockVertices ql_v, BarDecisionHelper go) {
            //queue vertical bar
            if (go.Queue_VerticalBar)
                ui.DrawRectFilledNoAA(ql_v.TL_C, ql_v.BR_C, conf.backColBorder); 
            //queue triangle
            if (go.Queue_Triangle) {
                ui.DrawRightTriangle(ql_v.TL_C, ql_v.TL_X, ql_v.TL_Y, conf.backColBorder);
                ui.DrawRightTriangle(ql_v.TR_C, ql_v.TR_X, ql_v.TR_Y, conf.backColBorder);
            }
        }
    }
}