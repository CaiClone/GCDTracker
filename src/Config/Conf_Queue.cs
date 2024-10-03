using ImGuiNET;

namespace GCDTracker.Config {
public partial class Configuration {
    public bool QueueLockEnabled = true;
    public bool BarQueueLockWhenIdle = true;
    public bool ShowQueuelockTriangles = true;

    
    private void DrawQueueLockGeneralConfig() {
        ImGui.Checkbox("Show queue lock", ref QueueLockEnabled);
        if (ImGui.IsItemHovered()){
            ImGui.BeginTooltip();
            ImGui.Text("If enabled, the wheel background will expand on the timing where you can queue the next GCD.");
            ImGui.EndTooltip();
        }
    }

    private void DrawQueueLockBarConfig() {
        DrawQueueLockGeneralConfig();
        if (QueueLockEnabled && BarEnabled && ShowAdvanced){
            ImGui.Checkbox("Show Queue Lock When Idle", ref BarQueueLockWhenIdle);
            ImGui.Checkbox("Show Queue Lock Triangles", ref ShowQueuelockTriangles);
            if (ImGui.IsItemHovered()){
                ImGui.BeginTooltip();
                ImGui.Text("If enabled, show a triangle on the top of the queuelock indicator");
                ImGui.EndTooltip();
            }
            if (ShowQueuelockTriangles) {
                ImGui.SliderInt("Triangle Size", ref triangleSize, 0, 12);
                if (ImGui.IsItemHovered()){
                    ImGui.BeginTooltip();
                    ImGui.Text("Triangle size shared with (Castbar) Slidelock.");
                    ImGui.EndTooltip();
                }
            }
        }
    }

    private void DrawQueueLockWheelConfig() => DrawQueueLockGeneralConfig();
}
}