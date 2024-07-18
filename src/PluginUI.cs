using Dalamud.Interface;
using Dalamud.Interface.Animation;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Logging;
using Dalamud.Utility.Numerics;
using GCDTracker.Data;
using ImGuiNET;
using System;
using System.Diagnostics;
using System.Numerics;

namespace GCDTracker
{
    public class PluginUI
    {
        public bool IsVisible { get; set; }
        private readonly Easing clipAnimAlpha;
        private readonly Easing clipAnimPos;
        private readonly Easing abcAnimAlpha;
        private readonly Easing abcAnimPos;
        public GCDWheel gcd;
        public ComboTracker ct;
        public Configuration conf;

        public Vector2 w_cent;
        public Vector2 w_size;
        public float Scale;
        private ImDrawListPtr draw;
        private string[] clipText;

        public PluginUI(Configuration conf) {
            this.conf = conf;
            clipAnimAlpha = new OutCubic(new(0, 0, 0, 2, 1000)) {
                Point1 = new(0.25f, 0),
                Point2 = new(1f, 0)
            };
            clipAnimPos = new OutCubic(new(0, 0, 0, 1, 500)) {
                Point1 = new(0, 0),
                Point2 = new(0, -20)
            };
            abcAnimAlpha = new OutCubic(new(0, 0, 0, 2, 1000)) {
                Point1 = new(0.25f, 0),
                Point2 = new(1f, 0)
            };
            abcAnimPos = new OutCubic(new(0, 0, 0, 1, 500)) {
                Point1 = new(0, 0),
                Point2 = new(0, -20)
            };

            clipText = ["CLIP", "0.0", "0.00"];
        }

