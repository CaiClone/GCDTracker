using System;
using System.Drawing;
using GCDTracker.Utils;

    namespace GCDTracker.UI.Components {
    public class SlideCast {
        private readonly BarInfo info;
        private readonly Configuration conf;
        private readonly BarDecisionHelper go;
        private readonly Line lineL;
        private readonly Line lineR;
        private readonly Bar bar;

        private float slide_Bar_Start;
        private float slide_Bar_End;

        public SlideCast(BarInfo info, BarVertices bar_v, Configuration conf, BarDecisionHelper go) {
            this.info = info;
            this.conf = conf;
            this.go = go;
            lineL = new(info, bar_v);
            lineR = new(info, bar_v);
            bar = new(info, bar_v);
            go.OnReset += ResetSlideCast;
        }
    
        public void Update(BarVertices bar_v) {
            if (!conf.SlideCastEnabled) return;
            if (info.IsCastBar) {
                slide_Bar_Start = info.GCDTime_SlidecastStart;
                slide_Bar_End = (conf.SlideCastFullBar || info.IsNonAbility) ? 1f : info.GCDTotal_SlidecastEnd;
            }
            
            switch (go.CurrentState){
                case BarState.ShortCast:
                case BarState.LongCast:
                    slide_Bar_Start = Math.Max(slide_Bar_Start, info.CurrentPos);
                    slide_Bar_End = Math.Max(slide_Bar_End, info.CurrentPos);
                    BarCheckSlideEvent(info, conf);
                    break;
                case BarState.NonAbilityCast:
                case BarState.NoSlideAbility:
                default:
                    ResetSlideCast();
                    break;
            }

            UpdateVisualization(bar_v);
        }

        private void ResetSlideCast() {
            slide_Bar_Start = 0f;
            slide_Bar_End = 0f;
        }
        
        private void BarCheckSlideEvent(BarInfo bar, Configuration conf){
            var notify = AlertManager.Instance;
            if (info.CurrentPos >= slide_Bar_Start - 0.025f && info.CurrentPos > 0.2f) {
                go.ActivateAlertIfNeeded(EventType.BarColorPulse, conf.pulseBarColorAtSlide);
                go.ActivateAlertIfNeeded(EventType.BarWidthPulse, conf.pulseBarWidthAtSlide);
                go.ActivateAlertIfNeeded(EventType.BarHeightPulse, conf.pulseBarHeightAtSlide);
            }
        }

        private void UpdateVisualization(BarVertices bar_v) {
            int xStart = bar_v.ProgToScreen(slide_Bar_Start);
            int xEnd = bar_v.ProgToScreen(slide_Bar_End);
            xEnd = Math.Min(xEnd, bar_v.RightLimit);

            lineL.Update(xStart);
            bar.Update(xStart, xEnd);
            lineR.Update(xEnd);
        }

        public void Draw(PluginUI ui) {
            if (!conf.SlideCastEnabled || slide_Bar_End == 0f) return;
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