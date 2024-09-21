using System;
using System.Drawing;
using GCDTracker.Data;
using GCDTracker.Utils;

    namespace GCDTracker.UI.Components {
    public unsafe class SlideCast {
        private readonly BarInfo info;
        private readonly Configuration conf;
        private readonly BarDecisionHelper go;
        private readonly Line lineL;
        private readonly Line lineR;
        private readonly Bar bar;

        private float startPos;
        private float endPos;

        public System.Action OnSlideStartReached;

        public SlideCast(BarInfo info, BarVertices bar_v, Configuration conf, BarDecisionHelper go) {
            this.info = info;
            this.conf = conf;
            this.go = go;
            lineL = new(info, bar_v);
            lineR = new(info, bar_v);
            bar = new(info, bar_v);
            go.OnReset += Reset;
        }
    
        public void Update(BarVertices bar_v) {
            if (!conf.SlideCastEnabled) return;
            float castTotal = DataStore.Action->TotalCastTime;

            switch (go.CurrentState){
                case BarState.ShortCast:
                    float gcdTotal = DataStore.Action->TotalGCD;
                    startPos = Math.Max((castTotal - conf.SlidecastDelay) / gcdTotal, 0f);
                    endPos = castTotal / gcdTotal;
                    break;
                case BarState.LongCast:
                    startPos = Math.Max((castTotal - conf.SlidecastDelay) / castTotal, 0f);
                    endPos = 1f;
                    break;
                case BarState.NonAbilityCast:
                case BarState.NoSlideAbility:
                default:
                    Reset();
                    return;
            }
            CheckEvents();
            startPos = Math.Max(startPos, info.CurrentPos);
            endPos = (conf.SlideCastFullBar || info.IsNonAbility) ? 1f : Math.Max(endPos, info.CurrentPos);
            UpdateVisualization(bar_v);
        }

        private void Reset() => startPos = endPos = 0f;

        private void CheckEvents() {
            if (info.CurrentPos >= startPos - 0.025f && info.CurrentPos > 0.2f)
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