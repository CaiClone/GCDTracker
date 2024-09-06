using System;
using System.Drawing;
using System.Numerics;
using GCDTracker.Utils;

namespace GCDTracker.UI.Components {
    public class BarSegment(BarInfo bar, bool HasTopTri) {
        private Rectangle rect;
        private readonly bool hasTopTri = HasTopTri;
        private readonly BarInfo bar = bar;

        private float borderWidth;
        private int barLimit;

        public void Update(BarVertices bar_v, float start, float end = -1) {
            if (end == -1) end = start;
            barLimit = (int) bar_v.EndVertex.X + 1;
            rect = new Rectangle(
                (int)(bar.CenterX + (start * bar_v.Width) - bar_v.HalfWidth),
                (int)(bar.CenterY - bar_v.RawHalfHeight),
                (int)Math.Min((end + bar_v.BorderWidthPercent - start) * bar_v.Width - 1, barLimit),
                (int)(2 * bar_v.HalfHeight));
            borderWidth = bar_v.BorderWidthPercent * bar_v.Width;
        }

        public void DrawRect(PluginUI ui, Vector4 color) =>
            ui.DrawRectFilledNoAA(rect.LT(), rect.RB(), color);

        public void DrawTriangles(PluginUI ui, Vector4 color) {
            (Vector2 L, Vector2 R) = hasTopTri ? (rect.LT(), rect.RT()) : (rect.LB(), rect.RB());
            Vector2 triHOffset = new(bar.TriangleOffset, 0);
            Vector2 triVOffset = new(0, bar.TriangleOffset * (hasTopTri ? 1 : -1));
            Vector2 borderOffset = new(borderWidth, 0);
    
            // Left
            ui.DrawAATriangle(L, L - triHOffset, L + triVOffset, color);
            Vector2 RightmostPoint = R + triHOffset + borderOffset;
            if (RightmostPoint.X > barLimit){
                // If the triangle falls outside we can try to draw it on the left line
                R = L;
                RightmostPoint = R + triHOffset + borderOffset;
                if (RightmostPoint.X > barLimit) // If it's still outside, don't draw
                    return;
            }
            R += borderOffset;
            triVOffset += new Vector2(0, hasTopTri ? 1 : -1); // Neccesary to avoid a bug in AA
            // Right
            ui.DrawAATriangle(R, RightmostPoint, R + triVOffset, color);
        }

        public void DrawVerticalLines(PluginUI ui, Vector4 color) {
            // Left
            ui.DrawRectFilledNoAA(rect.LT(), rect.LB() + new Vector2(borderWidth, 0), color);
            // Right
            Vector2 RightmostPoint = rect.RB() + new Vector2(borderWidth, 0);
            if (RightmostPoint.X <= barLimit) // Don't draw if it's outside the limit
                ui.DrawRectFilledNoAA(rect.RT(), RightmostPoint, color);
        }
    }
}
