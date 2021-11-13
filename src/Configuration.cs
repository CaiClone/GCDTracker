using Dalamud.Configuration;
using Dalamud.Logging;
using Dalamud.Plugin;
using GCDTracker.Data;
using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        public bool WindowLockedGW = false;
        public Vector4 backCol = new Vector4(0.376f, 0.376f, 0.376f, 1);
        public Vector4 backColBorder = new Vector4(0f, 0f, 0f, 1f);
        public Vector4 frontCol = new Vector4(0.9f, 0.9f, 0.9f, 1f);
        public Vector4 ogcdCol = new Vector4(1f, 1f, 1f, 1f);
        public Vector4 anLockCol = new Vector4(0.334f, 0.334f, 0.334f, 0.667f);
        public Vector4 clipCol = new Vector4(1f, 0f, 0f, 0.667f);
        //Combo
        public bool ComboEnabled = true;
        public bool WindowLockedCT = false;
        public Vector4 ctComboUsed = new Vector4(0.431f, 0.431f, 0.431f, 1f);
        public Vector4 ctComboActive = new Vector4(1f, 1f, 1f, 1f);
        public Vector2 ctsep = new Vector2(23, 23);

        // ID Main Class, Name, Supported in CT, Supportd in GW
        [JsonIgnore]
        private readonly (uint, string,bool,bool)[] infoJobs = new (uint, string, bool, bool)[]
        {
            (19,"PLD",true,true),
            (21,"WAR",true,true),
            (32,"DRK",true,true),
            (37,"GNB",true,true),
            (28,"SCH",true,false),
            (24,"WHM",true,false),
            (33,"AST",true,false),
            (20,"MNK",true,true),
            (22,"DRG",true,true),
            (30,"NIN",true,true),
            (34,"SAM",true,true),
            (25,"BLM",true,false),
            (27,"SMN",true,false),
            (35,"RDM",true,false),
            (23,"BRD",true,false),
            (31,"MCH",true,true),
            (38,"DNC",true,false)
        };

        public Dictionary<uint,bool> EnabledCTJobs = new()
        {
            {1,true},
            {19,true},
            {3,true},
            {21,true},
            {32,true},
            {37,true},
            {26,false},
            {28,false},
            {6,false},
            {24,false},
            {33,false},
            {2,true},
            {20,true},
            {4,true},
            {22,true},
            {29,true},
            {30,true},
            {34,true},
            {7,false},
            {25,false},
            {27,false},
            {35,false},
            {5,true},
            {23,false},
            {31,true},
            {38,false}
        };
        public Dictionary<uint, bool> EnabledGWJobs = new()
        {
            { 1, true },
            { 19, true },
            { 3, true },
            { 21, true },
            { 32, true },
            { 37, true },
            { 26, true },
            { 28, true },
            { 6, true },
            { 24, true },
            { 33, true },
            { 2, true },
            { 20, true },
            { 4, true },
            { 22, true },
            { 29, true },
            { 30, true },
            { 34, true },
            { 7, true },
            { 25, true },
            { 27, true },
            { 35, true },
            { 5, true },
            { 23, true },
            { 31, true },
            { 38, true }
        };


        // Add any other properties or methods here.
        [JsonIgnore] private DalamudPluginInterface pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface.SavePluginConfig(this);
        }
        public void DrawConfig()
        {
            if (!this.configEnabled) return;
            ImGui.Begin("GCDTracker_Config",ref configEnabled,ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize);

            if (ImGui.BeginTabBar("GCDConfig")){
                if (ImGui.BeginTabItem("GCDWheel"))
                {
                    ImGui.Checkbox("Lock window", ref WindowLockedGW);
                    ImGui.Checkbox("GCD Wheel enabled", ref WheelEnabled);
                    ImGui.ColorEdit4("Background Color", ref backCol, ImGuiColorEditFlags.NoInputs);
                    ImGui.ColorEdit4("Background border Color", ref backColBorder, ImGuiColorEditFlags.NoInputs);
                    ImGui.ColorEdit4("Front bar color Color", ref frontCol, ImGuiColorEditFlags.NoInputs);
                    ImGui.ColorEdit4("Action tick Color", ref ogcdCol, ImGuiColorEditFlags.NoInputs);
                    ImGui.ColorEdit4("Animation lock Color", ref anLockCol, ImGuiColorEditFlags.NoInputs);
                    ImGui.ColorEdit4("Clipping Color", ref clipCol, ImGuiColorEditFlags.NoInputs);

                    DrawJobGrid(ref EnabledGWJobs, true);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("ComboTrack"))
                {
                    ImGui.Checkbox("Lock window", ref WindowLockedCT);
                    ImGui.Checkbox("Combo track enabled", ref ComboEnabled);
                    ImGui.ColorEdit4("Abilities used Color", ref ctComboUsed, ImGuiColorEditFlags.NoInputs);
                    ImGui.ColorEdit4("Active Ability Color", ref ctComboActive, ImGuiColorEditFlags.NoInputs);
                    ImGui.SliderFloat2("Separation", ref ctsep, 0, 100);

                    DrawJobGrid(ref EnabledCTJobs,false);
                    ImGui.EndTabItem();
                }
            }
            ImGui.End();
        }

        private void DrawJobGrid(ref Dictionary<uint, bool> enabledDict,bool colorPos)
        {
            var redCol = ImGui.GetColorU32(new Vector4(1f, 0, 0, 1f));
            if (ImGui.BeginTable("Job Grid", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchSame))
            {
                for (int i = 0; i < infoJobs.Length; i++)
                {
                    ImGui.TableNextColumn();

                    var enabled = enabledDict[infoJobs[i].Item1];
                    var supported = colorPos ? infoJobs[i].Item3 : infoJobs[i].Item4;
                    if (!supported) ImGui.PushStyleColor(ImGuiCol.Text, redCol);
                    if (ImGui.Checkbox(infoJobs[i].Item2, ref enabled))
                    {
                        enabledDict[infoJobs[i].Item1] = enabled;
                        enabledDict[ComboStore.GetParentJob(infoJobs[i].Item1) ?? 0] = enabled;
                    }
                    if (!supported) ImGui.PopStyleColor();
                }
                ImGui.EndTable();
                ImGui.TextColored(new Vector4(1f,0,0,1f), "Jobs in red are not currently supported and may have bugs");
            }
        }
    }
}