        public void Draw() {
            conf.DrawConfig();

            if (DataStore.ClientState.LocalPlayer == null)
                return;

            bool inCombat = DataStore.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat];
            bool noUI = DataStore.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedInQuestEvent]
                        || DataStore.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas]
                        || DataStore.ClientState.IsPvP;

            conf.EnabledGWJobs.TryGetValue(DataStore.ClientState.LocalPlayer.ClassJob.Id, out var enabledJobGW);
            conf.EnabledGBJobs.TryGetValue(DataStore.ClientState.LocalPlayer.ClassJob.Id, out var enabledJobGB);
            conf.EnabledCTJobs.TryGetValue(DataStore.ClientState.LocalPlayer.ClassJob.Id, out var enabledJobCT);
            
            if (conf.WheelEnabled && !noUI && (conf.WindowMoveableGW || 
                (enabledJobGW
                    && (conf.ShowOutOfCombatGW || inCombat)
                    && (!conf.ShowOnlyGCDRunningGW || gcd.SecondsSinceGCDEnd < conf.GCDTimeout)
                    ))) {
                SetupWindow("GCDTracker_GCDWheel", conf.WindowMoveableGW);
                gcd.DrawGCDWheel(this, conf);
                ImGui.End();
            }

            if (conf.BarEnabled && !noUI && (conf.BarWindowMoveable || 
                (enabledJobGB 
                    && (conf.BarShowOutOfCombat || inCombat)
                    && (!conf.BarShowOnlyGCDRunning || gcd.SecondsSinceGCDEnd < conf.GCDTimeout)
                    ))) {
                SetupWindow("GCDTracker_Bar", conf.BarWindowMoveable);
                gcd.DrawGCDBar(this, conf);
                ImGui.End();
            }


            if (conf.ComboEnabled && !noUI && (conf.WindowMoveableCT || 
                (enabledJobCT 
                    && (conf.ShowOutOfCombatCT || inCombat)
                    ))) {
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

        public void DrawBar(float startRatio, float endRatio, float width, float thickness, Vector4 color) {
            float start =  w_cent.X + (startRatio * width) - (width / 2);
            float end = w_cent.X + (endRatio * width) - (width / 2);
            draw.AddRectFilled(
                new Vector2(start, w_cent.Y - (thickness / 2)),
                new Vector2(end, w_cent.Y + (thickness / 2)),
                ImGui.GetColorU32(color), 0, ImDrawFlags.None);
        }

        public void DrawRect(Vector2 start, Vector2 end, Vector4 color, float thickness) {
            draw.AddRect(start, end, ImGui.GetColorU32(color), 0, ImDrawFlags.None, thickness);
        }

        public void DrawRectFilled(Vector2 start, Vector2 end, Vector4 color) {
            draw.AddRectFilled(start, end, ImGui.GetColorU32(color), 0, ImDrawFlags.None);
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

        public void StartClip(float ms) {
            if (!conf.ClipAlertEnabled && !conf.BarClipAlertEnabled) return;
            clipText[1] = string.Format("{0:0.0}", ms);
            clipText[2] = string.Format("{0:0.00}", ms);
            clipAnimAlpha.Restart();
            clipAnimPos.Restart();
        }

        public void DrawClip(float relx, float rely, float textSize, Vector4 textCol, Vector4 backCol, int clipTextPrecision = 0) {
            if (!clipAnimAlpha.IsRunning || clipAnimAlpha.IsDone) return;
            if (clipTextPrecision > clipText.Length - 1){
                GCDTracker.Log.Error("Clip text precision invalid");
                return;
            }

            ImGui.PushFont(UiBuilder.MonoFont);
            ImGui.SetWindowFontScale(textSize);

            var textSz = ImGui.CalcTextSize(clipText[clipTextPrecision]);
            var textStartPos =
                w_cent
                - (w_size / 2)
                + new Vector2(w_size.X * relx, w_size.Y * rely)
                - (textSz / 2);
            var padding = new Vector2(10, 5) * textSize;

            if (!clipAnimAlpha.IsDone) clipAnimAlpha.Update();
            if (!clipAnimPos.IsDone) clipAnimPos.Update();

            var animAlpha = clipAnimAlpha.EasedPoint.X;
            var animPos = clipAnimPos.EasedPoint;

            draw.AddRectFilled(
                textStartPos - padding + animPos,
                textStartPos + textSz + padding + animPos,
                ImGui.GetColorU32(backCol.WithAlpha(1-animAlpha)), 10f);
            draw.AddText(
                textStartPos + animPos,
                ImGui.GetColorU32(textCol.WithAlpha(1-animAlpha)),
                clipText[clipTextPrecision]);

            ImGui.SetWindowFontScale(1f);
            ImGui.PopFont();
        } 

        public void StartABC() {
            if (!conf.abcAlertEnabled && !conf.BarABCAlertEnabled) return;
            abcAnimAlpha.Restart();
            abcAnimPos.Restart();
        }
        public void DrawABC(float relx, float rely, float textSize, Vector4 textCol, Vector4 backCol) {
            if (!abcAnimAlpha.IsRunning || abcAnimAlpha.IsDone) return;

            ImGui.PushFont(UiBuilder.MonoFont);
            ImGui.SetWindowFontScale(textSize);

            var textSz = ImGui.CalcTextSize("A-B-C");
            var textStartPos =
                w_cent
                - (w_size / 2)
                + new Vector2(w_size.X * relx, w_size.Y * rely)
                - (textSz / 2);
            var padding = new Vector2(10, 5) * textSize;

            if (!abcAnimAlpha.IsDone) abcAnimAlpha.Update();
            if (!abcAnimPos.IsDone) abcAnimPos.Update();

            var animAlpha = abcAnimAlpha.EasedPoint.X;
            var animPos = abcAnimPos.EasedPoint;

            draw.AddRectFilled(
                textStartPos - padding + animPos,
                textStartPos + textSz + padding + animPos,
                ImGui.GetColorU32(backCol.WithAlpha(1-animAlpha)), 10f);
            draw.AddText(
                textStartPos + animPos,
                ImGui.GetColorU32(textCol.WithAlpha(1-animAlpha)),
                "A-B-C");

            ImGui.SetWindowFontScale(1f);
            ImGui.PopFont();
        }
    }
}
