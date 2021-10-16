
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
            //var isQueued = *(bool*) (actionManager + 0x68);
            //var skill = actionManager + 0x614;
            var elapsedGCD = *(float*) (actionManager + 0x618);
            var recastGCD = actionManager + 0x61c;
            //var inQueue = *(bool*)(actionManager + 0xBC0);

            if (ret != 1 || animationLock <= 0.001f) return;

            var newGCD = *(float*)recastGCD;
            var ctime = (float)ImGui.GetTime();
            var actionTime = ctime - lastGCDtime;

            if (newGCD > 0.001f && elapsedGCD<0.001f)
            {
                //WeaponSkill
                totalGCD = newGCD;
                lastGCDtime = ctime;

                ogcds.Clear();
            }
            var (logcd, lanlock) = ogcds.Count>0? ogcds[ogcds.Count-1]: (-1,-1);

            //Ignore repeats
            if (Math.Abs(actionTime - logcd) < 0.2f)
                return;
            //Handle Queue
            if (logcd + lanlock > actionTime)
                ogcds.Add((logcd + lanlock, 0.6f));
            else
                ogcds.Add((ctime - lastGCDtime, animationLock));
        }
    }
}
