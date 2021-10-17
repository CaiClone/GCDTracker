using Dalamud.Configuration;
using Dalamud.Plugin;
using ImGuiNET;
using Newtonsoft.Json;
using System;
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
        public bool WindowLocked;
        public Vector4 backCol; 
        public Vector4 backColBorder;
        public Vector4 frontCol;
        public Vector4 ogcdCol;
        public Vector4 anLockCol;
        public Vector4 clipCol;
        //Combo
        public bool ComboEnabled=true;
        public Vector4 ctComboUsed;
        public Vector4 ctComboActive;
        public int ctxsep;
        public int ctysep;




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
            if (ImGui.TreeNode("GCDWheel"))
            {
                ImGui.ColorEdit4("Background Color", ref backCol);
                ImGui.ColorEdit4("Background border Color", ref backColBorder);
                ImGui.ColorEdit4("Front bar color Color", ref frontCol);
                ImGui.ColorEdit4("Action tick Color", ref ogcdCol);
                ImGui.ColorEdit4("Animation lock Color", ref anLockCol);
                ImGui.ColorEdit4("Clipping Color", ref clipCol);
            }
            else if (ImGui.TreeNode("ComboTrack"))
            {
                ImGui.Checkbox("Combo track enabled", ref ComboEnabled);
                ImGui.ColorEdit4("Abilities used Color", ref ctComboUsed);
                ImGui.ColorEdit4("Active Ability Color", ref ctComboActive);
                ImGui.SliderInt("X Separation", ref ctxsep, 5, 100);
                ImGui.SliderInt("Y Separation", ref ctysep, 5, 100);
            }
            ImGui.End();
        }
    }
}
