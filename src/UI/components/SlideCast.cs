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

        private float startPos;
        private float endPos;
        private bool showTris;

        public System.Action OnSlideStartReached;

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
                startPos = info.GCDTime_SlidecastStart;
                endPos = (conf.SlideCastFullBar || info.IsNonAbility) ? 1f : info.GCDTotal_SlidecastEnd;
            }
            
            switch (go.CurrentState){
                case BarState.ShortCast:
                case BarState.LongCast:
                    showTris = conf.ShowSlidecastTriangles && conf.ShowTrianglesOnHardCasts;
                    startPos = Math.Max(startPos, info.CurrentPos);
                    endPos = Math.Max(endPos, info.CurrentPos);
                    BarCheckSlideEvent();
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
            startPos = endPos = 0f;
            showTris = false;
        }
        
        private void BarCheckSlideEvent() {
            if (info.CurrentPos >= startPos - 0.025f && info.CurrentPos > 0.2f)
                OnSlideStartReached?.Invoke();
        }

        private void UpdateVisualization(BarVertices bar_v) {
            int xStart = bar_v.ProgToScreen(startPos);
            int xEnd = bar_v.ProgToScreen(endPos);
            xEnd = Math.Min(xEnd, bar_v.RightLimit);

            lineL.Update(xStart);
            bar.Update(xStart, xEnd);
            lineR.Update(xEnd);
        }

        public void Draw(PluginUI ui) {
            if (!conf.SlideCastEnabled || endPos == 0f) return;
            bar.Draw(ui, conf.slideCol);
            // Vertical lines:
            lineL.Draw(ui, conf.backColBorder);
            if(lineR.Left - lineL.Left > 1)
                lineR.Draw(ui, conf.backColBorder);
            
            // Triangles:
            if (!showTris) return;
            lineL.DrawTri(ui, isTop: false, isRight: false, conf.backColBorder);
            if (lineR.CanFitRightTri())
                lineR.DrawTri(ui, isTop: false, isRight: true, conf.backColBorder);
            else if (lineL.CanFitRightTri())
                lineL.DrawTri(ui, isTop: false, isRight: true, conf.backColBorder);
        }
        
    }
}