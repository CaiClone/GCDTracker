﻿using Dalamud.Interface;
using Dalamud.Interface.Animation;
using GCDTracker.Data;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace GCDTracker.UI {
    public class PluginUI(Configuration conf) {
        public bool IsVisible { get; set; }
        public GCDBar bar;
        public GCDHelper helper;
        public Configuration conf = conf;

        public Vector2 w_cent;
        public Vector2 w_size;
        public float Scale;
        private ImDrawListPtr draw;

        public List<IWindow> Windows;

        public void Draw() {
            conf.DrawConfig(bar);

            if (DataStore.ClientState.LocalPlayer == null)
                return;
        
            bool inCombat = DataStore.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat];
            bool noUI = DataStore.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedInQuestEvent]
                        || DataStore.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas]
                        || DataStore.ClientState.IsPvP;
            foreach (var window in Windows) {
                window.DrawWindow(this, inCombat, noUI);
            }
        }

        public void SetupWindow(string name,bool windowMovable) {
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

        public void DrawCircSegment(float start_rad, float end_rad, float thickness,Vector4 col) {
            start_rad = Math.Clamp(Math.Min(start_rad, end_rad),0,2);
            end_rad = Math.Clamp(Math.Max(start_rad, end_rad),0,2);
            int n_segments = Math.Clamp((int)Math.Ceiling((end_rad - start_rad) * 30), 1, 40);
            draw.PathArcTo(w_cent, w_size.X*0.3f , (start_rad *2*(float)Math.PI) - 1.57f, (end_rad * 2 * (float)Math.PI) - 1.57f, n_segments);
            draw.PathStroke(ImGui.GetColorU32(col), ImDrawFlags.None, thickness);
        }

        public unsafe void DrawActionCircle(Vector2 cpos,float circRad,uint action, uint lastAction, ComboTracker ct) {
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

        public void DrawAlert(float relx, float rely, Alert alert) {
            var notify = GCDEventHandler.Instance;
            var config = alert.Reason == EventCause.Clip 
                ? new AlertConfig {
                    AnimEnabled = notify.clipAnimEnabled,
                    AnimPos = notify.clipAnimPos,
                    TextColor = conf.ClipTextColor,
                    BackColor = conf.ClipBackColor,
                    TextSize = conf.ClipTextSize,
                    TextPrecision = conf.ClipAlertPrecision
                }
                : new AlertConfig {
                    AnimEnabled = notify.abcAnimEnabled,
                    AnimPos = notify.abcAnimPos,
                    TextColor = conf.abcTextColor,
                    BackColor = conf.abcBackColor,
                    TextSize = conf.abcTextSize,
                    TextPrecision = 3
                };

            // Update animations
            if (!config.AnimEnabled.IsDone) config.AnimEnabled.Update();
            if (!config.AnimPos.IsDone) config.AnimPos.Update();

            // Validate alertTextPrecision
            if (config.TextPrecision > notify.alertText.Length - 1) {
                GCDTracker.Log.Error("Alert text precision invalid");
                return;
            }
            if (conf.OverrideDefaltFont)
                ImGui.PushFont(UiBuilder.MonoFont);

            float animAlpha = config.AnimEnabled.EasedPoint.X;
            Vector2 animPos = config.AnimPos.EasedPoint;

            var textSz = ImGui.CalcTextSize(notify.alertText[config.TextPrecision]);
            var textStartPos = 
                w_cent - (w_size / 2) + 
                new Vector2(w_size.X * relx, w_size.Y * rely) - 
                (textSz / 2);
            var padding = new Vector2(10, 5) * config.TextSize;

            draw.AddRectFilled(
                textStartPos - padding + animPos,
                textStartPos + textSz + padding + animPos,
                ImGui.GetColorU32(config.BackColor.WithAlpha(1 - animAlpha)), 10f);
            draw.AddText(
                textStartPos + animPos,
                ImGui.GetColorU32(config.TextColor.WithAlpha(1 - animAlpha)),
                notify.alertText[config.TextPrecision]);

            ImGui.SetWindowFontScale(1f);
            if (conf.OverrideDefaltFont)
                ImGui.PopFont();
        }

        private class AlertConfig {
            public Easing AnimEnabled { get; set; }
            public Easing AnimPos { get; set; }
            public Vector4 TextColor { get; set; }
            public Vector4 BackColor { get; set; }
            public float TextSize { get; set; }
            public int TextPrecision { get; set; }
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

        public void DrawAATriangle(Vector2 p1, Vector2 p2, Vector2 p3, Vector4 color) {
            var originalFlags = draw.Flags;
            draw.Flags &= ~ImDrawListFlags.AntiAliasedFill;
            draw.AddTriangleFilled(p1, p2, p3, ImGui.GetColorU32(color));
            draw.Flags = originalFlags;
        }

        public void DrawRect(Vector2 start, Vector2 end, Vector4 color, float thickness) {
            draw.AddRect(start, end, ImGui.GetColorU32(color), 0, ImDrawFlags.None, thickness);
        }

        public void DrawRectFilledNoAA(Vector2 start, Vector2 end, Vector4 color, int gradientMode = 0, float gradientIntensity = 0f) {
            var eGradientMode = (BarGradientMode)gradientMode;
            var originalFlags = draw.Flags;
            draw.Flags &= ~ImDrawListFlags.AntiAliasedFill;
            
            if (!conf.BarHasGradient || eGradientMode == BarGradientMode.None) {
                draw.AddRectFilled(start, end, ImGui.GetColorU32(color), 0, ImDrawFlags.None);
                draw.Flags = originalFlags;
                return;
            }
            
            int height = (int)(end.Y - start.Y);
            gradientIntensity = Math.Clamp(gradientIntensity, 0f, 1f);
            for (int y = 0; y < height; y++) {
                var lineColor = eGradientMode switch {
                    BarGradientMode.White => LerpColorNoA(color, Vector4.One, gradientIntensity * y / height),
                    BarGradientMode.Black => LerpColorNoA(color, Vector4.Zero, gradientIntensity * y / height),
                    BarGradientMode.Blended when y < height / 2 => LerpColorNoA(color, Vector4.Zero, gradientIntensity * (height / 2 - y) / (height / 2)),
                    BarGradientMode.Blended => LerpColorNoA(color, Vector4.One, gradientIntensity * (y - height / 2) / (height / 2)),
                    _ => color
                };

                // Draw each line with the gradient color for that line
                draw.AddLine(
                    new Vector2(start.X, start.Y + y),
                    new Vector2(end.X, start.Y + y),
                    ImGui.GetColorU32(lineColor)
                );
            }

            draw.Flags = originalFlags;
        }

        private static Vector4 LerpColorNoA(Vector4 start, Vector4 end, float t) => new(
                start.X + (end.X - start.X) * t,
                start.Y + (end.Y - start.Y) * t,
                start.Z + (end.Z - start.Z) * t,
                start.W
        );
    }
    public enum BarGradientMode {
        White = 0,
        Black = 1,
        Blended = 2,
        None = 3
    }
}
