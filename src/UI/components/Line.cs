using System;
using System.Drawing;
using System.Numerics;
using GCDTracker.Utils;

namespace GCDTracker.UI.Components {
    public class Line(BarInfo bar, BarVertices bar_v) {
        private readonly BarInfo bar = bar;
        private readonly BarVertices bar_v = bar_v;
        private Rectangle rect = new();

        public int Left => rect.Left;
        public void Update(int pos) {
            rect = new Rectangle(
                pos,
                bar_v.Rect.Top,
                bar_v.BorderWidth,
                bar_v.Height);
        }
        public void Draw(PluginUI ui, Vector4 color) =>
            ui.DrawRectFilledNoAA(rect.LT(), rect.RB(), color);

        public bool CanFitRightTri() => rect.Right + bar.TriangleOffset <= bar_v.RightLimit;

        public void DrawTri(PluginUI ui, bool isTop, bool isRight, Vector4 color) {
            float x = isRight ? rect.Right : rect.Left;
            float y = isTop ? rect.Top : rect.Bottom;
            // Need to add 1 to the right tri to avoid a bug in AA
            ui.DrawAATriangle(
                new Vector2(x, y),
                new Vector2(x + (isRight ? bar.TriangleOffset + 1 : -bar.TriangleOffset), y),
                new Vector2(x, y + (isTop ? 1 : -1) * bar.TriangleOffset),
                color);
        }

    }
}