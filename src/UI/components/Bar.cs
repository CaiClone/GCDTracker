using System;
using System.Drawing;
using System.Numerics;
using GCDTracker.Utils;

namespace GCDTracker.UI.Components {
    public class Bar(BarInfo info, BarVertices bar_v) {
        private readonly BarInfo info = info;
        private readonly BarVertices bar_v = bar_v;
        private Rectangle rect = new();

        public void Update(int start, int end) {
            rect = new Rectangle(
                start,
                bar_v.Rect.Top,
                end - start,
                bar_v.Height);
        }
        public void Draw(PluginUI ui, Vector4 col, int gradientMode = 0, float gradientIntensity = 0f) =>
            ui.DrawRectFilledNoAA(rect.LT(), rect.RB(), col, gradientMode, gradientIntensity);

        public void DrawBorder(PluginUI ui, Vector4 col) {
            if (info.BorderSize > 0) {
                ui.DrawRect(
                    bar_v.Rect.LB() - new Vector2(info.HalfBorderSize, info.HalfBorderSize),
                    bar_v.Rect.RT() + new Vector2(info.HalfBorderSize, info.HalfBorderSize),
                    col, info.BorderSize);
            }
        }
    }
}