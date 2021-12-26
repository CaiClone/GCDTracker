﻿
using Dalamud.Game;
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
        public Dictionary<float, float> ogcds;

        private DateTime nextAllowedGCDEnd;
        public GCDWheel() {
            lastGCDtime = 0f;
            totalGCD = 3.5f;
            ogcds = new Dictionary<float, float>();
            nextAllowedGCDEnd = DateTime.Now;
        }

        public unsafe void onActionUse(byte ret,IntPtr actionManager, uint actionType, uint actionID, long targetedActorID, uint param, uint useType, int pvp)
        {
            Data.Action* act = DataStore.action;
            if (ret != 1) return;
            
            var isWeaponSkill = HelperMethods.IsWeaponSkill(actionType, actionID);
            var AddingToQueue = HelperMethods.IsAddingToQueue(actionType, actionID);
            var ExecutingQueued = (act->InQueue1 && !AddingToQueue);

            if (AddingToQueue)
                ogcds[Math.Max(isWeaponSkill ? act->TotalGCD:0 , act->ElapsedGCD + act->AnimationLock)] = !act->IsCast? 0.6f:0.01f;
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
                    ogcds[act->ElapsedGCD] = act->AnimationLock;
            }
        }

        public void Update(Framework framework)
        {
            if (DataStore.clientState.LocalPlayer == null)
                return;
            if (DataStore.action->ElapsedGCD < 0.0001f && !HelperMethods.IsCasting()) //no gcd
                SlideGCDs((float)(framework.UpdateDelta.TotalMilliseconds * 0.001), false);
            else if (Math.Abs(DataStore.action->ElapsedGCD - DataStore.action->TotalGCD) < 0.01f && framework.LastUpdate >= nextAllowedGCDEnd && !HelperMethods.IsCasting())
            {
                SlideGCDs(DataStore.action->TotalGCD, true);
                nextAllowedGCDEnd = framework.LastUpdate + new TimeSpan(0, 0, 0, 0, 100);
            }
        }

        /// <summary>
        /// This function slides all the GCDs forward by a delta and deletes the ones that reach 0
        /// </summary>
        private void SlideGCDs(float delta, bool isOver)
        {
            var ogcdsNew = new Dictionary<float, float>();
            foreach (var (k, v) in ogcds)
            {
                if (k < delta && v > delta)
                    ogcdsNew[k] = v - delta;
                else if (isOver && k < delta && k + v > totalGCD)
                    ogcdsNew[0f] = k + v - totalGCD;
                else if (k > delta) 
                    ogcdsNew[k - delta] = v;
            }
            ogcds = ogcdsNew;
        }
        public bool DrawGCDWheel(PluginUI ui, Configuration conf)
        {
            float gcdTotal = totalGCD;
            float gcdTime = DataStore.action->ElapsedGCD;

            ui.DrawCircSegment(0f, 1f, 6f * ui.Scale, conf.backColBorder); //Background
            ui.DrawCircSegment(0f, 1f, 3f * ui.Scale, conf.backCol);
            ui.DrawCircSegment(0.8f, 1, 9f * ui.Scale, conf.backColBorder); //Queue lock
            ui.DrawCircSegment(0.8f, 1, 6f * ui.Scale, conf.backCol);

            ui.DrawCircSegment(0f, Math.Min(gcdTime / gcdTotal, 1f), 20f * ui.Scale, conf.frontCol);

            foreach (var (ogcd, anlock) in ogcds)
            {
                var isClipping = (ogcd < (gcdTotal - 0.01f) && ogcd + anlock > gcdTotal) || (gcdTime < 0.001f && ogcd < 0.001f && !(anlock % 0.1f < 10e-4)) ;
                ui.DrawCircSegment(ogcd / gcdTotal, (ogcd + anlock) / gcdTotal, 21f * ui.Scale, (isClipping)? conf.clipCol : conf.anLockCol);
                ui.DrawCircSegment(ogcd / gcdTotal, (ogcd + 0.04f) / gcdTotal, 23f * ui.Scale, conf.ogcdCol);
            }
            return true;
        }

        public void UpdateAnlock(float oldLock, float newLock)
        {
            if (ogcds.Count == 0) return;
            var ctime = DataStore.action->ElapsedGCD;

            var items = ogcds.Where(x => x.Key <= ctime && ctime < x.Key + x.Value);
            if (items.Count() == 0) return;
            var item = items.First(); //Should always be one

            ogcds[item.Key] = ctime - item.Key + newLock;
            var diff = newLock - oldLock;
            var toSlide = ogcds.Where(x => x.Key > ctime).ToList();
            foreach (var ogcd in toSlide)
                ogcds[ogcd.Key + diff] = ogcd.Value;
            foreach (var ogcd in toSlide)
                ogcds.Remove(ogcd.Key);
        }
    }
}
