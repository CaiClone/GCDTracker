using GCDTracker.Data;
using ImGuiNET;
using System;
using System.Numerics;

namespace GCDTracker
{
    public class PluginUI
    {
        public bool IsVisible { get; set; }
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
        }
        public unsafe void Draw()
        {
            if (DataStore.clientState.LocalPlayer==null)
                return;

            conf.DrawConfig();
            var flags = ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar;
            if (conf.WindowLocked)
            {
                flags |= ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoInputs;
            }

            ImGui.SetNextWindowSize(new Vector2(300, 300),ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowBgAlpha(0.45f);
            ImGui.Begin("GCDTracker_UI", flags);
            getWindowsInfo();
            draw = ImGui.GetBackgroundDrawList();

            bool wheeldrawn = gcd.DrawGCDWheel(this,conf);
            if (conf.ComboEnabled && (ct.ComboUsed.Count>0 || wheeldrawn))
            {
                ct.DrawComboLines(this,conf);
            }
            ImGui.End();
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
            if (DataStore.combo->Action == action)
            {
                draw.AddCircleFilled(cpos, circRad, ImGui.GetColorU32(conf.ctComboActive));
            }
            else if (ct.ComboUsed.Contains(action))
            {
                draw.AddCircleFilled(cpos, circRad, ImGui.GetColorU32(conf.ctComboUsed));
            }
            draw.AddCircle(cpos, circRad, ImGui.GetColorU32(conf.backColBorder), 20, 5f * this.Scale);
            draw.AddCircle(cpos, circRad, ImGui.GetColorU32(conf.backCol), 20, 3f * this.Scale);
        }

        public void DrawConnectingLine(Vector2 from, Vector2 to, float circRad)
        {
            //We can only go either 0º or 45º. Sorry for maths but this is probably more efficient
            var vx = circRad + (from.Y.CompareTo(to.Y) * (circRad / 2));
            var vy = (from.Y.CompareTo(to.Y) * (circRad / 2));

            draw.AddLine(from + new Vector2(vx, -vy), to - new Vector2(circRad, 0), ImGui.GetColorU32(conf.backColBorder), 5f * this.Scale);
            draw.AddLine(from + new Vector2(vx, -vy), to - new Vector2(circRad, 0), ImGui.GetColorU32(conf.backCol), 3f * this.Scale);
        }
    }
}
