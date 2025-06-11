using System;
using System.Runtime.CompilerServices;

namespace GCDTracker.UI.Components {
public class QueueLock(BarVertices bar_v, BarDecisionHelper go, Configuration conf) {
    private readonly Configuration conf = conf;
    private readonly BarDecisionHelper go = go;
    private readonly Line line = new(conf, bar_v);

    public float LockPos { get; private set; }

    public Action OnQueueLockReached = delegate { };

    public void Update(BarVertices bar_v) {
        switch (go.CurrentState){
            case BarState.GCDOnly:
            case BarState.ShortCast:
                float lockThresh = Math.Min(0.8f * go.GCDTotal, go.GCDTotal - 0.5f) / go.GCDTotal;
                // As in the slidecast, match to 0.8f if close enough so both lines match.
                if (Math.Abs(lockThresh - 0.8f) < 0.025f)
                    lockThresh = 0.8f;
                LockPos = Math.Max(lockThresh, go.CurrentPos);
                CheckEvents();
                break;
            case BarState.LongCast:
                LockPos = Math.Max(0.8f * (go.GCDTotal / go.CastTotal), go.CurrentPos);
                CheckEvents();
                break;
            case BarState.NonAbilityCast:
            case BarState.NoSlideAbility:
                LockPos = 0f;
                break;
            case BarState.Idle:
            default:
                LockPos = conf.BarQueueLockWhenIdle ? 0.8f : 0f;
                break;
        }

        line.Update(bar_v.ProgToScreen(LockPos));
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