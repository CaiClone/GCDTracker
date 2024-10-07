
using System.Numerics;

namespace GCDTracker.Config.Alerts {
public enum AlertType { Popup, FloatingWindow, Pulse }
public enum SectionType { Clip, ABC, QueueLock, SlideCast }
public class ConfAlert {
    public AlertType Type;
    public SectionType Section;

    public virtual void DrawConfig() { }
    public virtual string Name => "Error";
}
}