using Dalamud.Game.ClientState;
using Dalamud.Logging;
using GCDTracker.Data;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace GCDTracker
{
    public class PluginUI
    {
        public bool IsVisible { get; set; }
        public GCDWheel gcd;
        public Configuration conf;

        private Vector2 w_cent;
        private Vector2 w_size;
        private float scale;
        private ClientState cs;

        public PluginUI(Configuration conf,ClientState cs)
        {
            this.conf = conf;
            this.cs = cs;
        }
        public void Draw()
        {
            if (cs.LocalPlayer==null)
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
            var w_pos = ImGui.GetWindowPos();
            this.w_size = ImGui.GetWindowSize();
            this.w_cent = new Vector2(w_pos.X + w_size.X * 0.5f, w_pos.Y + w_size.Y * 0.5f);
            this.scale = this.w_size.X / 200f;
            DrawGCDWheel();
            if (conf.ComboEnabled)
            {
                DrawComboLines();
            }
            ImGui.End();
        }

        private void DrawGCDWheel()
        {

            float gcdTotal = gcd.totalGCD;

            float gcdTime = (float)ImGui.GetTime() - gcd.lastGCDtime;

            if (gcdTime > gcdTotal * 1.25f)
                return;

            DrawCircSegment(0f, 1f, 6f * this.scale, conf.backColBorder);//Background
            DrawCircSegment(0f, 1f, 3f * this.scale, conf.backCol); 
            DrawCircSegment(0.8f, 1, 9f * this.scale, conf.backColBorder); //Queue lock
            DrawCircSegment(0.8f, 1, 6f * this.scale, conf.backCol); 

            DrawCircSegment(0f, Math.Min(gcdTime / gcdTotal,1f), 20f * this.scale, conf.frontCol);

            foreach (var (ogcd,anlock) in gcd.ogcds)
            {
                DrawCircSegment(ogcd / gcdTotal, (ogcd + anlock) / gcdTotal, 21f * this.scale, conf.anLockCol * new Vector4(1, 1, 1, Math.Min(1.5f - ((gcdTime - ogcd) / 3f), 1f)));
                DrawCircSegment(ogcd / gcdTotal, (ogcd + 0.04f) / gcdTotal, 23f*this.scale, conf.ogcdCol);
            }
            if (gcdTime > gcdTotal)
                DrawCircSegment(0f, (gcdTime - gcdTotal) / gcdTotal, 21f * this.scale, conf.clipCol);
        }
        private void DrawCircSegment(float start_rad, float end_rad, float thickness,Vector4 col)
        {

            var draw = ImGui.GetForegroundDrawList();
            int n_segments = (int)Math.Round((end_rad-start_rad)*30);
            draw.PathArcTo(this.w_cent, this.w_size.X*0.3f , start_rad*2*(float)Math.PI - 1.57f, end_rad * 2 * (float)Math.PI - 1.57f, n_segments);
            draw.PathStroke(ImGui.GetColorU32(col), ImDrawFlags.None, thickness);
        }
        private void DrawComboLines()
        {
            ImGui.Text($"{cs.LocalPlayer.ClassJob.Id}");
            var xsep = 30*this.scale;
            var ysep = 30 * this.scale;
            int[][] combos;
            var circRad = 8* this.scale;

            if (ComboStore.COMBOS.TryGetValue(cs.LocalPlayer.ClassJob.Id, out combos))
            {
                var startPos = this.w_cent + new Vector2((this.w_size.X * 0.3f) + circRad + 2, -(combos.Length*ysep)/2);
                var draw = ImGui.GetBackgroundDrawList();
                for(uint i=0;i<combos.Length;i++)
                {
                    var combo = combos[i];
                    var cpos = startPos+ new Vector2(0,i*ysep);
                    var bifurc = false;
                    for(uint j=0;j<combo.Length;j++)
                    {
                        if (i > 0 && combos[i - 1][j] == combo[j]) //skip if same start as previous combo
                            bifurc = true;
                        else
                        {
                            if (bifurc) //Draw previous line if it comes from another combo
                            {
                                cpos += new Vector2((xsep + circRad * 2)*j-1, 0);
                                DrawBackLine(draw, cpos, xsep, ysep, circRad);
                                bifurc = false;
                            }
                            cpos += new Vector2(xsep, 0);
                            DrawActionCircle(draw, cpos, circRad);
                            if (j < (combo.Length - 1))
                                DrawFrontLine(draw, cpos, xsep, ysep, circRad);
                            cpos += new Vector2(circRad*2, 0);
                        }
                    }
                }
            }
        }
        private void DrawBackLine(ImDrawListPtr draw,Vector2 cpos,float xsep, float ysep,float circRad)
        {
            draw.AddLine(cpos + new Vector2(xsep - circRad, 0), cpos - new Vector2(circRad + (circRad / 2), ysep - (circRad / 2)), ImGui.GetColorU32(conf.backColBorder), 5f * this.scale);
            draw.AddLine(cpos + new Vector2(xsep - circRad, 0), cpos - new Vector2(circRad + (circRad / 2), ysep - (circRad / 2)), ImGui.GetColorU32(conf.backCol), 3f * this.scale);
        }
        private void DrawFrontLine(ImDrawListPtr draw, Vector2 cpos, float xsep, float ysep, float circRad)
        {
            draw.AddLine(cpos + new Vector2(circRad, 0), cpos + new Vector2(circRad+xsep, 0), ImGui.GetColorU32(conf.backColBorder), 5f * this.scale);
            draw.AddLine(cpos + new Vector2(circRad, 0), cpos + new Vector2(circRad + xsep, 0), ImGui.GetColorU32(conf.backCol), 3f * this.scale);
        }
        private void DrawActionCircle(ImDrawListPtr draw, Vector2 cpos,float circRad)
        {
            draw.AddCircle(cpos, circRad, ImGui.GetColorU32(conf.backColBorder),20,5f*this.scale);
            draw.AddCircle(cpos, circRad, ImGui.GetColorU32(conf.backCol), 20, 3f * this.scale);
        }
    }
}
