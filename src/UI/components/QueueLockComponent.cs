using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tests")]
namespace GCDTracker.UI.Components {
public class QueueLockComponent(BarVertices bar_v, BarDecisionHelper go, Configuration conf, GCDBar gcdBar) {
    private readonly Configuration conf = conf;
    private readonly BarDecisionHelper go = go;
    private readonly Line line = new(conf, bar_v);
    private readonly GCDBar gcdBar = gcdBar;

    public float LockPos { get; private set; }
    internal float vizLockPos;

    public Action OnQueueLockReached = delegate { };

    public void Update(BarVertices bar_v) {
        switch (go.CurrentState){
            case BarState.GCDOnly:
            case BarState.ShortCast:
                LockPos = Math.Min(0.8f * go.GCDTotal, go.GCDTotal - 0.5f) / go.GCDTotal;
                vizLockPos = LockPos;
                // If near the SlideCast end, let's round it up so it matches exactly
                // This is technically not correct, but moving one pixel the bar so it can be read easier is worth it
                if (Math.Abs(LockPos - gcdBar.SlideCast.EndPos) < 0.025f)
                    vizLockPos = gcdBar.SlideCast.EndPos;
                vizLockPos = Math.Max(vizLockPos, go.CurrentPos);
                CheckEvents();
                
                break;
            case BarState.LongCast:
                LockPos = 0.8f * (go.GCDTotal / go.CastTotal);
                vizLockPos = LockPos;
                // Do the same on the SlideCast Start
                if (Math.Abs(LockPos - gcdBar.SlideCast.StartPos) < 0.025f)
                    vizLockPos = gcdBar.SlideCast.StartPos;
                vizLockPos = Math.Max(vizLockPos, go.CurrentPos);
                CheckEvents();

                break;
            case BarState.NonAbilityCast:
            case BarState.NoSlideAbility:
                LockPos = 0f;
                vizLockPos = 0f;
                break;
            case BarState.Idle:
            default:
                LockPos = 0f;
                vizLockPos = conf.BarQueueLockWhenIdle ? 0.8f : 0f;
                break;
        }

        line.Update(bar_v.ProgToScreen(vizLockPos));
    }

    private void CheckEvents() {
        if (go.CurrentPos >= LockPos - 0.025f && go.CurrentPos > 0.2f)
            OnQueueLockReached();
    }

    public void Draw(PluginUI ui) {
        if (!conf.QueueLockEnabled || LockPos == 0f) return;
        line.Draw(ui, conf.backColBorder);

        if (!conf.ShowQueuelockTriangles) return;
        line.DrawTri(ui, isTop: true, isRight: false, conf.backColBorder);
        if (line.CanFitRightTri())
            line.DrawTri(ui, isTop: true, isRight: true, conf.backColBorder);
    }
}
}