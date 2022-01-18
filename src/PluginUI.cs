﻿using Dalamud.Interface;
using Dalamud.Interface.Animation;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Logging;
using GCDTracker.Data;
using ImGuiNET;
using System;
using System.Numerics;

namespace GCDTracker
{
    public class PluginUI
    {
        public bool IsVisible { get; set; }
        private Easing clipAnimAlpha;
        private Easing clipAnimPos;
        public GCDWheel gcd;
        public ComboTracker ct;
        public Configuration conf;

        public Vector2 w_cent;
        public Vector2 w_size;
        public float Scale;
        private ImDrawListPtr draw;

        public PluginUI(Configuration conf)
        {
            this.conf = conf;
            clipAnimAlpha = new OutCubic(new(0, 0, 0, 2, 500));
            clipAnimAlpha.Point1 = new(0.65f, 0);
            clipAnimAlpha.Point2 = new(1f, 0);
            clipAnimPos = new OutCubic(new(0, 0, 0, 1, 500));
            clipAnimPos.Point1 = new(0, 0);
            clipAnimPos.Point2 = new(0, -30);

        }
        public unsafe void Draw()
        {

            conf.DrawConfig();

            if (DataStore.clientState.LocalPlayer == null)
                return;

            bool inCombat = DataStore.condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat];
            bool noUI = DataStore.condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedInQuestEvent]
                        || DataStore.condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas];

            conf.EnabledGWJobs.TryGetValue(DataStore.clientState.LocalPlayer.ClassJob.Id, out var enabledJobGW);
            conf.EnabledCTJobs.TryGetValue(DataStore.clientState.LocalPlayer.ClassJob.Id, out var enabledJobCT);
            if (conf.WheelEnabled && !noUI && (conf.WindowMoveableGW || (enabledJobGW && (conf.ShowOutOfCombatGW || inCombat))))
            {
                SetupWindow("GCDTracker_GCDWheel", conf.WindowMoveableGW);
                gcd.DrawGCDWheel(this, conf);
                ImGui.End();
            }

            if (conf.ComboEnabled && !noUI && (conf.WindowMoveableCT || (enabledJobCT && (conf.ShowOutOfCombatCT || inCombat))))
            {
                SetupWindow("GCDTracker_ComboTracker", conf.WindowMoveableCT);
                ct.DrawComboLines(this, conf);
                ImGui.End();
            }
        }
        private void SetupWindow(string name,bool windowMovable)
        {
            ImGui.SetNextWindowSize(new Vector2(100, 100), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowBgAlpha(0.45f);
            ImGui.Begin(name, getFlags(windowMovable));
            getWindowsInfo();
            draw = ImGui.GetBackgroundDrawList();
        }
        private ImGuiWindowFlags getFlags(bool windowMovable)
        {
            var flags = ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar;
            if (!windowMovable) flags |= ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs;
            return flags;
        }
        private void getWindowsInfo()
        {
            var w_pos = ImGui.GetWindowPos();
            this.w_size = ImGui.GetWindowSize();
            this.w_cent = new Vector2(w_pos.X + w_size.X * 0.5f, w_pos.Y + w_size.Y * 0.5f);
            this.Scale = this.w_size.X / 200f;
        }
        public void DrawCircSegment(float start_rad, float end_rad, float thickness,Vector4 col)
        {
            int n_segments = (int)Math.Round((end_rad-start_rad)*30);
            draw.PathArcTo(this.w_cent, this.w_size.X*0.3f , start_rad*2*(float)Math.PI - 1.57f, end_rad * 2 * (float)Math.PI - 1.57f, n_segments);
            draw.PathStroke(ImGui.GetColorU32(col), ImDrawFlags.None, thickness);
        }

        public unsafe void DrawActionCircle(Vector2 cpos,float circRad,uint action)
        {
            if (DataStore.combo->Action == action || ct.LastComboActionUsed.Contains(action))
                draw.AddCircleFilled(cpos, circRad, ImGui.GetColorU32(conf.ctComboActive));
            else if (ct.ComboUsed.Contains(action))
                draw.AddCircleFilled(cpos, circRad, ImGui.GetColorU32(conf.ctComboUsed));

            draw.AddCircle(cpos, circRad, ImGui.GetColorU32(conf.backColBorder), 20, 5f * this.Scale);
            draw.AddCircle(cpos, circRad, ImGui.GetColorU32(conf.backCol), 20, 3f * this.Scale);
        }

        public void DrawConnectingLine(Vector2 from, Vector2 to, float circRad)
        {
            var comparison = Math.Sign(from.Y.CompareTo(to.Y));
            //We can only go either 0º or 45º. Sorry for maths but this is probably more efficient
            var vx = circRad + (Math.Abs(comparison)*-1 * (circRad / 2));
            var vy = (comparison * (circRad / 2));

            draw.AddLine(from + new Vector2(vx, -vy), to - new Vector2(circRad, 0), ImGui.GetColorU32(conf.backColBorder), 5f * this.Scale);
            draw.AddLine(from + new Vector2(vx, -vy), to - new Vector2(circRad, 0), ImGui.GetColorU32(conf.backCol), 3f * this.Scale);
        }
        public void StartClip()
        {
            clipAnimAlpha.Restart();
            clipAnimPos.Restart();
        }

        public void DrawClip()
        {
            if (!clipAnimAlpha.IsRunning || clipAnimAlpha.IsDone) return;

            ImGui.PushFont(UiBuilder.MonoFont);
            ImGui.SetWindowFontScale(1.3f * this.Scale);

            var clipText = "CLIP";
            var textSz = ImGui.CalcTextSize(clipText);
            var textStartPos = w_cent - (textSz / 2) - new Vector2(0,this.w_size.X * 0.3f + 20*this.Scale);
            var padding = new Vector2(10, 5) * this.Scale;

            if (!clipAnimAlpha.IsDone) clipAnimAlpha.Update();
            if (!clipAnimPos.IsDone) clipAnimPos.Update();
            var animAlpha = clipAnimAlpha.EasedPoint.X;
            var animPos = clipAnimPos.EasedPoint;

            draw.AddRectFilled(textStartPos - padding + animPos, textStartPos + textSz + padding + animPos, ImGui.GetColorU32(new Vector4(conf.clipCol.X, conf.clipCol.Y, conf.clipCol.Z, 1-animAlpha)), 10f);
            draw.AddText(textStartPos + animPos, ImGui.GetColorU32(new Vector4(conf.frontCol.X, conf.frontCol.Y, conf.frontCol.Z,1-animAlpha)), clipText);

            ImGui.SetWindowFontScale(1f);
            ImGui.PopFont();
        }

    }
}
