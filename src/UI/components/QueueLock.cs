using System;
using System.Drawing;

namespace GCDTracker.UI.Components {
public class QueueLock {
    private readonly BarInfo info;
    private readonly Configuration conf;
    private readonly BarDecisionHelper go;
    private readonly Line line;

    private float lockPos = 0f;

    public Action OnQueueLockReached = delegate { };

    public QueueLock(BarInfo info, BarVertices bar_v, Configuration conf, BarDecisionHelper go) {
        this.info = info;
        this.conf = conf;
        this.go = go;
        line = new(info, bar_v);
        go.OnReset += Reset;
    }

    public void Update(BarVertices bar_v) {
        switch (go.CurrentState){
            case BarState.GCDOnly:
            case BarState.ShortCast:
                lockPos = Math.Max(0.8f, info.CurrentPos);
                break;
            case BarState.LongCast:
                lockPos = Math.Max(0.8f * (info.GCDTotal / info.CastTotal), info.CurrentPos);
                CheckEvents();
                break;
            case BarState.NonAbilityCast:
            case BarState.NoSlideAbility:
                lockPos = 0f;
                break;
            case BarState.Idle:
            default:
                Reset();
                break;
        }

        line.Update(bar_v.ProgToScreen(lockPos));
    }
    private void Reset() => lockPos = conf.BarQueueLockWhenIdle ? 0.8f : 0f;

    private void CheckEvents() {
        if (info.CurrentPos >= lockPos - 0.025f && info.CurrentPos > 0.2f)
            OnQueueLockReached();
    }

    public void Draw(PluginUI ui) {
        if (!conf.QueueLockEnabled || lockPos == 0f) return;
        line.Draw(ui, conf.backColBorder);

        if (!conf.ShowQueuelockTriangles) return;
        line.DrawTri(ui, isTop: true, isRight: false, conf.backColBorder);
        if (line.CanFitRightTri())
            line.DrawTri(ui, isTop: true, isRight: true, conf.backColBorder);
    }
}
}