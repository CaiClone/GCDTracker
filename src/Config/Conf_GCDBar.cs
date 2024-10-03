using GCDTracker.Data;
using GCDTracker.UI;
using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;

namespace GCDTracker.Config {
public partial class Configuration {
    public bool BarEnabled = false;
    [JsonIgnore]
    public bool BarWindowMoveable = false;
    
    //General
    public bool BarRollGCDs = true;
    public bool ShowQueuedSpellNameGCD = false;

    //Display
    public int BarBorderSizeInt = 2;
    public float BarWidthRatio = 0.9f;
    public float BarHeightRatio = 0.5f;
    public float BarGradientMul = 0.175f;
    public float BarBgGradientMul = 0.175f;
    public bool BarHasGradient = false;
    public int BarGradMode = (int)BarGradientMode.Blended;
    public int BarBgGradMode = (int)BarGradientMode.None;
    public Dictionary<uint, bool> EnabledGBJobs = new() {
        {1,true},
        {19,true},
        {3,true},
        {21,true},
        {32,true},
        {37,true},
        {26,true},
        {28,true},
        {6,true},
        {24,true},
        {33,true},
        {2,true},
        {20,true},
        {4,true},
        {22,true},
        {29,true},
        {30,true},
        {34,true},
        {7,true},
        {25,true},
        {27,true},
        {35,true},
        {5,true},
        {23,true},
        {31,true},
        {38,true},
        {39,true},
        {40,true},
        {41,true},
        {42,true},
    };

    //CastBar
    public bool CastBarEnabled = false;
    public bool EnableCastText = true;
    public Vector3 CastBarTextColor = new(1f, 1f, 1f);
    public bool CastBarBoldText = false;
    public bool CastBarShowQueuedSpell = true;
    public float CastBarTextSize = 0.92f;
    public bool CastBarTextOutlineEnabled = true;
    public float OutlineThickness = 1f;
    public bool CastTimeEnabled = true;
    public int castTimePosition = 0;

    private void DrawGCDBarConfig(GCDBar bar) {
        ImGui.Checkbox("Enable GCDBar", ref BarEnabled);
        if (!BarEnabled) return;
        ImGui.Checkbox("Move/resize GCDBar", ref BarWindowMoveable);
        if (BarWindowMoveable) {
            ImGui.TextDisabled("\tWindow being edited, may ignore further visibility options.");
            var barSize = bar.GetBarSize();
            ImGui.TextDisabled($"Current Dimensions (in pixels): {barSize.X}x{barSize.Y}");
        }
        if (ImGui.TreeNodeEx("GCDBar General")) {
            ImGui.Checkbox("Roll GCDs", ref BarRollGCDs);
            if (ImGui.IsItemHovered()){
                ImGui.BeginTooltip();
                ImGui.Text("If enabled abilities that start on the next GCD will always be shown inside the bar, even if it overlaps the current GCD.");
                ImGui.EndTooltip();
            }
            if (ShowAdvanced && EnableCastText) {
                var inBattle = GameState.IsCasting() || GameState.InBattle();
                if (inBattle) ImGui.BeginDisabled();
                ImGui.Checkbox("Show Queued Spell on GCDBar", ref ShowQueuedSpellNameGCD);
                if (ImGui.IsItemHovered()){
                    ImGui.BeginTooltip();
                    ImGui.Text("If enabled, successfuly queued abilities will be shown after an arrow ( -> )");
                    ImGui.EndTooltip();
                }
                if (inBattle) ImGui.EndDisabled();
            }
            ImGui.TreePop();
        }
        if (ImGui.TreeNodeEx("GCDBar Display")) {
            DrawGCDBarDisplaySettings();
            ImGui.TreePop();
        }
        if (ImGui.TreeNodeEx("CastBar")) {
            DrawCastBarSettings();
            ImGui.TreePop();
        }
        if (ImGui.TreeNodeEx("QueueLock")) {
            DrawQueueLockBarConfig();
            ImGui.TreePop();
        }
        if (ImGui.TreeNodeEx("Slidecast")) {
            DrawSlideCastBarConfig();
            ImGui.TreePop();
        }
        if (ImGui.TreeNodeEx("GCDBar Job Settings")) {
            DrawJobGrid(ref EnabledGBJobs, true);
            ImGui.TreePop();
        }
    }

