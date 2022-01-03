
using Dalamud.Game;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using GCDTracker.Data;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GCDTracker
{
    public unsafe class GCDWheel
    {
        public float lastGCDtime;
        public float totalGCD;
        public Dictionary<float, (float, bool)> ogcds;

        private DateTime nextAllowedGCDEnd;
        private bool lastActionCast;
        public GCDWheel() {
            lastGCDtime = 0f;
            totalGCD = 3.5f;
            ogcds = new();
            nextAllowedGCDEnd = DateTime.Now;
            lastActionCast = false;
        }

        public unsafe void onActionUse(byte ret,IntPtr actionManager, ActionType actionType, uint actionID, long targetedActorID, uint param, uint useType, int pvp)
        {
            Data.Action* act = DataStore.action;

            var isWeaponSkill = HelperMethods.IsWeaponSkill(actionType, actionID);
            var AddingToQueue = HelperMethods.IsAddingToQueue(actionType, actionID);
            var ExecutingQueued = (act->InQueue && !AddingToQueue);
            if (ret != 1)
            {
                if (ExecutingQueued && Math.Abs(act->ElapsedCastTime-act->TotalCastTime)<0.0001f) ogcds.Clear();
                return;
            }
            if (AddingToQueue) { 
                if (!act->IsCast)
                    ogcds[Math.Max(isWeaponSkill ? act->TotalGCD : 0, act->ElapsedGCD + act->AnimationLock)] = (0.6f,false);
                else
                    ogcds[Math.Max(act->TotalCastTime+ 0.1f,act->TotalGCD)] = (0.6f,false);
            }
            else
            {
                if (isWeaponSkill)
                {
                    totalGCD = act->TotalGCD; //Store it in a variable in order to cache it when it goes back to 0
                    lastGCDtime = (float)ImGui.GetTime();
                    ogcds.Clear();
                    if (act->IsCast)
                    {
                        lastActionCast = true;
                        ogcds[0f] = (0.1f, false);
                        ogcds[act->TotalCastTime] = (0.1f,true);   //0.1f alock exists added after ending cast                 
                    }
                    else
                        ogcds[0f] = (act->AnimationLock,false);
                }
                else if (!ExecutingQueued)
                    ogcds[act->ElapsedGCD] = (act->AnimationLock,false);
            }
        }

        public void Update(Framework framework)
        {
            if (DataStore.clientState.LocalPlayer == null)
                return;
            if (DataStore.action->ElapsedGCD < 0.0001f) //no gcd
            {
                SlideGCDs((float)(framework.UpdateDelta.TotalMilliseconds * 0.001), false);
            }
            else if (Math.Abs(DataStore.action->ElapsedGCD - DataStore.action->TotalGCD) < 0.01f && framework.LastUpdate >= nextAllowedGCDEnd)
            {
                SlideGCDs(DataStore.action->TotalGCD, true);
                nextAllowedGCDEnd = framework.LastUpdate + new TimeSpan(0, 0, 0, 0, 100);
            }
            if (lastActionCast && !HelperMethods.IsCasting()) handleCancelCast();
        }

        private void handleCancelCast()
        {
            lastActionCast = false;
            SlideGCDs(DataStore.action->TotalCastTime, true);
        }

        /// <summary>
        /// This function slides all the GCDs forward by a delta and deletes the ones that reach 0
        /// </summary>
        private void SlideGCDs(float delta, bool isOver)
        {
            var ogcdsNew = new Dictionary<float, (float,bool)>();
            foreach (var (k, (v,vt)) in ogcds)
            {
                if (k < -0.1)
                    ; //remove from dictionary
                else if (k <= delta && v > delta)
                    ogcdsNew[k] = (v - delta,vt);
                else if (k > delta)
                    ogcdsNew[k - delta] = (v, vt);
                else if ((isOver && k + v > totalGCD))
                    ogcdsNew[0] = (k + v - delta, vt);
            }
            ogcds = ogcdsNew;
        }
        public bool DrawGCDWheel(PluginUI ui, Configuration conf)
        {
            float gcdTotal = totalGCD;
            float gcdTime = DataStore.action->ElapsedGCD;
            if (HelperMethods.IsCasting() && DataStore.action->ElapsedCastTime > totalGCD) gcdTime = totalGCD;

            ui.DrawCircSegment(0f, 1f, 6f * ui.Scale, conf.backColBorder); //Background
            ui.DrawCircSegment(0f, 1f, 3f * ui.Scale, conf.backCol);
            ui.DrawCircSegment(0.8f, 1, 9f * ui.Scale, conf.backColBorder); //Queue lock
            ui.DrawCircSegment(0.8f, 1, 6f * ui.Scale, conf.backCol);

            ui.DrawCircSegment(0f, Math.Min(gcdTime / gcdTotal, 1f), 20f * ui.Scale, conf.frontCol);

            foreach (var (ogcd, (anlock,castLock)) in ogcds)
            {
                var isClipping = !castLock && ((ogcd < (gcdTotal - 0.01f) && ogcd + anlock > gcdTotal) || (gcdTime < 0.001f && ogcd < 0.001f && !(anlock % 0.1f < 10e-4)));
                ui.DrawCircSegment(ogcd / gcdTotal, (ogcd + anlock) / gcdTotal, 21f * ui.Scale, (isClipping)? conf.clipCol : conf.anLockCol);
                if (!castLock) ui.DrawCircSegment(ogcd / gcdTotal, (ogcd + 0.04f) / gcdTotal, 23f * ui.Scale, conf.ogcdCol);
            }
            return true;
        }

        public void UpdateAnlock(float oldLock, float newLock)
        {
            if (oldLock == newLock) return; //Ignore autoattacks
            if (ogcds.Count == 0) return;
            if (oldLock == 0) //End of cast
            {
                lastActionCast = false;
                return;
            }
            var ctime = DataStore.action->ElapsedGCD;

            var items = ogcds.Where(x => x.Key <= ctime && ctime < x.Key + x.Value.Item1);
            if (items.Count() == 0) return;
            var item = items.First(); //Should always be one

            ogcds[item.Key] = (ctime - item.Key + newLock,item.Value.Item2);
            var diff = newLock - oldLock;
            var toSlide = ogcds.Where(x => x.Key > ctime).ToList();
            foreach (var ogcd in toSlide)
                ogcds[ogcd.Key + diff] = ogcd.Value;
            foreach (var ogcd in toSlide)
                ogcds.Remove(ogcd.Key);
        }
    }
}
