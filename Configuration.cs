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
        public bool GCDWheelLocked = true;
        public Vector4 backCol = new Vector4(0.6f, 0.6f, 0.6f, 1f); 
        public Vector4 backColBorder = new Vector4(0.1f, 0.1f, 0.1f, 1f);
        public Vector4 frontCol = new Vector4(0.7f, 0.1f, 0.85f, 1f);
        public Vector4 ogcdCol = new Vector4(1f, 0f, 0f, 1f);
        public Vector4 anLockCol = new Vector4(0.8f, 0f, 0f, 0.6f);
        public Vector4 clipCol = new Vector4(1f, 0f, 0f, 1f);



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


            if (ImGui.TreeNode("GCDWheel"))
            {
                ImGui.Checkbox("Lock GCDWheel", ref GCDWheelLocked);
                ImGui.ColorEdit4("Background Color", ref backCol);
                ImGui.ColorEdit4("Background border Color", ref backColBorder);
                ImGui.ColorEdit4("Front bar color Color", ref frontCol);
                ImGui.ColorEdit4("Action tick Color", ref ogcdCol);
                ImGui.ColorEdit4("Animation lock Color", ref anLockCol);
                ImGui.ColorEdit4("Clipping Color", ref clipCol);
            }
            if (ImGui.TreeNode("ComboTrack"))
            {

            }
            ImGui.End();
        }
    }
}
