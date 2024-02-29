
using Dalamud.Game;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using GCDTracker.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tests")]
namespace GCDTracker
{
    public unsafe class GCDWheel
    {
        public float TotalGCD;
        public Dictionary<float, (float, bool)> ogcds;

        private DateTime lastGCDEnd;
        private float lastElapsedGCD;
        private bool lastActionCast;
        private float lastClipDelta;
        private bool clippedGCD;
        private bool checkClip;

        public GCDWheel() {
            TotalGCD = 3.5f;
            ogcds = new();
            lastGCDEnd = DateTime.Now;
            lastActionCast = false;
            lastClipDelta = 0f;
            clippedGCD = false;
            checkClip = false;
        }

        #pragma warning disable RCS1163
        public void OnActionUse(byte ret, ActionManager* actionManager, ActionType actionType, uint actionID, ulong targetedActorID, uint param, uint useType, int pvp) {
            #pragma warning restore RCS1163
            Data.Action* act = DataStore.Action;

            var isWeaponSkill = HelperMethods.IsWeaponSkill(actionType, actionID);
            var AddingToQueue = HelperMethods.IsAddingToQueue(isWeaponSkill, act);
            var ExecutingQueued = act->InQueue && !AddingToQueue;

            if (ret != 1) {
                if (ExecutingQueued && Math.Abs(act->ElapsedCastTime-act->TotalCastTime)<0.0001f)
                    ogcds.Clear();
                return;
            }
            if (AddingToQueue) {
                var IsHigherAbility = act->QueuedAction != actionID && act->QueuedAction != 0;
                if (IsHigherAbility) {
                    //we already added the previous version so skip.
                    return;
                }
                if (!act->IsCast)
                    ogcds[Math.Max(isWeaponSkill ? act->TotalGCD : 0, act->ElapsedGCD + act->AnimationLock)] = (0.64f,false);
                else
                    ogcds[Math.Max(isWeaponSkill ? act->TotalGCD : 0, act->TotalCastTime + 0.1f)] = (0.64f,false);
            } else {
                if (isWeaponSkill) {
                    EndCurrentGCD(TotalGCD);
                    TotalGCD = act->TotalGCD; //Store it in a variable in order to cache it when it goes back to 0
                    if (act->IsCast) {
                        lastActionCast = true;
                        ogcds[0f] = (0.1f, false);
                        ogcds[act->TotalCastTime] = (0.1f,true); //0.1f alock exists added after ending cast                 
                    } else {
                        ogcds[0f] = (act->AnimationLock,false);
                    }
                } else if (!ExecutingQueued) {
                    ogcds[act->ElapsedGCD] = (act->AnimationLock,false);
                }
            }
        }
        public void Update(IFramework framework) {
            if (DataStore.ClientState.LocalPlayer == null)
                return;

            if (lastActionCast && !HelperMethods.IsCasting())
                HandleCancelCast();
            else if (DataStore.Action->ElapsedGCD < lastElapsedGCD)
                EndCurrentGCD(lastElapsedGCD);
            else if (DataStore.Action->ElapsedGCD < 0.0001f)
                SlideGCDs((float)(framework.UpdateDelta.TotalMilliseconds * 0.001), false);
            lastElapsedGCD = DataStore.Action->ElapsedGCD;
        }

        private void HandleCancelCast() {
            lastActionCast = false;
            EndCurrentGCD(DataStore.Action->TotalCastTime);
        }

        /// <summary>
        /// This function slides all the GCDs forward by a delta and deletes the ones that reach 0
        /// </summary>
        internal void SlideGCDs(float delta, bool isOver)
        {
            if (delta <= 0) return; //avoid problem with float precision
            var ogcdsNew = new Dictionary<float, (float,bool)>();
            foreach (var (k, (v,vt)) in ogcds) {
                if (k < -0.1) { } //remove from dictionary
                else if (k < delta && v > delta) {
                    ogcdsNew[k] = (v - delta, vt);
                } else if (k > delta) {
                    ogcdsNew[k - delta] = (v, vt);
                } else if (isOver && k + v > TotalGCD) {
                    ogcdsNew[0] = (k + v - delta, vt);
                    if (k < delta - 0.02f) // Ignore things that are queued or queued + cast end animation lock
                        lastClipDelta = k + v - delta;
                }
            }
            ogcds = ogcdsNew;
        }

        private bool ShouldStartClip() {
            checkClip = false;
            clippedGCD = lastClipDelta > 0.01f;
            return clippedGCD;
        }

        public void DrawGCDWheel(PluginUI ui, Configuration conf) {
            float gcdTotal = TotalGCD;
            float gcdTime = lastElapsedGCD;
            if (HelperMethods.IsCasting() && DataStore.Action->ElapsedCastTime >= gcdTotal && !HelperMethods.IsTeleport(DataStore.Action->CastId))
                gcdTime = gcdTotal;
            if (gcdTotal < 0.1f) return;
            if (checkClip && ShouldStartClip()) {
                ui.StartClip(lastClipDelta);
                lastClipDelta = 0;
            }
            if (clippedGCD && lastGCDEnd + TimeSpan.FromSeconds(4) < DateTime.Now)
                clippedGCD = false;

            var backgroundCol = clippedGCD ? conf.clipCol : conf.backCol;
            ui.DrawCircSegment(0f, 1f, 6f * ui.Scale, conf.backColBorder); //Background
            ui.DrawCircSegment(0f, 1f, 3f * ui.Scale, backgroundCol);

            ui.DrawCircSegment(0.8f, 1, 9f * ui.Scale, conf.backColBorder); //Queue lock
            ui.DrawCircSegment(0.8f, 1, 6f * ui.Scale, backgroundCol);
            ui.DrawClip();

            ui.DrawCircSegment(0f, Math.Min(gcdTime / gcdTotal, 1f), 20f * ui.Scale, conf.frontCol);

            foreach (var (ogcd, (anlock,castLock)) in ogcds) {
                var isClipping = !castLock && DateTime.Now > lastGCDEnd + TimeSpan.FromMilliseconds(50)  &&
                    (
                        (ogcd < (gcdTotal - 0.05f) && ogcd + anlock > gcdTotal) // You will clip next GCD
                        || (gcdTime < 0.001f && ogcd < 0.001f && (anlock > (lastActionCast? 0.125:0.025))) // anlock when no gcdRolling nor CastEndAnimation
                    );
                ui.DrawCircSegment(ogcd / gcdTotal, (ogcd + anlock) / gcdTotal, 21f * ui.Scale, isClipping ? conf.clipCol : conf.anLockCol);
                if (!castLock) ui.DrawCircSegment(ogcd / gcdTotal, (ogcd + 0.04f) / gcdTotal, 23f * ui.Scale, conf.ogcdCol);
            }
        }

        private void EndCurrentGCD(float GCDtime) {
            SlideGCDs(GCDtime, true);
            if (lastElapsedGCD > 0) checkClip = true;
            lastElapsedGCD = DataStore.Action->ElapsedGCD;
            lastGCDEnd = DateTime.Now;
        }

        public void UpdateAnlock(float oldLock, float newLock) {
            if (oldLock == newLock) return; //Ignore autoattacks
            if (ogcds.Count == 0) return;
            if (oldLock == 0) { //End of cast
                lastActionCast = false;
                return;
            }
            var ctime = DataStore.Action->ElapsedGCD;

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
