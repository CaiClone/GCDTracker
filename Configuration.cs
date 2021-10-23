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
        public bool WindowLocked = false;
        public Vector4 backCol = new Vector4(0.376f, 0.376f, 0.376f, 1);
        public Vector4 backColBorder = new Vector4(0f, 0f, 0f, 1f);
        public Vector4 frontCol = new Vector4(1f, 0.99f, 0.99f, 1f);
        public Vector4 ogcdCol = new Vector4(1f, 0.99f, 0.99f, 1f);
        public Vector4 anLockCol = new Vector4(0.334f, 0.334f, 0.334f, 0.49f);
        public Vector4 clipCol = new Vector4(1f, 0f, 0f, 1f);
        //Combo
        public bool ComboEnabled = true;
        public Vector4 ctComboUsed = new Vector4(0.431f, 0.431f, 0.431f, 1f);
        public Vector4 ctComboActive = new Vector4(1f, 1f, 1f, 1f);
        public Vector2 ctsep = new Vector2(23, 23);

        [JsonIgnore]
        private readonly (uint, string)[] infoJobs = new (uint, string)[]
        {
            (19,"PLD"),
            (21,"WAR"),
            (32,"DRK"),
            (37,"GNB"),
            (28,"SCH"),
            (24,"WHM"),
            (33,"AST"),
            (20,"MNK"),
            (22,"DRG"),
            (30,"NIN"),
            (34,"SAM"),
            (25,"BLM"),
            (27,"SMN"),
            (35,"RDM"),
            (23,"BRD"),
            (31,"MCH"),
            (38,"DNC")
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
            {23,true},
            {31,true},
            {38,false}
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

            ImGui.Checkbox("Lock window", ref WindowLocked);
            if (ImGui.BeginTabBar("GCDConfig")){
                if (ImGui.BeginTabItem("GCDWheel"))
                {
                    ImGui.Checkbox("GCD Wheel enabled", ref WheelEnabled);
                    ImGui.ColorEdit4("Background Color", ref backCol, ImGuiColorEditFlags.NoInputs);
                    ImGui.ColorEdit4("Background border Color", ref backColBorder, ImGuiColorEditFlags.NoInputs);
                    ImGui.ColorEdit4("Front bar color Color", ref frontCol, ImGuiColorEditFlags.NoInputs);
                    ImGui.ColorEdit4("Action tick Color", ref ogcdCol, ImGuiColorEditFlags.NoInputs);
                    ImGui.ColorEdit4("Animation lock Color", ref anLockCol, ImGuiColorEditFlags.NoInputs);
                    ImGui.ColorEdit4("Clipping Color", ref clipCol, ImGuiColorEditFlags.NoInputs);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("ComboTrack"))
                {
                    ImGui.Checkbox("Combo track enabled", ref ComboEnabled);
                    ImGui.ColorEdit4("Abilities used Color", ref ctComboUsed, ImGuiColorEditFlags.NoInputs);
                    ImGui.ColorEdit4("Active Ability Color", ref ctComboActive, ImGuiColorEditFlags.NoInputs);
                    ImGui.SliderFloat2("Separation", ref ctsep, 0, 100);

                    if (ImGui.BeginTable("Classes CT", 3, ImGuiTableFlags.Borders |ImGuiTableFlags.SizingStretchSame))
                    {
                        for (int i = 0; i < infoJobs.Length; i++)
                        {
                            ImGui.TableNextColumn();
                            //var newval =  ImGui.Selectable(infoJobs[i].Item2, oldval);
                            var enabled = EnabledCTJobs[infoJobs[i].Item1];
                            if (ImGui.Checkbox(infoJobs[i].Item2, ref enabled))
                            {
                                EnabledCTJobs[infoJobs[i].Item1] = enabled;
                                PluginLog.Log($"{ComboStore.GetParentJob(infoJobs[i].Item1)},{infoJobs[i].Item1}");
                                EnabledCTJobs[ComboStore.GetParentJob(infoJobs[i].Item1) ?? 0] = enabled;
                            }
                        }
                        ImGui.EndTable();
                    }
                    ImGui.EndTabItem();
                }
            }
            ImGui.End();
        }
    }
}
