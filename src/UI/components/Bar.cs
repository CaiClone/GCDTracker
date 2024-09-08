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
                (int)(bar.CenterY - bar_v.RawHalfHeight),
                end - start,
                2 * bar_v.HalfHeight);
        }
        public void Draw(PluginUI ui, Vector4 color) =>
            ui.DrawRectFilledNoAA(rect.LT(), rect.RB(), color);
    }
}