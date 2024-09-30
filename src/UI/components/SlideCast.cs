using GCDTracker.Config;
using System;

    namespace GCDTracker.UI.Components {
    public unsafe class SlideCast {
        private readonly Configuration conf;
        private readonly BarDecisionHelper go;
        private readonly Line lineL;
        private readonly Line lineR;
        private readonly Bar bar;

        private float startPos;
        private float endPos;

        public System.Action OnSlideStartReached;

        public SlideCast(BarVertices bar_v, BarDecisionHelper go, Configuration conf) {
            this.conf = conf;
            this.go = go;
            lineL = new(conf, bar_v);
            lineR = new(conf, bar_v);
            bar = new(bar_v);
            go.OnReset += Reset;
        }
    
        public void Update(BarVertices bar_v) {
            if (!conf.SlideCastEnabled) return;

            switch (go.CurrentState){
                case BarState.ShortCast:
                case BarState.LongCast:
                    startPos = Math.Max((go.CastTotal - conf.SlidecastDelay) / go.BarEnd, 0f);
                    endPos = go.CastTotal / go.BarEnd;
                    break;
                case BarState.NonAbilityCast:
                case BarState.NoSlideAbility:
                default:
                    Reset();
                    return;
            }
            CheckEvents();
            startPos = Math.Max(startPos, go.CurrentPos);
            endPos = (conf.SlideCastFullBar || go.IsNonAbility) ? 1f : Math.Max(endPos, go.CurrentPos);
            UpdateVisualization(bar_v);
        }

        private void Reset() => startPos = endPos = 0f;

        private void CheckEvents() {
            if (go.CurrentPos >= startPos - 0.025f && go.CurrentPos > 0.2f)
                OnSlideStartReached();
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
            if (!conf.ShowSlidecastTriangles || (!conf.ShowTrianglesOnHardCasts && go.CurrentState == BarState.LongCast)) return;
            lineL.DrawTri(ui, isTop: false, isRight: false, conf.backColBorder);
            if (lineR.CanFitRightTri())
                lineR.DrawTri(ui, isTop: false, isRight: true, conf.backColBorder);
            else if (lineL.CanFitRightTri())
                lineL.DrawTri(ui, isTop: false, isRight: true, conf.backColBorder);
        }
        
    }
}