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

namespace GCDTracker
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        [JsonIgnore]
        public int LastVersion = 6;
        public int Version { get; set; } = 6;

        [JsonIgnore]
        public bool configEnabled;

        //Common
        public bool ShowAdvanced = false;
        public bool ShowOutOfCombat = true;
        public bool HideAlertsOutOfCombat = true;
        public bool HideIfTP = true; //Not exposed in the UI
        public bool ShowOnlyGCDRunning = true;
        public bool QueueLockEnabled = true;
        public bool ClipAlertEnabled = true;
        public bool ColorClipEnabled = true;
        public bool abcAlertEnabled = false;
        public bool ColorABCEnabled = false;
        public bool subtlePulses = false;
        [JsonIgnore]
        private bool showResetConfirmation = false;
        public int ClipAlertPrecision = 0;
        public float GCDTimeout = 2f;
        public int abcDelay = 10;
        public float ClipTextSize = 0.86f;
        public float abcTextSize = 0.86f;
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
        public bool pulseWheelAtQueue = false;

        //GCDBar
        public bool BarEnabled = false;
        [JsonIgnore]
        public bool BarWindowMoveable = false;
        public bool pulseBarColorAtSlide = false;
        public bool pulseBarWidthAtSlide = false;
        public bool pulseBarHeightAtSlide = false;
        public bool pulseBarColorAtQueue = false;
        public bool pulseBarWidthAtQueue = false;
        public bool pulseBarHeightAtQueue = false;
        public bool BarQueueLockWhenIdle = true;
        public bool BarQueueLockSlide = false;
        public bool BarRollGCDs = true;
        public bool ShowQueuedSpellNameGCD = false;
        public int BarBorderSizeInt = 2;
        public float BarWidthRatio = 0.9f;
        public float BarHeightRatio = 0.5f;
        public float BarGradientMul = 0.175f;
        public float BarBgGradientMul = 0.175f;
        public bool BarHasGradient = false;
        public int BarGradMode = (int)BarGradientMode.Blended;
        public int BarBgGradMode = (int)BarGradientMode.None;

        public Vector3 QueuePulseCol = new(1f, 1f, 1f);

        //CastBar
        public bool CastBarEnabled = false;
        public bool SlideCastEnabled = true;
        public bool SlideCastFullBar = false;
        public bool SlideCastBackground = false;
        public bool OverrideDefaltFont = false;
        public bool ShowQueuelockTriangles = true;
        public bool ShowSlidecastTriangles = true;
        public bool ShowTrianglesOnHardCasts = true;
        public bool EnableCastText = true;
        public bool CastBarShowQueuedSpell = true;
        public Vector4 slideCol = new(0.6745098f, 0.0f, 0.9882353f, 0.8f);
        public int triangleSize = 6;
        public float SlidecastDelay = 0.5f;
        public float CastBarTextSize = 0.92f;
        public Vector3 CastBarTextColor = new(1f, 1f, 1f);
        public bool CastBarTextOutlineEnabled = true;
        public bool CastTimeEnabled = true;
        public int castTimePosition = 0;
        public float OutlineThickness = 1f;
        public bool CastBarBoldText = false;

        //Combo
        public bool ComboEnabled = false;
        [JsonIgnore]
        public bool WindowMoveableCT = false;
        public bool ShowOutOfCombatCT = false;
        public Vector4 ctComboUsed = new(0.431f, 0.431f, 0.431f, 1f);
        public Vector4 ctComboActive = new(1f, 1f, 1f, 1f);
        public Vector2 ctsep = new(23, 23);

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
        public void DrawConfig(float x_size, float y_size) {
            if (Migration4to5) DrawMigration4to5();
            if (!configEnabled) return;
            var scale = ImGui.GetIO().FontGlobalScale;
            ImGui.SetNextWindowSizeConstraints(new Vector2(500 * scale, 100 * scale),new Vector2(500 * scale,1000 * scale));
            ImGui.Begin("GCDTracker Settings",ref configEnabled, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize);

            if (ImGui.BeginTabBar("GCDConfig")){
                if(ImGui.BeginTabItem("GCDTracker")){
                    ImGui.Checkbox("Show out of combat", ref ShowOutOfCombat);
                    if(ShowOutOfCombat){
                        ImGui.Indent();
                        if(ShowAdvanced){
                        ImGui.Checkbox("Hide alerts out of combat", ref HideAlertsOutOfCombat);
                            if (ImGui.IsItemHovered()){
                                ImGui.BeginTooltip();
                                ImGui.Text("If enabled, clip and A-B-C pop up alerts will be hidden outside of combat");
                                ImGui.EndTooltip();
                            }
                        }
                        ImGui.Checkbox("Show only when GCD running", ref ShowOnlyGCDRunning);
                        ImGui.SliderFloat("GCD Timeout (in seconds)", ref GCDTimeout, .2f, 4f);
                            if (ImGui.IsItemHovered()){
                                ImGui.BeginTooltip();
                                ImGui.Text("Controls the length of the GCD Timeout.");
                                ImGui.EndTooltip();
                            }
                        ImGui.Unindent();
                    }
                    ImGui.Checkbox("Show queue lock", ref QueueLockEnabled);
                    if (ImGui.IsItemHovered()){
                        ImGui.BeginTooltip();
                        ImGui.Text("If enabled, the wheel background will expand on the timing where you can queue the next GCD.");
                        ImGui.EndTooltip();
                    }
                    if (QueueLockEnabled && BarEnabled && ShowAdvanced){
                        ImGui.Indent();
                        ImGui.Checkbox("(Bar Only) Progress Bar Pushes Queue Lock", ref BarQueueLockSlide);
                        ImGui.Checkbox("(Bar Only) Show Queue Lock When Idle", ref BarQueueLockWhenIdle);
                        ImGui.Checkbox("(Bar Only) Show Queue Lock Triangles", ref ShowQueuelockTriangles);
                        if (ImGui.IsItemHovered()){
                            ImGui.BeginTooltip();
                            ImGui.Text("If enabled, show a triangle on the top of the queuelock indicator");
                            ImGui.EndTooltip();
                        }
                        if (ShowQueuelockTriangles)
                            ImGui.SliderInt("Triangle Size", ref triangleSize, 0, 12);
                            if (ImGui.IsItemHovered()){
                                ImGui.BeginTooltip();
                                ImGui.Text("Triangle size shared with (Castbar) Slidelock.");
                                ImGui.EndTooltip();
                            }
                        ImGui.Unindent();
                    }
                    ImGui.Separator();

                    ImGui.Checkbox("Color wheel on A-B-C failure", ref ColorABCEnabled);
                    if (ImGui.IsItemHovered()){
                        ImGui.BeginTooltip();
                        ImGui.Text("Always-Be-Casting, highlights when you idle between casts.");
                        ImGui.EndTooltip();
                    }
                    ImGui.Checkbox("Show A-B-C failure alert", ref abcAlertEnabled);
                    if (abcAlertEnabled) {
                        ImGui.ColorEdit4("A-B-C text color", ref abcTextColor, ImGuiColorEditFlags.NoInputs);
                        ImGui.SameLine();
                        ImGui.ColorEdit4("A-B-C background color", ref abcBackColor, ImGuiColorEditFlags.NoInputs);
                        if(ShowAdvanced) {
                            ImGui.SliderFloat("A-B-C text size", ref abcTextSize, 0.2f, 2f);
                            ImGui.SliderInt("A-B-C alert delay (in ms)", ref abcDelay, 1, 200);
                            if (ImGui.IsItemHovered()){
                                ImGui.BeginTooltip();
                                ImGui.Text("Controls how much delay is allowed between abilities.");
                                ImGui.EndTooltip();
                            }
                        }
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
                        if (ShowAdvanced) {
                            ImGui.SliderFloat("Clip text size", ref ClipTextSize, 0.2f, 2f);
                        }
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
                        if (BarWindowMoveable) {
                            ImGui.TextDisabled("\tWindow being edited, may ignore further visibility options.");
                            ImGui.TextDisabled("\tCurent Dimensions (in pixels): " + ((int)(x_size * BarWidthRatio + 2 * BarBorderSizeInt)).ToString()+ "x" +((int)(y_size * BarHeightRatio + 2 * BarBorderSizeInt)).ToString());
                        }
                        ImGui.Checkbox("Roll GCDs", ref BarRollGCDs);
                        if (ImGui.IsItemHovered()){
                            ImGui.BeginTooltip();
                            ImGui.Text("If enabled abilities that start on the next GCD will always be shown inside the bar, even if it overlaps the current GCD.");
                            ImGui.EndTooltip();
                        }
                        ImGui.SliderInt("Border size", ref BarBorderSizeInt, 0, 10);
                        Vector2 size = new(BarWidthRatio, BarHeightRatio);
                        ImGui.SliderFloat2("Width and height ratio", ref size, 0.1f, 1f);
                        BarWidthRatio = size.X;
                        BarHeightRatio = size.Y;
                        if (ShowAdvanced) {
                            if (EnableCastText) {
                                var inBattle = GameState.IsCasting() || DataStore.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat];
                                if (inBattle) ImGui.BeginDisabled();
                                ImGui.Checkbox("Show Queued Spell on GCDBar", ref ShowQueuedSpellNameGCD);
                                if (ImGui.IsItemHovered()){
                                    ImGui.BeginTooltip();
                                    ImGui.Text("If enabled, successfuly queued abilities will be shown after an arrow ( -> )");
                                    ImGui.EndTooltip();
                                }
                                if (inBattle) ImGui.EndDisabled();
                            }
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

                        ImGui.Separator();

                        DrawJobGrid(ref EnabledGWJobs, true);
                    ImGui.EndTabItem();
                }

                if (BarEnabled){
                    if (ImGui.BeginTabItem("Castbar (BETA)")) {
                        ImGui.Checkbox("Enable Castbar Mode", ref CastBarEnabled);
                        if(CastBarEnabled) {
                            ImGui.Separator();
                            ImGui.Checkbox("Enable Slidecast Functionality", ref SlideCastEnabled);
                            if(SlideCastEnabled) {
                                ImGui.Indent();
                                ImGui.Checkbox("Show Slidcast Bar Background", ref SlideCastBackground);
                                if (ImGui.IsItemHovered()){
                                    ImGui.BeginTooltip();
                                    ImGui.Text("Show a colored bar to indicate when a spell has registred succesfully and you can move freely.");
                                    ImGui.EndTooltip();
                                }
                                if (SlideCastBackground) {
                                    ImGui.ColorEdit4("Slidecast Bar Color", ref slideCol, ImGuiColorEditFlags.NoInputs);
                                }
                                if(ShowAdvanced) {
                                    ImGui.Checkbox("Slidecast Covers End of Bar", ref SlideCastFullBar);
                                    if (ImGui.IsItemHovered()){
                                        ImGui.BeginTooltip();
                                        ImGui.Text("If enabled, colored portion of the slidecast bar will extend to the end of the castbar.");
                                        ImGui.EndTooltip();
                                    }
                                    var SlidecastDelayInt = (int)(SlidecastDelay * 1000);
                                    ImGui.SliderInt("Slidecast Time (in ms)", ref SlidecastDelayInt, 400, 600);
                                    SlidecastDelay = SlidecastDelayInt / 1000f;
                                    ImGui.Checkbox("Show Slidecast Triangles", ref ShowSlidecastTriangles);
                                    if (ImGui.IsItemHovered()){
                                        ImGui.BeginTooltip();
                                        ImGui.Text("If enabled, show a triangle on the bottom of the slidcast indicator");
                                        ImGui.EndTooltip();
                                    }
                                    if (ShowSlidecastTriangles) {
                                        ImGui.Indent();
                                        ImGui.Checkbox("Also Show Triangles on Hard Casts", ref ShowTrianglesOnHardCasts);
                                        if (ImGui.IsItemHovered()){
                                            ImGui.BeginTooltip();
                                            ImGui.Text("If enabled, display Slidecast triangle when cast time > recast time.");
                                            ImGui.EndTooltip();
                                        }
                                        ImGui.SliderInt("Triangle Size", ref triangleSize, 0, 12);
                                        if (ImGui.IsItemHovered()){
                                            ImGui.BeginTooltip();
                                            ImGui.Text("Triangle size shared with Queuelock.");
                                            ImGui.EndTooltip();
                                        }
                                        ImGui.Unindent();
                                    }
                                }
                                ImGui.Unindent();
                            }
                            ImGui.Separator();
                            var inBattle = GameState.IsCasting() || DataStore.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat];
                            if (inBattle) ImGui.BeginDisabled();
                            ImGui.Checkbox("Enable Spell Name/Time Text", ref EnableCastText);
                            if (inBattle) ImGui.EndDisabled();
                            if (EnableCastText) {
                                ImGui.Indent();
                                ImGui.ColorEdit3("Castbar Text Color", ref CastBarTextColor, ImGuiColorEditFlags.NoInputs);
                                ImGui.Checkbox("\"Bold\" Castbar Text", ref CastBarBoldText);
                                ImGui.Checkbox("Show Next Spell When Queued", ref CastBarShowQueuedSpell);
                                if (ImGui.IsItemHovered()){
                                    ImGui.BeginTooltip();
                                    ImGui.Text("If enabled, successfuly queued spells will be shown after an arrow ( -> )");
                                    ImGui.EndTooltip();
                                }
                                var CastBarTextInt = (int)(CastBarTextSize * 12f);
                                ImGui.SliderInt("Spell Name/Time Text Size", ref CastBarTextInt, 6, 18);
                                CastBarTextSize = CastBarTextInt / 12f;
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
                                ImGui.Unindent();
                            }
                        }
                    ImGui.EndTabItem();
                    }

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
                if (ImGui.BeginTabItem("Advanced")) {
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
