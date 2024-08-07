﻿using Dalamud.Configuration;
using Dalamud.Interface;
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
        [JsonIgnore]
        public int LastVersion = 5;
        public int Version { get; set; } = 5;

        [JsonIgnore]
        public bool configEnabled;

        //Common
        public bool ShowOutOfCombat = false;
        public bool HideAlertsOutOfCombat = false;
        public bool HideIfTP = true; //Not exposed in the UI
        public bool ShowOnlyGCDRunning = false;
        public bool QueueLockEnabled = true;
        public bool ClipAlertEnabled = true;
        public bool ColorClipEnabled = true;
        public bool abcAlertEnabled = false;
        public bool ColorABCEnabled = false;
        public int ClipAlertPrecision = 0;
        public float GCDTimeout = 2f;
        public int abcDelay = 10;
        public float ClipTextSize = 0.86f;
        public float abcTextSize = 0.8f;
        public Vector4 clipCol = new(1f, 0f, 0f, 0.667f);
        public Vector4 abcCol = new(1f, .7f, 0f, 0.667f);
        public Vector4 ClipTextColor = new(0.9f, 0.9f, 0.9f, 1f);
        public Vector4 ClipBackColor = new(1f, 0f, 0f, 1f);
        public Vector4 abcTextColor = new(0f, 0f, 0f, 1f);
        public Vector4 abcBackColor = new(1f, .7f, 0f, 1f);
        public Vector4 backCol = new(0.376f, 0.376f, 0.376f, 1);
        public Vector4 backColBorder = new(0f, 0f, 0f, 1f);
        public Vector4 frontCol = new(0.9f, 0.9f, 0.9f, 1f);
        public Vector4 ogcdCol = new(1f, 1f, 1f, 1f);
        public Vector4 anLockCol = new(0.334f, 0.334f, 0.334f, 0.667f);

        //GCDWheel
        public bool WheelEnabled = true;
        [JsonIgnore]
        public bool WindowMoveableGW = false;

        //GCDBar
        public bool BarEnabled = false;
        [JsonIgnore]
        public bool BarWindowMoveable = false;
        public bool BarRollGCDs = true;
        public float BarBorderSize = 2f;
        public float BarWidthRatio = 0.9f;
        public float BarHeightRatio = 0.5f;
        public Vector4 BarBackColBorder = new(0f, 0f, 0f, 1f);

        //Combo
        public bool ComboEnabled = false;
        [JsonIgnore]
        public bool WindowMoveableCT = false;
        public bool ShowOutOfCombatCT = false;
        public Vector4 ctComboUsed = new(0.431f, 0.431f, 0.431f, 1f);
        public Vector4 ctComboActive = new(1f, 1f, 1f, 1f);
        public Vector2 ctsep = new(23, 23);

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

        //MigrationSettings
        public bool Migration4to5 = false;

        // ID Main Class, Name, Supported in GW, Supported in CT
        [JsonIgnore]
        private readonly List<(uint, string,bool,bool)> infoJobs = [
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
            (40,"SGE",true,false),
            (41,"VPR",true,false),
            (42,"PCT",true,false)
        ];

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
            {40,true},
            {41,true},
            {42,true},
        };

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
        public void DrawConfig() {
            if (Migration4to5) DrawMigration4to5();
            if (!configEnabled) return;
            var scale = ImGui.GetIO().FontGlobalScale;
            ImGui.SetNextWindowSizeConstraints(new Vector2(500 * scale, 100 * scale),new Vector2(500 * scale,1000 * scale));
            ImGui.Begin("GCDTracker Settings",ref configEnabled, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize);

            if (ImGui.BeginTabBar("GCDConfig")){
                if(ImGui.BeginTabItem("GCDTracker")){
                    ImGui.Checkbox("Show out of combat", ref ShowOutOfCombat);
                    ImGui.Checkbox("Show only when GCD running", ref ShowOnlyGCDRunning);
                    ImGui.SliderFloat("GCD Timeout (in seconds)", ref GCDTimeout, .2f, 4f);
                    if (ImGui.IsItemHovered()){
                        ImGui.BeginTooltip();
                        ImGui.Text("Controls the length of the GCD Timeout.");
                        ImGui.EndTooltip();
                    }
                    ImGui.Checkbox("Show queue lock", ref QueueLockEnabled);
                    if (ImGui.IsItemHovered()){
                        ImGui.BeginTooltip();
                        ImGui.Text("If enabled, the wheel background will expand on the timing where you can queue the next GCD.");
                        ImGui.EndTooltip();
                    }
                    ImGui.Checkbox("Hide alerts out of combat", ref HideAlertsOutOfCombat);
                    if (ImGui.IsItemHovered()){
                        ImGui.BeginTooltip();
                        ImGui.Text("If enabled, clip and A-B-C pop up alerts will be hidden outside of combat");
                        ImGui.EndTooltip();
                    }
                    ImGui.Separator();

                    ImGui.Checkbox("Color wheel on ABC failure", ref ColorABCEnabled);
                    if (ImGui.IsItemHovered()){
                        ImGui.BeginTooltip();
                        ImGui.Text("Always Be Casting, highlights when you idle between casts.");
                        ImGui.EndTooltip();
                    }
                    ImGui.Checkbox("Show ABC failure alert", ref abcAlertEnabled);
                    if (abcAlertEnabled) {
                        ImGui.ColorEdit4("ABC text color", ref abcTextColor, ImGuiColorEditFlags.NoInputs);
                        ImGui.SameLine();
                        ImGui.ColorEdit4("ABC background color", ref abcBackColor, ImGuiColorEditFlags.NoInputs);
                    }
                    ImGui.SliderFloat("A-B-C text size", ref abcTextSize, 0.2f, 2f);
                    ImGui.SliderInt("A-B-C alert delay (in milliseconds)", ref abcDelay, 1, 200);
                    if (ImGui.IsItemHovered()){
                        ImGui.BeginTooltip();
                        ImGui.Text("Controls how much delay is allowed between abilities.");
                        ImGui.EndTooltip();
                    }

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

                        ImGui.ColorEdit4("Clip text color", ref ClipTextColor, ImGuiColorEditFlags.NoInputs);
                        ImGui.SameLine();
                        ImGui.ColorEdit4("Clip background color", ref ClipBackColor, ImGuiColorEditFlags.NoInputs);
                    }
                    ImGui.SliderFloat("Clip text size", ref ClipTextSize, 0.2f, 2f);

                    ImGui.Separator();

                    ImGui.Columns(2);
                    ImGui.ColorEdit4("Background bar color", ref backCol, ImGuiColorEditFlags.NoInputs);
                    ImGui.ColorEdit4("Background border color", ref backColBorder, ImGuiColorEditFlags.NoInputs);
                    ImGui.ColorEdit4("GCD bar color", ref frontCol, ImGuiColorEditFlags.NoInputs);
                    ImGui.NextColumn();
                    ImGui.ColorEdit4("GCD start indicator color", ref ogcdCol, ImGuiColorEditFlags.NoInputs);
                    ImGui.ColorEdit4("Animation lock bar color", ref anLockCol, ImGuiColorEditFlags.NoInputs);
                    ImGui.ColorEdit4("Clipping color", ref clipCol, ImGuiColorEditFlags.NoInputs);
                    ImGui.ColorEdit4("ABC failure color", ref abcCol, ImGuiColorEditFlags.NoInputs);
                    ImGui.Columns(1);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("GCDDisplay")) {
                    ImGui.Checkbox("Enable GCDWheel", ref WheelEnabled);
                    if (WheelEnabled) {
                        ImGui.Checkbox("Move/resize GCDWheel", ref WindowMoveableGW);
                        if (WindowMoveableGW)
                            ImGui.TextDisabled("\tWindow being edited, may ignore further visibility options.");
                    }

                        ImGui.Separator();

                    ImGui.Checkbox("Enable GCDBar", ref BarEnabled);
                    if (BarEnabled) {
                        ImGui.Checkbox("Move/resize GCDBar", ref BarWindowMoveable);
                        if (BarWindowMoveable)
                            ImGui.TextDisabled("\tWindow being edited, may ignore further visibility options.");
                        ImGui.Checkbox("Roll GCDs", ref BarRollGCDs);
                        if (ImGui.IsItemHovered()){
                            ImGui.BeginTooltip();
                            ImGui.Text("If enabled abilities that start on the next GCD will always be shown inside the bar, even if it overlaps the current GCD.");
                            ImGui.EndTooltip();
                        }
                        ImGui.SliderFloat("Border size", ref BarBorderSize, 0f, 10f);
                        Vector2 size = new(BarWidthRatio, BarHeightRatio);
                        ImGui.SliderFloat2("Width and height ratio", ref size, 0.1f, 1f);
                        BarWidthRatio = size.X;
                        BarHeightRatio = size.Y;
                    }

                        ImGui.Separator();

                        DrawJobGrid(ref EnabledGWJobs, true);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Combo Tracker")) {
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