    private void DrawGCDBarDisplaySettings() {
        ImGui.SliderInt("Border size", ref BarBorderSizeInt, 0, 10);
        Vector2 size = new(BarWidthRatio, BarHeightRatio);
        ImGui.SliderFloat2("Width and height ratio", ref size, 0.1f, 1f);
        BarWidthRatio = size.X;
        BarHeightRatio = size.Y;

        if (ShowAdvanced) {
            ImGui.Checkbox("Enable GCDBar Gradient", ref BarHasGradient);
            if (BarHasGradient) {
                ImGui.Indent();
                ImGui.Text("Foreground Gradient Mode: ");
                ImGui.RadioButton("White", ref BarGradMode, 0);
                ImGui.SameLine();
                ImGui.RadioButton("Black", ref BarGradMode, 1);
                ImGui.SameLine();
                ImGui.RadioButton("Blended", ref BarGradMode, 2);
                ImGui.SameLine();
                ImGui.RadioButton("None", ref BarGradMode, 3);
                ImGui.SliderFloat("FG Gradient Intensity", ref BarGradientMul, 0f, 1f);
                ImGui.Text("Background Gradient Mode: ");
                ImGui.RadioButton("White  ", ref BarBgGradMode, 0);
                ImGui.SameLine();
                ImGui.RadioButton("Black ", ref BarBgGradMode, 1);
                ImGui.SameLine();
                ImGui.RadioButton("Blended ", ref BarBgGradMode, 2);
                ImGui.SameLine();
                ImGui.RadioButton("None ", ref BarBgGradMode, 3);
                ImGui.SliderFloat("BG Gradient Intensity", ref BarBgGradientMul, 0f, 1f);
                ImGui.Unindent();
            }
        }
    }


    private void DrawCastBarSettings() {
        ImGui.Checkbox("Enable Castbar", ref CastBarEnabled);
        if (!EnableCastText) return;
        var inBattle = GameState.IsCasting() || GameState.InBattle();
        if (inBattle) ImGui.BeginDisabled();
        ImGui.Checkbox("Enable Spell Name/Time Text", ref EnableCastText);
        if (inBattle) ImGui.EndDisabled();
        ImGui.Indent();
        ImGui.ColorEdit3("Text Color", ref CastBarTextColor, ImGuiColorEditFlags.NoInputs);
        var CastBarTextInt = (int)(CastBarTextSize * 12f);
        ImGui.SliderInt("Name/Time Text Size", ref CastBarTextInt, 6, 18);
        CastBarTextSize = CastBarTextInt / 12f;
        ImGui.Checkbox("Bold", ref CastBarBoldText);

        ImGui.Checkbox("Show Next Spell When Queued", ref CastBarShowQueuedSpell);
        if (ImGui.IsItemHovered()){
            ImGui.BeginTooltip();
            ImGui.Text("If enabled, successfuly queued spells will be shown after an arrow ( -> )");
            ImGui.EndTooltip();
        }
        if (ShowAdvanced) {
            ImGui.Checkbox("Enable Text Outline:", ref CastBarTextOutlineEnabled);
            if (CastBarTextOutlineEnabled) {
                var OutlineThicknessInt = (int)(OutlineThickness * 10f);
                ImGui.SameLine();
                ImGui.RadioButton("Normal", ref OutlineThicknessInt, 10);
                ImGui.SameLine();
                ImGui.RadioButton("Thick", ref OutlineThicknessInt, 12);
                OutlineThickness = OutlineThicknessInt / 10f;
            }
            ImGui.Checkbox("Show remaining cast time:", ref CastTimeEnabled);
            if (CastTimeEnabled) {
                ImGui.SameLine();
                ImGui.RadioButton("Left", ref castTimePosition, 0);
                ImGui.SameLine();
                ImGui.RadioButton("Right", ref castTimePosition, 1);
            }
        }
    }
}
}