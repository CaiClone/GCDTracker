using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tests")]
namespace GCDTracker.UI.Components {
public class QueueLock(BarVertices bar_v, BarDecisionHelper go, Configuration conf) {
    private readonly Configuration conf = conf;
    private readonly BarDecisionHelper go = go;
    private readonly Line line = new(conf, bar_v);

    internal float lockPos = 0f;

    public Action OnQueueLockReached = delegate { };

        public void Update(BarVertices bar_v) {
        switch (go.CurrentState){
            case BarState.GCDOnly:
            case BarState.ShortCast:
                lockPos = Math.Max(0.8f, go.CurrentPos);
                CheckEvents();
                break;
            case BarState.LongCast:
                lockPos = Math.Max(0.8f * (go.GCDTotal / go.CastTotal), go.CurrentPos);
                CheckEvents();
                break;
            case BarState.NonAbilityCast:
            case BarState.NoSlideAbility:
                lockPos = 0f;
                break;
            case BarState.Idle:
            default:
                lockPos = conf.BarQueueLockWhenIdle ? 0.8f : 0f;
                break;
        }

        line.Update(bar_v.ProgToScreen(lockPos));
    }

    private void CheckEvents() {
        if (go.CurrentPos >= lockPos - 0.025f && go.CurrentPos > 0.2f)
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