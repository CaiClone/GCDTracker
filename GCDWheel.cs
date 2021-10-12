
using Dalamud.Logging;
using ImGuiNET;
using System;
using System.Collections.Generic;

namespace GCDTracker
{
    public class GCDWheel : Module
    {
        public float lastGCDtime;
        public float totalGCD;
        public List<(float,float)> ogcds;
        public GCDWheel() {
            lastGCDtime = 0f;
            totalGCD = 3.5f;
            ogcds = new List<(float,float)>();
        }
        public override unsafe void onActionUse(byte ret,IntPtr actionManager, uint actionType, uint actionID, long targetedActorID, uint param, uint useType, int pvp)
        {
            var animationLock = *(float*) (actionManager + 0x8);
            var isQueued = *(bool*) (actionManager + 0x68);
            var comboTimerPtr = actionManager + 0x60;
            var isGCDRecastActivePtr = actionManager + 0x610;
            var skill = actionManager + 0x614;
            var elapsedGCD = *(float*) (actionManager + 0x618);
            var recastGCD = actionManager + 0x61c;
            var inQueue = *(bool*)(actionManager + 0xBC0);

            if (ret != 1 || animationLock <= 0.001f) return;

            var newGCD = *(float*)recastGCD;
            var ctime = (float)ImGui.GetTime();
            if (newGCD > 0.001f && elapsedGCD<0.001f)
            {
                PluginLog.Log("Weapon");
                //WeaponSkill
                totalGCD = newGCD;
                lastGCDtime = ctime;

                ogcds.Clear();
            }
            ogcds.Add((ctime - lastGCDtime, animationLock));
            /*
            PluginLog.Log($"Hello {lastGCDtime}");
            PluginLog.Log($"Anlock {animationLock}");
            PluginLog.Log($"isQueued {isQueued}");
            PluginLog.Log($"comboTimerPtr {*(float*)comboTimerPtr }");
            PluginLog.Log($"isGCDRecastActivePtr {*(bool*)isGCDRecastActivePtr }");
            PluginLog.Log($"skill {*(int*)skill }");;
            PluginLog.Log($"ActionManager 0x{(ulong)(actionManager):x}");
            PluginLog.Log($"newGCD {newGCD}");
            PluginLog.Log($"elapsedGCD {elapsedGCD}");
            PluginLog.Log($"TotalGCD {totalGCD }"); 
            PluginLog.Log($"ret {ret}");
            PluginLog.Log($"inQueue {inQueue}");
            */

            PluginLog.Log($"-----------------------");
        }
    }
}
