
using Dalamud.Logging;
using GCDTracker.Data;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace GCDTracker
{
    public unsafe class GCDWheel
    {
        public float lastGCDtime;
        public float totalGCD;
        public Dictionary<float, float> ogcds;

        public GCDWheel() {
            lastGCDtime = 0f;
            totalGCD = 3.5f;
            ogcds = new Dictionary<float, float>();
        }

        public unsafe void onActionUse(byte ret,IntPtr actionManager, uint actionType, uint actionID, long targetedActorID, uint param, uint useType, int pvp)
        {
            Data.Action* act = DataStore.action;
            if (ret != 1 || act->IsCast) return;

            var isWeaponSkill = HelperMethods.IsWeaponSkill(actionType, actionID);
            var AddingToQueue = HelperMethods.IsAddingToQueue();
            var ExecutingQueued = (act->InQueue1 && !AddingToQueue);

            if (AddingToQueue)
                ogcds[isWeaponSkill? Math.Max(act->TotalGCD, act->ElapsedGCD + act->AnimationLock): act->ElapsedGCD+act->AnimationLock] = 0.6f;
            else
            {
                if (isWeaponSkill)
                {
                    totalGCD = act->TotalGCD; //Store it in a variable in order to cache it when it goes back to 0
                    lastGCDtime = (float)ImGui.GetTime();
                    ogcds.Clear();
                    ogcds[act->ElapsedGCD]=act->AnimationLock;
                }
                else if (!ExecutingQueued)
                    ogcds[act->ElapsedGCD]=act->AnimationLock;
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

        public void UpdateAnlock(float oldLock, float newLock)
        {
            if (ogcds.Count == 0) return;
            var ctime = DataStore.action->ElapsedGCD;

            var items = ogcds.Where(x => x.Key < ctime && ctime < x.Key + x.Value);
            if (items.Count() == 0) return;
            var item = items.First();

            ogcds[item.Key] = ctime- item.Key+ newLock;
            var diff = newLock - oldLock;
            foreach(var ogcd in ogcds.Where(x => x.Key > ctime))
            {
                ogcds[ogcd.Key] = ogcd.Value + diff;
            }

        }
    }
}
