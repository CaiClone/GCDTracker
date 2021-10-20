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

            bool wheeldrawn = gcd.DrawGCDWheel(this,conf);
            if (conf.ComboEnabled && ((DataStore.combo->Timer>0f && DataStore.combo->Action!=0) || wheeldrawn))
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

            var draw = ImGui.GetBackgroundDrawList();
            int n_segments = (int)Math.Round((end_rad-start_rad)*30);
            draw.PathArcTo(this.w_cent, this.w_size.X*0.3f , start_rad*2*(float)Math.PI - 1.57f, end_rad * 2 * (float)Math.PI - 1.57f, n_segments);
            draw.PathStroke(ImGui.GetColorU32(col), ImDrawFlags.None, thickness);
        }
        public void DrawBackLine(ImDrawListPtr draw,Vector2 cpos,float xsep, float ysep,float circRad)
        {
            draw.AddLine(cpos + new Vector2(xsep - circRad, 0), cpos - new Vector2(circRad + (circRad / 2), ysep - (circRad / 2)), ImGui.GetColorU32(conf.backColBorder), 5f * this.Scale);
            draw.AddLine(cpos + new Vector2(xsep - circRad, 0), cpos - new Vector2(circRad + (circRad / 2), ysep - (circRad / 2)), ImGui.GetColorU32(conf.backCol), 3f * this.Scale);
        }
        public void DrawFrontLine(ImDrawListPtr draw, Vector2 cpos, float xsep, float ysep, float circRad)
        {
            draw.AddLine(cpos + new Vector2(circRad, 0), cpos + new Vector2(circRad+xsep, 0), ImGui.GetColorU32(conf.backColBorder), 5f * this.Scale);
            draw.AddLine(cpos + new Vector2(circRad, 0), cpos + new Vector2(circRad + xsep, 0), ImGui.GetColorU32(conf.backCol), 3f * this.Scale);
        }
        public unsafe void DrawActionCircle(ImDrawListPtr draw, Vector2 cpos,float circRad,uint action)
        {
            if (DataStore.combo->Action == action)
            {
                draw.AddCircleFilled(cpos, circRad, ImGui.GetColorU32(conf.ctComboActive));
            }
            else if (ct.ComboUsed.Contains(action))
            {
                draw.AddCircleFilled(cpos, circRad, ImGui.GetColorU32(conf.ctComboUsed));
            }
            draw.AddCircle(cpos, circRad, ImGui.GetColorU32(conf.backColBorder),20,5f*this.Scale);
            draw.AddCircle(cpos, circRad, ImGui.GetColorU32(conf.backCol), 20, 3f * this.Scale);
        }
    }
}
