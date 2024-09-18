using System;
using System.Drawing;
using System.Numerics;
using GCDTracker.Utils;

namespace GCDTracker.UI.Components {
    public class Bar(BarInfo bar, BarVertices bar_v) {
        private readonly BarInfo bar = bar;
        private readonly BarVertices bar_v = bar_v;
        private Rectangle rect = new();

        public void Update(int start, int end) {
            rect = new Rectangle(
                start,
                bar_v.Rect.Top,
                end - start,
                bar_v.Height);
        }
        public void Draw(PluginUI ui, Vector4 color) =>
            ui.DrawRectFilledNoAA(rect.LT(), rect.RB(), color);
    }
}