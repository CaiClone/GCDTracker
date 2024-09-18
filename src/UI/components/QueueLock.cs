using System.Drawing;

namespace GCDTracker.UI.Components {
public class QueueLock(BarInfo bar, BarVertices bar_v, Configuration conf, BarDecisionHelper go) {
    private readonly BarInfo bar = bar;
    private readonly Configuration conf = conf;
    private readonly BarDecisionHelper go = go;
    private readonly Line line = new(bar, bar_v);

    public void Update(BarVertices bar_v) =>
        line.Update((int)bar_v.ProgToScreen(go.Queue_Lock_Start));

    public void Draw(PluginUI ui) {
        line.Draw(ui, conf.backColBorder);
        line.DrawTri(ui, isTop: true, isRight: false, conf.backColBorder);
        if (line.CanFitRightTri())
            line.DrawTri(ui, isTop: true, isRight: true, conf.backColBorder);
    }
}
}