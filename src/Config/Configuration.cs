using Dalamud.Configuration;
using Dalamud.Interface;
using Dalamud.Plugin;
using GCDTracker.Data;
using GCDTracker.UI;
using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace GCDTracker.Config {
    [Serializable]
    public partial class Configuration : IPluginConfiguration
    {
        [JsonIgnore]
        public int LastVersion = 6;
        public int Version { get; set; } = 6;

        [JsonIgnore]
        public bool configEnabled;

        //Common
        public bool ShowAdvanced = false;
        public bool HideAlertsOutOfCombat = true;
        public bool HideIfTP = true; //Not exposed in the UI
        public bool ClipAlertEnabled = true;
        public bool ColorClipEnabled = true;
        public bool abcAlertEnabled = false;
        public bool ColorABCEnabled = false;
        public bool subtlePulses = false;
        [JsonIgnore]
        private bool showResetConfirmation = false;
        public int ClipAlertPrecision = 0;
        public int abcDelay = 10;
        public float ClipTextSize = 0.86f;
        public float abcTextSize = 0.86f;
        public Vector4 clipCol = new(1f, 0f, 0f, 0.667f);
        public Vector4 abcCol = new(1f, .7f, 0f, 0.667f);
        public Vector4 ClipTextColor = new(0.9f, 0.9f, 0.9f, 1f);
        public Vector4 ClipBackColor = new(1f, 0f, 0f, 1f);
        public Vector4 abcTextColor = new(0f, 0f, 0f, 1f);
        public Vector4 abcBackColor = new(1f, .7f, 0f, 1f);

        public bool pulseWheelAtQueue = false;

        //GCDBar
        public bool pulseBarColorAtSlide = false;
        public bool pulseBarWidthAtSlide = false;
        public bool pulseBarHeightAtSlide = false;
        public bool pulseBarColorAtQueue = false;
        public bool pulseBarWidthAtQueue = false;
        public bool pulseBarHeightAtQueue = false;

        public Vector3 QueuePulseCol = new(1f, 1f, 1f);

        public bool OverrideDefaltFont = false;
        public Vector4 slideCol = new(0.6745098f, 0.0f, 0.9882353f, 0.8f);



        //Floating Triangles
        public bool FloatingTrianglesEnable = false;
        public bool SlidecastTriangleEnable = true;
        public bool QueuelockTriangleEnable = true;
        public bool OnlyGreenTriangles = false;
        [JsonIgnore]
        public bool WindowMoveableSQI = false;

        //Deprecated
        public bool ShowOutOfCombatGW = false;
        public bool BarShowOutOfCombat = false;
        public bool BarColorClipEnabled = true;
        public bool BarClipAlertEnabled = true;
        public int BarClipAlertPrecision = 0;
        public float BarClipTextSize = 0.8f;
        public Vector4 BarBackCol = new(0.376f, 0.376f, 0.376f, 0.667f);
        public Vector4 BarFrontCol = new(0.9f, 0.9f, 0.9f, 1f);
        public Vector4 BarOgcdCol = new(1f, 1f, 1f, 1f);
        public Vector4 BarAnLockCol = new(0.334f, 0.334f, 0.334f, 0.667f);
        public Vector4 BarclipCol = new(1f, 0f, 0f, 0.667f);
        public Vector4 BarBackColBorder = new(0f, 0f, 0f, 1f);
        public float BarBorderSize = 2f;

        //MigrationSettings
        public bool Migration4to5 = false;

        [JsonIgnore] private IDalamudPluginInterface pluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface) => this.pluginInterface = pluginInterface;
        public void Migrate() {
            if (Version == LastVersion) return;
            GCDTracker.Log.Warning($"Migrating config from version {Version} to version {LastVersion}");
            while (Version < LastVersion) {
                switch (Version) {
                    case 3:
                        ClipTextColor = frontCol.WithAlpha(1f);
                        ClipBackColor = clipCol.WithAlpha(1f);
                        break;
                    case 4:
                        Migration4to5 = true;
                        break;
                    case 5:
                        BarBorderSizeInt = (int)BarBorderSize;
                        break;
                        
                }
                Version++;
            }
            Save();
        }
        public void Save() => pluginInterface.SavePluginConfig(this);

        public void DrawMigration4to5() {
            ImGui.SetNextWindowSizeConstraints(new Vector2(700, 100), new Vector2(700, 1000));
            ImGui.Begin("GCDTracker Migration", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize);
            ImGui.TextWrapped("Hello! As you might have noticed we have recently added a bar visualization and an \"Always Be Casting\" alert to GCDTracker. We are merging the setting for both Wheel and Bar visualizations and need you to choose which settings you would like to keep.");
            ImGui.TextWrapped(" - If you were using the GCD Wheel, click \"Keep GCD Wheel Settings\"");
            ImGui.TextWrapped(" - If you already configured the GCD Bar and would prefer to keep it, click \"Keep GCD Bar Settings\"");
            ImGui.Spacing();

            if (ImGui.Button("Keep GCD Wheel Settings")) {
                ShowOutOfCombat = ShowOutOfCombatGW;
                Migration4to5 = false;
                Save();
            }

            ImGui.SameLine();

            if (ImGui.Button("Keep GCD Bar Settings")) {
                ShowOutOfCombat = BarShowOutOfCombat;
                ClipAlertEnabled = BarClipAlertEnabled;
                ClipAlertPrecision = BarClipAlertPrecision;
                ClipTextSize = BarClipTextSize;
                backCol = BarBackCol;
                frontCol = BarFrontCol;
                ogcdCol = BarOgcdCol;
                anLockCol = BarAnLockCol;
                clipCol = BarclipCol;
                Migration4to5 = false;
                Save();
            }
            ImGui.End();
        }
        public void DrawConfig(GCDBar bar) {
            if (Migration4to5) DrawMigration4to5();
            if (!configEnabled) return;
            
            var scale = ImGui.GetIO().FontGlobalScale;
            ImGui.SetNextWindowSizeConstraints(new Vector2(500 * scale, 100 * scale),new Vector2(500 * scale,1000 * scale));
            ImGui.Begin("GCDTracker Settings",ref configEnabled, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize);
            if (ImGui.BeginTabBar("GCDConfig")) {
                if (ImGui.BeginTabItem("General")) {
                    DrawGeneralConfig();
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("GCDDisplay")) {
                    DrawGCDDisplayConfig(bar);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("ComboTracker")) {
                    DrawComboTrackerConfig();
                    ImGui.EndTabItem();
                }
            }
            ImGui.End();
        }

        public void ResetToDefault(bool keepBarSize = false) {
            var defaultConfig = new Configuration();

            foreach (var field in typeof(Configuration).GetFields()) {
                if (!field.IsStatic) {
                    // Check if we are keeping the bar size
                    if (keepBarSize && (field.Name == nameof(BarWidthRatio) || field.Name == nameof(BarHeightRatio)))
                        continue; // Skip resetting these fields
                    
                    field.SetValue(this, field.GetValue(defaultConfig));
                }
            }
            Save();
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
