using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;

namespace GCDTracker.Config {
public partial class Configuration {
    //Combo
    public bool ComboEnabled = false;
    [JsonIgnore]
    public bool WindowMoveableCT = false;
    public bool ShowOutOfCombatCT = false;
    public Vector4 ctComboUsed = new(0.431f, 0.431f, 0.431f, 1f);
    public Vector4 ctComboActive = new(1f, 1f, 1f, 1f);
    public Vector2 ctsep = new(23, 23);

    
    public Dictionary<uint, bool> EnabledCTJobs = new() {
        { 1, true },
        { 19, true },
        { 3, true },
        { 21, true },
        { 32, true },
        { 37, true },
        { 26, false },
        { 28, false },
        { 6, false },
        { 24, false },
        { 33, false },
        { 2, true },
        { 20, false },
        { 4, true },
        { 22, true },
        { 29, true },
        { 30, true },
        { 34, true },
        { 7, false },
        { 25, false },
        { 27, true },
        { 35, true },
        { 5, true },
        { 23, false },
        { 31, true },
        { 38, false },
        { 39, true },
        { 40, false },
        { 41, false },
        { 42, false },
    };

    private void DrawComboTrackerConfig() {
        ImGui.Checkbox("Enable ComboTrack", ref ComboEnabled);
        if (ComboEnabled) {
            ImGui.Checkbox("Move/resize window", ref WindowMoveableCT);
            if (WindowMoveableCT)
                ImGui.TextDisabled("\tWindow being edited, may ignore further visibility options.");
            ImGui.Checkbox("Show out of combat", ref ShowOutOfCombatCT);
            ImGui.Separator();

            ImGui.ColorEdit4("Actions used color", ref ctComboUsed, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Active combo action color", ref ctComboActive, ImGuiColorEditFlags.NoInputs);
            ImGui.SliderFloat2("Separation betwen actions", ref ctsep, 0, 100);
            ImGui.Separator();

            DrawJobGrid(ref EnabledCTJobs, false);
        }
    }
}
}