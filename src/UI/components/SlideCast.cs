using System;
using System.Drawing;
using GCDTracker.Utils;

    namespace GCDTracker.UI.Components {
    public class SlideCast(BarInfo info, BarVertices bar_v, Configuration conf, BarDecisionHelper go){
        private readonly BarInfo info = info;
        private readonly Configuration conf = conf;
        private readonly BarDecisionHelper go = go;
        private readonly Line lineL = new(info, bar_v);
        private readonly Line lineR = new(info, bar_v);
        private readonly Bar bar = new(info, bar_v);

        public void Update(BarVertices bar_v) {
            if (!conf.SlideCastEnabled) return;
            int xStart = (int)(info.CenterX + (go.Slide_Bar_Start * bar_v.Width) - bar_v.HalfWidth);
            int xEnd = (int)(info.CenterX + (go.Slide_Bar_End * bar_v.Width) - bar_v.HalfWidth);
            xEnd = Math.Min(xEnd, bar_v.RightLimit);
            lineL.Update(xStart);
            bar.Update(xStart, xEnd);
            lineR.Update(xEnd);
        }

        public void Draw(PluginUI ui) {
            if (!conf.SlideCastEnabled) return;
            if (go.Slide_Background)
                bar.Draw(ui, conf.slideCol);
            // Vertical lines:
            if (!go.SlideStart_VerticalBar) return;
            lineL.Draw(ui, conf.backColBorder);
            if(lineR.Left - lineL.Left > 1)
                lineR.Draw(ui, conf.backColBorder);
            
            // Triangles:
            // TODO: fix logic on when to draw tris
            if (!go.SlideStart_LeftTri) return;
            lineL.DrawTri(ui, isTop: false, isRight: false, conf.backColBorder);
            if (lineR.CanFitRightTri())
                lineR.DrawTri(ui, isTop: false, isRight: true, conf.backColBorder);
            else if (lineL.CanFitRightTri())
                lineL.DrawTri(ui, isTop: false, isRight: true, conf.backColBorder);
        }
        
    }
}