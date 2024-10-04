using Dalamud.Configuration;
using ImGuiNET;

namespace GCDTracker.Config {
public partial class Configuration {
    public bool ShowOutOfCombat = true;
    public bool ShowOnlyGCDRunning = true;
    public float GCDTimeout = 2f;

    private void DrawGeneralConfig() {
        if (ImGui.TreeNodeEx("Visibility")) {
            ImGui.Checkbox("Show out of combat", ref ShowOutOfCombat);
            if (ShowOutOfCombat) {
                ImGui.Indent();
                ImGui.Checkbox("Show only when GCD running", ref ShowOnlyGCDRunning);
                ImGui.SliderFloat("GCD Timeout (in seconds)", ref GCDTimeout, .2f, 4f);
                if (ImGui.IsItemHovered()) {
                    ImGui.BeginTooltip();
                    ImGui.Text("Controls the length of the GCD Timeout.");
                    ImGui.EndTooltip();
                }
                ImGui.Unindent();
            }
            ImGui.TreePop();
        }
    }       
}
}