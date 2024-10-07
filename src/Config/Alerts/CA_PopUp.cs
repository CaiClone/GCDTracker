
using System.Numerics;
using ImGuiNET;

namespace GCDTracker.Config.Alerts {
public class CA_PopUp : ConfAlert {
    public override string Name => "Popup";

    public float TextSize = 0.86f;
    public int ClipAlertPrecision = 0;
    public Vector4 TextColor = new(0.9f, 0.9f, 0.9f, 1f);

    public override void DrawConfig() {
        ImGui.SliderFloat("Text Size", ref TextSize, 0.2f, 2f);
        ImGui.ColorEdit4("Text color", ref TextColor, ImGuiColorEditFlags.NoInputs);
        if (Section == SectionType.Clip) {
            ImGui.SameLine();
            ImGui.RadioButton("CLIP", ref ClipAlertPrecision, 0);
            ImGui.SameLine();
            ImGui.RadioButton("0.X", ref ClipAlertPrecision, 1);
            ImGui.SameLine();
            ImGui.RadioButton("0.XX", ref ClipAlertPrecision, 2);
        }
    }
}
}