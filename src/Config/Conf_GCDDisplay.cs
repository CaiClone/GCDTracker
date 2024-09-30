using GCDTracker.UI;
using ImGuiNET;
using System.Numerics;
using System.Text.Json.Serialization;

namespace GCDTracker.Config {
public partial class Configuration {
    public Vector4 backCol = new(0.376f, 0.376f, 0.376f, 1);
    public Vector4 backColBorder = new(0f, 0f, 0f, 1f);
    public Vector4 frontCol = new(0.9f, 0.9f, 0.9f, 1f);
    public Vector4 ogcdCol = new(1f, 1f, 1f, 1f);
    public Vector4 anLockCol = new(0.334f, 0.334f, 0.334f, 0.667f);

    private void DrawGCDDisplayConfig(GCDBar bar) {
        if (ImGui.TreeNode("Display")) {
            ImGui.Columns(2);
            ImGui.ColorEdit4("Background color", ref backCol, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Border color", ref backColBorder, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Progress color", ref frontCol, ImGuiColorEditFlags.NoInputs);
            ImGui.NextColumn();
            ImGui.ColorEdit4("GCD start indicator color", ref ogcdCol, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Animation lock color", ref anLockCol, ImGuiColorEditFlags.NoInputs);
            ImGui.Columns(1);
            ImGui.TreePop();
        }
        if (ImGui.BeginTabBar("##GCDisplayTabs")) {
            if (ImGui.BeginTabItem("GCDBar")) {
                DrawGCDBarConfig(bar);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("GCDWheel")) {
                DrawGCDWheelConfig();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }
}
}