using System.Numerics;
using Dalamud.Configuration;
using ImGuiNET;

namespace GCDTracker.Config {
public partial class Configuration {
    public bool ShowOutOfCombat = true;
    public bool ShowOnlyGCDRunning = true;
    public float GCDTimeout = 2f;

    //Advanced
    public bool ShowAdvanced = false;
    public bool OverrideDefaltFont = false;

    //Alerts Preview
    public bool pulseBarColorAtSlide = false;
    public bool pulseBarWidthAtSlide = false;
    public bool pulseBarHeightAtSlide = false;
    public bool pulseBarColorAtQueue = false;
    public bool pulseBarWidthAtQueue = false;
    public bool pulseBarHeightAtQueue = false;
    public bool pulseWheelAtQueue = false;
    public bool subtlePulses = false;
    public Vector3 QueuePulseCol = new(1f, 1f, 1f);

    

    //Floating Triangles
    public bool FloatingTrianglesEnable = false;
    public bool SlidecastTriangleEnable = true;
    public bool QueuelockTriangleEnable = true;
    public bool OnlyGreenTriangles = false;

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
        ImGui.Checkbox("Show Advanced Configuration Options", ref ShowAdvanced);
        if (ShowAdvanced) {
            ImGui.TextDisabled("\tIn addition to the options below, there are additional options");
            ImGui.TextDisabled("\tin GCDTracker, GCDDisplay, and Castbar.");
            
            ImGui.Checkbox("Override Default Font", ref OverrideDefaltFont);
            if (ImGui.IsItemHovered()){
                ImGui.BeginTooltip();
                ImGui.Text("If enabled, use Monospace font in GCDTracker.");
                ImGui.EndTooltip();
            }

            if (ImGui.TreeNodeEx("Preview Features", ImGuiTreeNodeFlags.DefaultOpen)) {
                ImGui.TextDisabled("Pulse and Floating Triangles are currently in development.");
                ImGui.TextColored(new Vector4(0.9f,0.175f,0.175f,1f), "Please note that these settings will reset once they leave preview.");
                ImGui.NewLine();
                ImGui.Text("Pulse GCDBar @ Slide Lock");
                ImGui.Indent();
                ImGui.Checkbox("Color##slide", ref pulseBarColorAtSlide);
                ImGui.SameLine();
                ImGui.Checkbox("Width##slide", ref pulseBarWidthAtSlide);
                ImGui.SameLine();
                ImGui.Checkbox("Height##slide", ref pulseBarHeightAtSlide);
                ImGui.Unindent();
                ImGui.Text("Pulse GCDBar @ Queue Lock");
                ImGui.Indent();
                ImGui.Checkbox("Color##queue", ref pulseBarColorAtQueue);
                ImGui.SameLine();
                ImGui.Checkbox("Width##queue", ref pulseBarWidthAtQueue);
                ImGui.SameLine();
                ImGui.Checkbox("Height##queue", ref pulseBarHeightAtQueue);
                ImGui.Unindent();
                if (pulseBarColorAtQueue) {
                    ImGui.NewLine();
                    ImGui.ColorEdit3("Queuelock Pulse Color", ref QueuePulseCol);
                }
                ImGui.Text("Pulse GCDWheel @ Queue Lock");
                ImGui.Indent();
                ImGui.Checkbox("Size", ref pulseWheelAtQueue);
                ImGui.Unindent();
                
                ImGui.NewLine();
                ImGui.Checkbox("Reduce Pulse Magnitude (Subtle Pulses)", ref subtlePulses);
                ImGui.NewLine();
                ImGui.Checkbox("Draw Floating Triangles", ref FloatingTrianglesEnable);
                if (FloatingTrianglesEnable){
                    ImGui.Indent();
                    ImGui.Checkbox("Draw Slidecast Triangle", ref SlidecastTriangleEnable);
                    ImGui.SameLine();
                    ImGui.Checkbox("Draw Queuelock Triangle", ref QueuelockTriangleEnable);
                    ImGui.Checkbox("Only Show Trianges When Green", ref OnlyGreenTriangles);
                    ImGui.Unindent();
                    ImGui.Checkbox("Move/resize Triangles", ref WindowMoveableSQI);
                    if (WindowMoveableSQI)
                        ImGui.TextDisabled("\tWindow being edited, may ignore further visibility options.");
                }
                ImGui.TreePop();
            }
        }
        if (ImGui.Button("Reset All Settings to Default"))
            showResetConfirmation = true;
        if (showResetConfirmation)
            ImGui.OpenPopup("Reset Confirmation");
        if (ImGui.BeginPopupModal("Reset Confirmation", ref showResetConfirmation, ImGuiWindowFlags.AlwaysAutoResize)) {
            ImGui.Text("This will reset your settings.\nPlease choose an option:");
            ImGui.Separator();
            if (ImGui.Button("Reset (All Settings)")) {
                ResetToDefault();
                showResetConfirmation = false;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Reset (Keep Bar Size)")) {
                ResetToDefault(keepBarSize: true);
                showResetConfirmation = false;
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("Take Me Back")) {
                showResetConfirmation = false;
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }       
}
}