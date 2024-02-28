using Dalamud.Configuration;
using Dalamud.Logging;
using Dalamud.Plugin;
using GCDTracker.Data;
using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace GCDTracker
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        [JsonIgnore]
        public bool configEnabled;
        //GCDWheel
        public bool WheelEnabled = true;
        [JsonIgnore]
        public bool WindowMoveableGW = false;
        public bool ShowOutOfCombatGW = false;
        public bool ColorClipEnabled = true;
        public bool ClipAlertEnabled = true;
        public int ClipAlertPrecision = 0;
        public Vector4 backCol = new(0.376f, 0.376f, 0.376f, 1);
        public Vector4 backColBorder = new(0f, 0f, 0f, 1f);
        public Vector4 frontCol = new(0.9f, 0.9f, 0.9f, 1f);
        public Vector4 ogcdCol = new(1f, 1f, 1f, 1f);
        public Vector4 anLockCol = new(0.334f, 0.334f, 0.334f, 0.667f);
        public Vector4 clipCol = new(1f, 0f, 0f, 0.667f);
        //Combo
        public bool ComboEnabled = true;
        [JsonIgnore]
        public bool WindowMoveableCT = false;
        public bool ShowOutOfCombatCT = false;
        public Vector4 ctComboUsed = new(0.431f, 0.431f, 0.431f, 1f);
        public Vector4 ctComboActive = new(1f, 1f, 1f, 1f);
        public Vector2 ctsep = new(23, 23);

        // ID Main Class, Name, Supported in GW, Supported in CT
        [JsonIgnore]
        private readonly List<(uint, string,bool,bool)> infoJobs = new() {
            (19,"PLD",true,true),
            (21,"WAR",true,true),
            (32,"DRK",true,true),
            (37,"GNB",true,true),
            (28,"SCH",true,false),
            (24,"WHM",true,false),
            (33,"AST",true,false),
            (20,"MNK",true,false),
            (22,"DRG",true,true),
            (30,"NIN",true,true),
            (34,"SAM",true,true),
            (25,"BLM",true,false),
            (27,"SMN",true,true),
            (35,"RDM",true,true),
            (23,"BRD",true,false),
            (31,"MCH",true,true),
            (38,"DNC",true,false),
            (39,"RPR",true,true),
            (40,"SGE",true,false)
        };

        public Dictionary<uint, bool> EnabledGWJobs = new() {
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
            {40,true}
        };

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
            { 40, false }
        };

        // Add any other properties or methods here.
        [JsonIgnore] private DalamudPluginInterface pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface) => this.pluginInterface = pluginInterface;
        public void Save() => pluginInterface.SavePluginConfig(this);

        public void DrawConfig() {
            if (!configEnabled) return;
            var scale = ImGui.GetIO().FontGlobalScale;
            ImGui.SetNextWindowSizeConstraints(new Vector2(500 * scale, 100 * scale),new Vector2(500 * scale,1000 * scale));
            ImGui.Begin("GCDTracker Settings",ref configEnabled,ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize);

            if (ImGui.BeginTabBar("GCDConfig")){
                if (ImGui.BeginTabItem("GCDWheel")) {
                    ImGui.Checkbox("Enable GCDWheel", ref WheelEnabled);
                    if (WheelEnabled) {
                        ImGui.Checkbox("Move/resize window", ref WindowMoveableGW);
                        if (WindowMoveableGW)
                            ImGui.TextDisabled("\tWindow being edited, may ignore further visibility options.");
                        ImGui.Checkbox("Show out of combat", ref ShowOutOfCombatGW);
                        ImGui.Separator();

                        ImGui.Checkbox("Color wheel on clipped GCD", ref ColorClipEnabled);
                        ImGui.Checkbox("Show clip alert", ref ClipAlertEnabled);
                        if (ClipAlertEnabled) {
                            ImGui.SameLine();
                            ImGui.RadioButton("CLIP", ref ClipAlertPrecision, 0);
                            ImGui.SameLine();
                            ImGui.RadioButton("0.X", ref ClipAlertPrecision, 1);
                            ImGui.SameLine();
                            ImGui.RadioButton("0.XX", ref ClipAlertPrecision, 2);
                        }

                        ImGui.Separator();
                        ImGui.Columns(2);
                        ImGui.ColorEdit4("Background bar color", ref backCol, ImGuiColorEditFlags.NoInputs);
                        ImGui.ColorEdit4("Background border color", ref backColBorder, ImGuiColorEditFlags.NoInputs);
                        ImGui.ColorEdit4("GCD bar color", ref frontCol, ImGuiColorEditFlags.NoInputs);
                        ImGui.NextColumn();
                        ImGui.ColorEdit4("GCD start indicator color", ref ogcdCol, ImGuiColorEditFlags.NoInputs);
                        ImGui.ColorEdit4("Animation lock bar color", ref anLockCol, ImGuiColorEditFlags.NoInputs);
                        ImGui.ColorEdit4("Clipping color", ref clipCol, ImGuiColorEditFlags.NoInputs);
                        ImGui.Columns(1);
                        ImGui.Separator();

                        DrawJobGrid(ref EnabledGWJobs, true);
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("ComboTrack")) {
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
                    ImGui.EndTabItem();
                }
            }
            ImGui.End();
        }

        private void DrawJobGrid(ref Dictionary<uint, bool> enabledDict,bool colorPos) {
            var redCol = ImGui.GetColorU32(new Vector4(1f, 0, 0, 1f));
            ImGui.Text("Enabled jobs:");
            if (ImGui.BeginTable("Job Grid", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchSame)) {
                for (int i = 0; i < infoJobs.Count; i++) {
                    ImGui.TableNextColumn();

                    var enabled = enabledDict[infoJobs[i].Item1];
                    var supported = colorPos ? infoJobs[i].Item3 : infoJobs[i].Item4;
                    if (!supported) ImGui.PushStyleColor(ImGuiCol.Text, redCol);
                    if (ImGui.Checkbox(infoJobs[i].Item2, ref enabled)) {
                        enabledDict[infoJobs[i].Item1] = enabled;
                        enabledDict[HelperMethods.GetParentJob(infoJobs[i].Item1) ?? 0] = enabled;
                    }
                    if (!supported) ImGui.PopStyleColor();
                }
                ImGui.EndTable();
                if(infoJobs.Any(x=>colorPos? !x.Item3: !x.Item4))
                    ImGui.TextColored(new Vector4(1f,0,0,1f), "Jobs in red are not currently supported and may have bugs");
            }
        }
    }
}
