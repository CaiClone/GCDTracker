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
                vizLockPos = Math.Max(SnapToPos(LockPos, gcdBar.SlideCast.EndPos), go.CurrentPos);
                CheckEvents();
                
                break;
            case BarState.LongCast:
                LockPos = 0.8f * (go.GCDTotal / go.CastTotal);
                vizLockPos = Math.Max(SnapToPos(LockPos, gcdBar.SlideCast.StartPos), go.CurrentPos);
                CheckEvents();

                break;
            case BarState.NonAbilityCast:
            case BarState.NoSlideAbility:
                LockPos = vizLockPos = 0f;
                break;
            case BarState.Idle:
            default:
                LockPos = 0f;
                vizLockPos = conf.BarQueueLockWhenIdle ? 0.8f : 0f;
                break;
        }

        line.Update(bar_v.ProgToScreen(vizLockPos));
    }

    private static float SnapToPos(float val, float target, float margin = 0.025f)
       => Math.Abs(val - target) < margin ? target : val;

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