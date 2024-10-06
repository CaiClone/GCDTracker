using ImGuiNET;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GCDTracker.Config {
public partial class Configuration {
    public bool WheelEnabled = true;
    [JsonIgnore]
    public bool WindowMoveableGW = false;

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
    private void DrawGCDWheelConfig() {
        ImGui.Checkbox("Enable GCDWheel", ref WheelEnabled);
        if (!WheelEnabled) return;
        ImGui.Checkbox("Move/resize GCDWheel", ref WindowMoveableGW);
        if (WindowMoveableGW)
            ImGui.TextDisabled("\tWindow being edited, may ignore further visibility options.");
        if (ImGui.TreeNodeEx("QueueLock")) {
            DrawQueueLockWheelConfig();
            ImGui.TreePop();
        }
        if (ImGui.TreeNodeEx("GCDWheel Job Setting")) {
            DrawJobGrid(ref EnabledGWJobs, true);
            ImGui.TreePop();
        }
    }
}
}