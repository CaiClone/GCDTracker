using Dalamud.Interface;
using Dalamud.Interface.Animation;
using Dalamud.Interface.Animation.EasingFunctions;
using GCDTracker.Data;
using ImGuiNET;
using System;
using System.Numerics;

namespace GCDTracker.UI {
    public class PluginUI {
        public bool IsVisible { get; set; }
        private readonly Easing alertAnimEnabled;
        private readonly Easing alertAnimPos;
        public GCDDisplay gcd;
        public GCDHelper helper;       
        public ComboTracker ct;
        public Configuration conf;

        public Vector2 w_cent;
        public Vector2 w_size;
        public float Scale;
        private ImDrawListPtr draw;
        private readonly string[] alertText;

        public PluginUI(Configuration conf) {
            this.conf = conf;
            alertAnimEnabled = new OutCubic(new(0, 0, 0, 2, 1000)) {
                Point1 = new(0.25f, 0),
                Point2 = new(1f, 0)
            };
            alertAnimPos = new OutCubic(new(0, 0, 0, 1, 500)) {
                Point1 = new(0, 0),
                Point2 = new(0, -20)
            };
            alertText = ["CLIP", "0.0", "0.00", "A-B-C"];
        }

        public void Draw() {
            conf.DrawConfig(w_size.X, w_size.Y);

            if (DataStore.ClientState.LocalPlayer == null)
                return;

            bool inCombat = DataStore.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat];
            bool noUI = DataStore.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedInQuestEvent]
                        || DataStore.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas]
                        || DataStore.ClientState.IsPvP;

            conf.EnabledGWJobs.TryGetValue(DataStore.ClientState.LocalPlayer.ClassJob.Id, out var enabledJobGW);
            conf.EnabledGBJobs.TryGetValue(DataStore.ClientState.LocalPlayer.ClassJob.Id, out var enabledJobGB);
            conf.EnabledCTJobs.TryGetValue(DataStore.ClientState.LocalPlayer.ClassJob.Id, out var enabledJobCT);

            bool shouldShowGCDWheel = conf.WheelEnabled && !noUI;
            bool isGCDWheelMoveable = conf.WindowMoveableGW;
            bool showGCDWheelInCombat = enabledJobGW && 
                                        (conf.ShowOutOfCombat || inCombat);
            bool showGCDWheelWhenGCDNotRunning = !conf.ShowOnlyGCDRunning || 
                                                (helper.idleTimerAccum < helper.GCDTimeoutBuffer && 
                                                !helper.lastActionTP);

            if (shouldShowGCDWheel && 
                (isGCDWheelMoveable || 
                (showGCDWheelInCombat && 
                showGCDWheelWhenGCDNotRunning))) 
            {
                SetupWindow("GCDTracker_GCDWheel", conf.WindowMoveableGW);
                gcd.DrawGCDWheel(this);
                ImGui.End();
            }

            bool shouldShowBar = conf.BarEnabled && 
                                !noUI;
            bool isBarMoveable = conf.BarWindowMoveable;
            bool showBarInCombat = enabledJobGB && 
                                (conf.ShowOutOfCombat || inCombat);
            bool showBarWhenGCDNotRunning = !conf.ShowOnlyGCDRunning || 
                                            (helper.idleTimerAccum < 
                                            helper.GCDTimeoutBuffer);
            bool showCastBarOrNoLastActionTP = conf.CastBarEnabled || 
                                            !helper.lastActionTP;

            if (shouldShowBar && 
                (isBarMoveable || 
                (showBarInCombat && 
                showBarWhenGCDNotRunning && 
                showCastBarOrNoLastActionTP))) 
            {
                SetupWindow("GCDTracker_Bar", conf.BarWindowMoveable);
                // Hide the GCDBar if the castbar is active.
                // This seems to work fine, but if it ever becomes a problem,
                // might try using string.IsNullOrEmpty(GetCastbarContents())
                // instead of HelperMethods.IsCasting() since that comes
                // directly from the game's castbar.
                if (!conf.CastBarEnabled || !HelperMethods.IsCasting()) {
                    gcd.DrawGCDBar(this);
                }
                if (conf.CastBarEnabled && HelperMethods.IsCasting()) {
                    gcd.DrawCastBar(this);
                }
                ImGui.End();
            }

            bool shouldShowComboTracker = conf.ComboEnabled && !noUI;
            bool isComboTrackerMoveable = conf.WindowMoveableCT;
            bool showComboTrackerInCombat = enabledJobCT && 
                                            (conf.ShowOutOfCombatCT || inCombat);

            if (shouldShowComboTracker && 
                (isComboTrackerMoveable || 
                showComboTrackerInCombat)) 
            {
                SetupWindow("GCDTracker_ComboTracker", conf.WindowMoveableCT);
                ct.DrawComboLines(this, conf);
                ImGui.End();
            }
        }

        private void SetupWindow(string name,bool windowMovable) {
            ImGui.SetNextWindowSize(new Vector2(100, 100), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowBgAlpha(0.45f);
            ImGui.Begin(name, GetFlags(windowMovable));
            GetWindowsInfo();
            draw = ImGui.GetBackgroundDrawList();
        }

        private static ImGuiWindowFlags GetFlags(bool windowMovable) {
            var flags = ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar;
            if (!windowMovable) flags |= ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs;
            return flags;
        }

        private void GetWindowsInfo() {
            var w_pos = ImGui.GetWindowPos();
            w_size = ImGui.GetWindowSize();
            w_cent = new Vector2(w_pos.X + (w_size.X * 0.5f), w_pos.Y + (w_size.Y * 0.5f));
            Scale = w_size.X / 200f;
        }

        public void DrawRect(Vector2 start, Vector2 end, Vector4 color, float thickness) {
            draw.AddRect(start, end, ImGui.GetColorU32(color), 0, ImDrawFlags.None, thickness);
        }

        public void DrawCircSegment(float start_rad, float end_rad, float thickness,Vector4 col) {
            start_rad = Math.Clamp(Math.Min(start_rad, end_rad),0,2);
            end_rad = Math.Clamp(Math.Max(start_rad, end_rad),0,2);
            int n_segments = Math.Clamp((int)Math.Ceiling((end_rad - start_rad) * 30), 1, 40);
            draw.PathArcTo(w_cent, w_size.X*0.3f , (start_rad *2*(float)Math.PI) - 1.57f, (end_rad * 2 * (float)Math.PI) - 1.57f, n_segments);
            draw.PathStroke(ImGui.GetColorU32(col), ImDrawFlags.None, thickness);
        }

        public unsafe void DrawActionCircle(Vector2 cpos,float circRad,uint action, uint lastAction) {
            if (lastAction == action || ct.LastComboActionUsed.Contains(action))
                draw.AddCircleFilled(cpos, circRad, ImGui.GetColorU32(conf.ctComboActive));
            else if (ct.ComboUsed.Contains(action))
                draw.AddCircleFilled(cpos, circRad, ImGui.GetColorU32(conf.ctComboUsed));

            draw.AddCircle(cpos, circRad, ImGui.GetColorU32(conf.backColBorder), 20, 5f * Scale);
            draw.AddCircle(cpos, circRad, ImGui.GetColorU32(conf.backCol), 20, 3f * Scale);
        }

        public void DrawConnectingLine(Vector2 from, Vector2 to, float circRad) {
            var comparison = Math.Sign(from.Y.CompareTo(to.Y));
            //We can only go either 0º or 45º. Sorry for maths but this is probably more efficient
            var vx = circRad + (Math.Abs(comparison)* -1 * (circRad / 2));
            var vy = comparison * (circRad / 2);

            draw.AddLine(from + new Vector2(vx, -vy), to - new Vector2(circRad, 0), ImGui.GetColorU32(conf.backColBorder), 5f * Scale);
            draw.AddLine(from + new Vector2(vx, -vy), to - new Vector2(circRad, 0), ImGui.GetColorU32(conf.backCol), 3f * Scale);
        }

        public void StartAlert(bool isClip, float ms) {
            if (isClip) {
                alertText[1] = string.Format("{0:0.0}", ms);
                alertText[2] = string.Format("{0:0.00}", ms);
            }
            alertAnimEnabled.Restart();
            alertAnimPos.Restart();
        }

        public void DrawAlert(float relx, float rely, float textSize, Vector4 textCol, Vector4 backCol, int alertTextPrecision = 0) {
            if (!alertAnimEnabled.IsRunning || alertAnimEnabled.IsDone) return;
            if (alertTextPrecision > alertText.Length - 1){
                GCDTracker.Log.Error("Alert text precision invalid");
                return;
            }
            if (conf.OverrideDefaltFont)
                ImGui.PushFont(UiBuilder.MonoFont);
            ImGui.SetWindowFontScale(textSize);

            var textSz = ImGui.CalcTextSize(alertText[alertTextPrecision]);
            var textStartPos =
                w_cent
                - (w_size / 2)
                + new Vector2(w_size.X * relx, w_size.Y * rely)
                - (textSz / 2);
            var padding = new Vector2(10, 5) * textSize;

            if (!alertAnimEnabled.IsDone) alertAnimEnabled.Update();
            if (!alertAnimPos.IsDone) alertAnimPos.Update();

            var animAlpha = alertAnimEnabled.EasedPoint.X;
            var animPos = alertAnimPos.EasedPoint;

            draw.AddRectFilled(
                textStartPos - padding + animPos,
                textStartPos + textSz + padding + animPos,
                ImGui.GetColorU32(backCol.WithAlpha(1-animAlpha)), 10f);
            draw.AddText(
                textStartPos + animPos,
                ImGui.GetColorU32(textCol.WithAlpha(1-animAlpha)),
                alertText[alertTextPrecision]);

            ImGui.SetWindowFontScale(1f);
            if (conf.OverrideDefaltFont)
                ImGui.PopFont();
        }

        public void DrawTextOutline(Vector2 textPos, Vector4 textColor, string text, float outlineThickness) {
                Vector4 calculatedOutlineColor = new Vector4(1f, 1f, 1f, textColor.W);
            if (((textColor.X * 0.3f) + (textColor.Y * 0.6f) + (textColor.Z * 0.2f)) > 0.7f)
                calculatedOutlineColor = new Vector4(0f, 0f, 0f, textColor.W);
            uint outlineColor = ImGui.GetColorU32(calculatedOutlineColor);
            
            float[] angles = [0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f];
            float radians = (float)Math.PI / 180f;
            foreach (float angle in angles) {
                float rad = angle * radians;
                float xOffset = (float)Math.Cos(rad) * outlineThickness;
                float yOffset = (float)Math.Sin(rad) * outlineThickness;
                
                // for unknown reasons, this seems to be necessary to "center"
                // the outline vertically.  not perfect; there must be a way to exactly
                // position the outline.  Maybe depends on the font?
                yOffset += 0.2f; 
                
                Vector2 offset = new(xOffset, yOffset);
                draw.AddText(textPos + offset, outlineColor, text);
            }
        }

        public void DrawCastBarText(string text, string combinedText, Vector2 textPos, float textSize, bool isTime) {
            if (!string.IsNullOrEmpty(text)) {
                
                if (conf.OverrideDefaltFont)
                    ImGui.PushFont(UiBuilder.MonoFont);
                ImGui.SetWindowFontScale(textSize);

                Vector2 textPosCentered = new(
                    isTime ? textPos.X - ImGui.CalcTextSize(text).X : textPos.X, 
                    textPos.Y - (ImGui.CalcTextSize(combinedText).Y / 2)
                );

                Vector4 textColorVector = new (conf.CastBarTextColor.X, conf.CastBarTextColor.Y, conf.CastBarTextColor.Z, 1f);
                uint textColor = ImGui.GetColorU32(textColorVector);

                if (conf.CastBarTextOutlineEnabled) {
                    DrawTextOutline(textPosCentered, textColorVector, text, conf.OutlineThickness);                    
                    if (conf.CastBarBoldText) 
                        DrawTextOutline(new(textPosCentered.X + 1f, textPosCentered.Y), textColorVector, text, conf.OutlineThickness);
                }
          
                draw.AddText(textPosCentered, textColor, text);
                if(conf.CastBarBoldText)
                    draw.AddText(new(textPosCentered.X + 1f, textPosCentered.Y), textColor, text);
                
                ImGui.SetWindowFontScale(1f);
                if (conf.OverrideDefaltFont)
                ImGui.PopFont();
            }
        }

        public void DrawRightTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Vector4 color) {
            var originalFlags = draw.Flags;
            draw.Flags &= ~ImDrawListFlags.AntiAliasedFill;
            draw.AddTriangleFilled(p1, p2, p3, ImGui.GetColorU32(color));
            draw.Flags = originalFlags;
        }

        public void DrawRectFilledNoAA(Vector2 start, Vector2 end, Vector4 color, int gradientMode = 0, float gradientIntensity = 0f) {
            var originalFlags = draw.Flags;
            draw.Flags &= ~ImDrawListFlags.AntiAliasedFill;
            
            if (!conf.BarHasGradient || gradientMode == 3) {
                draw.AddRectFilled(start, end, ImGui.GetColorU32(color), 0, ImDrawFlags.None);
                draw.Flags = originalFlags;
                return;
            }
            
            int height = (int)(end.Y - start.Y);
            // Unnecessary unless someone edits the config manually...
            gradientIntensity = Math.Clamp(gradientIntensity, 0f, 1f);
            for (int y = 0; y < height; y++) {
                Vector4 lineColor;

                switch (gradientMode)
                {
                    case 0: // White Mode
                        lineColor = new Vector4(
                            color.X + (gradientIntensity * (1.0f - color.X) * ((float)y / height)),
                            color.Y + (gradientIntensity * (1.0f - color.Y) * ((float)y / height)),
                            color.Z + (gradientIntensity * (1.0f - color.Z) * ((float)y / height)),
                            color.W
                        );
                        break;

                    case 1: // Black Mode
                        lineColor = new Vector4(
                            color.X * (1 - (gradientIntensity * ((float)y / height))),
                            color.Y * (1 - (gradientIntensity * ((float)y / height))),
                            color.Z * (1 - (gradientIntensity * ((float)y / height))),
                            color.W
                        );
                        break;

                    case 2: // Blended Mode: Top half darker, bottom half lighter
                        if (y < height / 2)
                        {
                            lineColor = new Vector4(
                                color.X * (1 - (gradientIntensity * ((float)(height / 2 - y) / (height / 2)))),
                                color.Y * (1 - (gradientIntensity * ((float)(height / 2 - y) / (height / 2)))),
                                color.Z * (1 - (gradientIntensity * ((float)(height / 2 - y) / (height / 2)))),
                                color.W
                            );
                        }
                        else
                        {
                            lineColor = new Vector4(
                                color.X + (gradientIntensity * (1.0f - color.X) * ((float)(y - height / 2) / (height / 2))),
                                color.Y + (gradientIntensity * (1.0f - color.Y) * ((float)(y - height / 2) / (height / 2))),
                                color.Z + (gradientIntensity * (1.0f - color.Z) * ((float)(y - height / 2) / (height / 2))),
                                color.W
                            );
                        }
                        break;

                    default:
                        // just in case
                        lineColor = color;
                        break;
                }

                // Draw each line with the gradient color for that line
                draw.AddLine(
                    new Vector2(start.X, start.Y + y),
                    new Vector2(end.X, start.Y + y),
                    ImGui.GetColorU32(lineColor)
                );
            }

            draw.Flags = originalFlags;
        }

        public void DrawRectNoAA(Vector2 start, Vector2 end, Vector4 color, int thickness) {
            var originalFlags = draw.Flags;
            draw.Flags &= ~ImDrawListFlags.AntiAliasedFill;
            draw.AddRect(start, end, ImGui.GetColorU32(color), 0, ImDrawFlags.None, thickness);
            draw.Flags = originalFlags;
        }
        public void DrawDebugText(float relx, float rely, float textSize, Vector4 textCol, Vector4 backCol, string debugText) {
            ImGui.PushFont(UiBuilder.MonoFont);
            ImGui.SetWindowFontScale(textSize);

            var textSz = ImGui.CalcTextSize(debugText);
            var textStartPos =
                w_cent
                - (w_size / 2)
                + new Vector2(w_size.X * relx, w_size.Y * rely)
                - (textSz / 2);
            var padding = new Vector2(10, 5) * textSize;

            draw.AddRectFilled(
                textStartPos - padding,
                textStartPos + textSz + padding,
                ImGui.GetColorU32(backCol), 10f);
            draw.AddText(
                textStartPos,
                ImGui.GetColorU32(textCol),
                debugText);

            ImGui.SetWindowFontScale(1f);
            ImGui.PopFont();
        }
    }
}
