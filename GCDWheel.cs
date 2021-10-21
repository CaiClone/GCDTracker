
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

        public override unsafe void onActionUse(byte ret,IntPtr actionManager, uint actionType, uint actionID, long targetedActorID, uint param, uint useType, int pvp)
        {
            if (ret != 1) return;
            Data.Action* act = DataStore.action;

            var isWeaponSkill = HelperMethods.IsWeaponSkill(actionType, actionID);
            var AddingToQueue = act->InQueue1 && (
                                    (isWeaponSkill && act->ElapsedGCD < act->TotalGCD && act->ElapsedGCD!=0)
                                    || (!isWeaponSkill && act->AnimationLock < 0.59f));
            var ExecutingQueued = (act->InQueue1 && !AddingToQueue);

            if (AddingToQueue)
                ogcds.Add((isWeaponSkill? act->TotalGCD: act->ElapsedGCD+act->AnimationLock, 0.6f));
            else
            {
                if (isWeaponSkill)
                {
                    totalGCD = act->TotalGCD; //Store it in a variable in order to cache it when it goes back to 0
                    lastGCDtime = (float)ImGui.GetTime();
                    ogcds.Clear();
                    ogcds.Add((act->ElapsedGCD, act->AnimationLock));
                }
                else if (!ExecutingQueued)
                    ogcds.Add((act->ElapsedGCD, act->AnimationLock));
            }
        }

        public bool DrawGCDWheel(PluginUI ui,Configuration conf)
        {
            float gcdTotal = totalGCD;
            float gcdTime = (float)ImGui.GetTime()-lastGCDtime;

            if (gcdTime > gcdTotal * 1.25f)
                return false;

            ui.DrawCircSegment(0f, 1f, 6f * ui.Scale, conf.backColBorder); //Background
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
