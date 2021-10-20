
using Dalamud.Logging;
using GCDTracker.Data;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace GCDTracker
{
    public unsafe class GCDWheel : Module
    {
        public float lastGCDtime;
        public float totalGCD;
        public List<(float, float)> ogcds;

        public GCDWheel() {
            lastGCDtime = 0f;
            totalGCD = 3.5f;
            ogcds = new List<(float,float)>();
        }
        public unsafe void onActionUse2(byte ret, IntPtr actionManager, uint actionType, uint actionID, long targetedActorID, uint param, uint useType, int pvp)
        {
            PluginLog.Log($"{actionID}->{HelperMethods.GetAdjustedActionId(actionID)}");
            PluginLog.Log($"{actionType}->{HelperMethods.GetRecastGroup(actionType, actionID)}");
            var animationLock = *(float*)(actionManager + 0x8);
            var isQueued = *(bool*)(actionManager + 0x68);
            //var skill = actionManager + 0x614;
            var elapsedGCD = *(float*)(actionManager + 0x618);
            var recastGCD = actionManager + 0x61c;
            //var inQueue = *(bool*)(actionManager + 0xBC0);

            if (ret != 1 || animationLock <= 0.001f) return;

            var newGCD = *(float*)recastGCD;
            var ctime = (float)ImGui.GetTime();
            var actionTime = ctime - lastGCDtime;

            if (newGCD > 0.001f && elapsedGCD < 0.001f)
            {
                //WeaponSkill
                totalGCD = newGCD;
                lastGCDtime = ctime;

                ogcds.Clear();
            }
            var (logcd, lanlock) = ogcds.Count > 0 ? ogcds[ogcds.Count - 1] : (-1, -1);
            //Ignore repeats
            if (Math.Abs(actionTime - logcd) < 0.2f)
                return;
            //Handle Queue
            // PluginLog.Log($"-{param},{isQueued}");
            if (logcd + lanlock > actionTime)
            {
                ogcds.Add((logcd + lanlock, 0.6f));
                //PluginLog.Log($"{param},{useType},{actionType},{logcd},{lanlock},{isQueued}");
            }
            else
                ogcds.Add((ctime - lastGCDtime, animationLock));
        }
        public override unsafe void onActionUse(byte ret,IntPtr actionManager, uint actionType, uint actionID, long targetedActorID, uint param, uint useType, int pvp)
        {
            if (ret != 1) return;
            PluginLog.Log($"{HelperMethods.IsWeaponSkill(actionType, actionID)}");

            if(HelperMethods.IsWeaponSkill(actionType, actionID))
            {
                //Configure gcd
            }
            else
            {

            }
        }

        public bool DrawGCDWheel(PluginUI ui,Configuration conf)
        {
            float gcdTotal = totalGCD;
            float gcdTime = (float)ImGui.GetTime() - lastGCDtime;

            if (gcdTime > gcdTotal * 1.25f)
                return false;

            ui.DrawCircSegment(0f, 1f, 6f * ui.Scale, conf.backColBorder);//Background
            ui.DrawCircSegment(0f, 1f, 3f * ui.Scale, conf.backCol);
            ui.DrawCircSegment(0.8f, 1, 9f * ui.Scale, conf.backColBorder); //Queue lock
            ui.DrawCircSegment(0.8f, 1, 6f * ui.Scale, conf.backCol);

            ui.DrawCircSegment(0f, Math.Min(gcdTime / gcdTotal, 1f), 20f * ui.Scale, conf.frontCol);

            foreach (var (ogcd, anlock) in ogcds)
            {
                ui.DrawCircSegment(ogcd / gcdTotal, (ogcd + anlock) / gcdTotal, 21f * ui.Scale, conf.anLockCol * new Vector4(1, 1, 1, Math.Min(1.5f - ((gcdTime - ogcd) / 3f), 1f)));
                ui.DrawCircSegment(ogcd / gcdTotal, (ogcd + 0.04f) / gcdTotal, 23f * ui.Scale, conf.ogcdCol);
            }
            if (gcdTime > gcdTotal)
                ui.DrawCircSegment(0f, (gcdTime - gcdTotal) / gcdTotal, 21f * ui.Scale, conf.clipCol);
            return true;
        }
    }
}
