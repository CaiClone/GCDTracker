using ImGuiNET;

namespace GCDTracker.Config {
public partial class Configuration {
    public bool SlideCastEnabled = true;
    public bool SlideCastFullBar = false;
    public bool SlideCastBackground = false;
    
    // Slidecast
    public bool ShowSlidecastTriangles = true;
    public bool ShowTrianglesOnHardCasts = true;
    public int triangleSize = 6; // Shared
    public float SlidecastDelay = 0.5f;

    private void DrawSlideCastBarConfig() {
        ImGui.Checkbox("Enable Slidecast", ref SlideCastEnabled);
        if (!SlideCastEnabled) return;
        ImGui.Checkbox("Show Slidcast Bar Background", ref SlideCastBackground);
        if (ImGui.IsItemHovered()){
            ImGui.BeginTooltip();
            ImGui.Text("Show a colored bar to indicate when a spell has registred succesfully and you can move freely.");
            ImGui.EndTooltip();
        }
        if (SlideCastBackground) {
            ImGui.ColorEdit4("Slidecast Bar Color", ref slideCol, ImGuiColorEditFlags.NoInputs);
        }
        if (!ShowAdvanced) return;
        ImGui.Checkbox("Slidecast Covers End of Bar", ref SlideCastFullBar);
        if (ImGui.IsItemHovered()){
            ImGui.BeginTooltip();
            ImGui.Text("If enabled, colored portion of the slidecast bar will extend to the end of the castbar.");
            ImGui.EndTooltip();
        }
        var SlidecastDelayInt = (int)(SlidecastDelay * 1000);
        ImGui.SliderInt("Slidecast Time (in ms)", ref SlidecastDelayInt, 400, 600);
        SlidecastDelay = SlidecastDelayInt / 1000f;

        ImGui.Checkbox("Show Slidecast Triangles", ref ShowSlidecastTriangles);
        if (ImGui.IsItemHovered()){
            ImGui.BeginTooltip();
            ImGui.Text("If enabled, show a triangle on the bottom of the slidcast indicator");
            ImGui.EndTooltip();
        }
        if (ShowSlidecastTriangles) {
            ImGui.Indent();
            ImGui.Checkbox("Also Show Triangles on Hard Casts", ref ShowTrianglesOnHardCasts);
            if (ImGui.IsItemHovered()){
                ImGui.BeginTooltip();
                ImGui.Text("If enabled, display Slidecast triangle when cast time > recast time.");
                ImGui.EndTooltip();
            }
            ImGui.SliderInt("Triangle Size", ref triangleSize, 0, 12);
            if (ImGui.IsItemHovered()){
                ImGui.BeginTooltip();
                ImGui.Text("Triangle size shared with Queuelock.");
                ImGui.EndTooltip();
            }
            ImGui.Unindent();
        }
    }
}
}