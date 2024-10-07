using ImGuiNET;

namespace GCDTracker.Config.Alerts {
public class CA_Pulse : ConfAlert {
    public override string Name => "Pulse";

    public bool BarWidth = false;
    public bool BarHeight = false;
    public bool BarColor = false;
    public bool WheelSize = false;
    public bool WheelColor = false;
    public float PulseStrength = 1.0f;

    public override void DrawConfig() {
        ImGui.Checkbox("Bar Width", ref BarWidth);
        ImGui.Checkbox("Bar Height", ref BarHeight);
        ImGui.Checkbox("Bar Color", ref BarColor);
        ImGui.Checkbox("Wheel Size", ref WheelSize);
        ImGui.Checkbox("Wheel Color", ref WheelColor);
        ImGui.SliderFloat("Pulse Strength", ref PulseStrength, 0.1f, 2f);
    }
}
}