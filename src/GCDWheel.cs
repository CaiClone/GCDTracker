using Dalamud.Game;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using GCDTracker.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("Tests")]
namespace GCDTracker {
    public record AbilityTiming(float AnimationLock, bool IsCasted);

    public unsafe class GCDWheel {
        private readonly Configuration conf;
        private readonly IDataManager dataManager;
        public Dictionary<float, AbilityTiming> ogcds = [];
        public float TotalGCD = 3.5f;
        private DateTime lastGCDEnd = DateTime.Now;

        private float lastElapsedGCD;
        private float lastClipDelta;
        private ulong targetBuffer = 1;

        public int idleTimerAccum;
        public int GCDTimeoutBuffer;
        public bool abcBlocker;
        public bool lastActionTP;

        private bool idleTimerReset = true;
        private bool idleTimerDone;
        private bool lastActionCast;

        private bool clippedGCD;
        private bool checkClip;
        private bool checkABC;
        private bool abcOnThisGCD;
        private bool abcOnLastGCD;
        private bool isRunning;
        private bool isHardCast;
        private float remainingCastTime;
        private string remainingCastTimeString;
        private string queuedAbilityName = " ";
        private string shortCastCachedSpellName = " ";
        private Vector4 bgCache;
        private bool shortCastFinished = false;

        public GCDWheel(Configuration conf, IDataManager dataManager) {
            this.conf = conf;
            this.dataManager = dataManager;
        }

        public void OnActionUse(byte ret, ActionManager* actionManager, ActionType actionType, uint actionID, ulong targetedActorID, uint param, uint useType, int pvp) {
            var act = DataStore.Action;
            var isWeaponSkill = HelperMethods.IsWeaponSkill(actionType, actionID);
            var addingToQueue = HelperMethods.IsAddingToQueue(isWeaponSkill, act) && useType != 1;
            var executingQueued = act->InQueue && !addingToQueue;
            if (ret != 1) {
                if (executingQueued && Math.Abs(act->ElapsedCastTime-act->TotalCastTime) < 0.0001f && isWeaponSkill)
                    ogcds.Clear();
                return;
            }
            //check to make sure that the player is targeting something, so that if they are spamming an action
            //button after the mob dies it won't update the targetBuffer and trigger an ABC
            if (DataStore.ClientState.LocalPlayer?.TargetObject != null)
                targetBuffer = DataStore.ClientState.LocalPlayer.TargetObjectId;
            if (addingToQueue) {
                AddToQueue(act, isWeaponSkill);
                    queuedAbilityName = GetAbilityName(actionID, DataStore.ClientState.LocalPlayer.CastActionType);
            } else {
                if (isWeaponSkill) {
                    EndCurrentGCD(TotalGCD);
                    //Store GCD in a variable in order to cache it when it goes back to 0
                    TotalGCD = act->TotalGCD;
                    AddWeaponSkill(act);
                        queuedAbilityName = GetAbilityName(actionID, DataStore.ClientState.LocalPlayer.CastActionType);
                } else if (!executingQueued) {
                    ogcds[act->ElapsedGCD] = new(act->AnimationLock, false);
                }
            }
        }

        //probably should find a way to do this from DataStore so we aren't passing the world
        //into GCDWheel
        private string GetAbilityName(uint actionID, byte actionType) {
            var lumina = dataManager;

            switch (actionType) {
                    //seem to need case 0 here for follow up casts for short spells (gcdTime>castTime).
                    case 0:
                    case 1:
                    var ability = lumina.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>()?.GetRow(actionID);
                    return ability?.Name;

                    case 2:
                    var item = lumina.GetExcelSheet<Lumina.Excel.GeneratedSheets.Item>()?.GetRow(actionID);
                    return item?.Name;

                    case 13:
                    var mount = lumina.GetExcelSheet<Lumina.Excel.GeneratedSheets.Mount>()?.GetRow(actionID);
                    return CapitalizeOutput(mount?.Singular);
                    
                    default:
                    //so, we're not going to talk about this, and I'm going to deny ever doing it.
                    if (DataStore.ClientState.LocalPlayer.TargetObject.ObjectKind.ToString() == "Aetheryte")
                        return "Attuning...";
                    if (DataStore.ClientState.LocalPlayer.TargetObject.ObjectKind.ToString() == "EventObj")
                        return "Interacting...";
                    if (DataStore.ClientState.LocalPlayer.TargetObject.ObjectKind.ToString() == "EventNpc")
                        return "Interacting...";
                    return "...";
            }
        }

        private string CapitalizeOutput(string input) {
            if (string.IsNullOrEmpty(input))
                return input;

            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            return textInfo.ToTitleCase(input.ToLower());
        }

        private string GetCastbarContents() {
            var AtkStage = FFXIVClientStructs.FFXIV.Component.GUI.AtkStage.Instance();
            byte** stringData = AtkStage->GetStringArrayData()[20][0].StringArray;

            int length = 0;
            byte* currentByte = stringData[0];
            while (currentByte[length] != 0) length++;

            byte[] data = new byte[length];
            for (int i = 0; i < length; i++) data[i] = currentByte[i];

            string result = Encoding.UTF8.GetString(data);
            GCDTracker.Log.Warning($"Cast: {result}");

            return result;
        }

        private void AddToQueue(Data.Action* act, bool isWeaponSkill) {
            var timings = new List<float>() {
                isWeaponSkill ? act->TotalGCD : 0, // Weapon skills
            };
            if (!act->IsCast) {
                // Add OGCDs
                timings.Add(act->ElapsedGCD + act->AnimationLock);
            } else if (act->ElapsedCastTime < act->TotalGCD) {
                // Add Casts
                timings.Add(act->TotalCastTime + 0.1f);
            } else {
                // Add Casts after 1 whole GCD of casting
                timings.Add(act->TotalCastTime - act->ElapsedCastTime + 0.1f);
            }
            ogcds[timings.Max()] = new(0.64f, false);
        }

        private void AddWeaponSkill(Data.Action* act) {
            if (act->IsCast) {
                lastActionCast = true;
                ogcds[0f] = new(0.1f, false);
                ogcds[act->TotalCastTime] = new(0.1f, true);
            } else {
                ogcds[0f] = new(act->AnimationLock, false);
            }
        }

        public void Update(IFramework framework) {
            if (DataStore.ClientState.LocalPlayer == null)
                return;
            CleanFailedOGCDs();
            GCDTimeoutHelper(framework);
            remainingCastTime = DataStore.Action->TotalCastTime - DataStore.Action->ElapsedCastTime;
            remainingCastTimeString = remainingCastTime == 0 ? "" : remainingCastTime.ToString("F1");
            if (lastActionCast && !HelperMethods.IsCasting())
                HandleCancelCast();
            else if (DataStore.Action->ElapsedGCD < lastElapsedGCD)
                EndCurrentGCD(lastElapsedGCD);
            else if (DataStore.Action->ElapsedGCD < 0.0001f)
                SlideGCDs((float)(framework.UpdateDelta.TotalMilliseconds * 0.001), false);
            lastElapsedGCD = DataStore.Action->ElapsedGCD;
        }

        private void CleanFailedOGCDs() {
            if (DataStore.Action->AnimationLock == 0 && ogcds.Count > 0) {
                ogcds = ogcds
                    .Where(x => x.Key > DataStore.Action->ElapsedGCD || x.Key + x.Value.AnimationLock < DataStore.Action->ElapsedGCD)
                    .ToDictionary(x => x.Key, x => x.Value);
            }
        }

        private void GCDTimeoutHelper(IFramework framework) {
            // Determine if we are running
            isRunning = (DataStore.Action->ElapsedGCD != DataStore.Action->TotalGCD) || HelperMethods.IsCasting();
            // Reset idleTimer when we start casting
            if (isRunning && idleTimerReset) {
                idleTimerAccum = 0;
                isHardCast = false;
                idleTimerReset = false;
                idleTimerDone = false;
                abcBlocker = false;
                GCDTimeoutBuffer = (int)(1000 * conf.GCDTimeout);
            }
            if (!isRunning && !idleTimerDone) {
                idleTimerAccum += framework.UpdateDelta.Milliseconds;
                idleTimerReset = true;
            }
            // Handle caster tax
            if (!isHardCast && HelperMethods.IsCasting() && DataStore.Action->TotalCastTime - 0.1f >= DataStore.Action->TotalGCD)
                isHardCast = true;
            checkABC = !abcBlocker && (idleTimerAccum >= (isHardCast ? (conf.abcDelay + 100) : conf.abcDelay));
            // Reset state after the GCDTimeout
            if (idleTimerAccum >= GCDTimeoutBuffer) {
                checkABC = false;
                clippedGCD = false;
                checkClip = false;
                abcOnLastGCD = false;
                abcOnThisGCD = false;
                lastActionTP = false;
                idleTimerDone = true;
            }
        }

        private void HandleCancelCast() {
            lastActionCast = false;
            EndCurrentGCD(DataStore.Action->TotalCastTime);
        }

        /// <summary>
        /// This function slides all the GCDs forward by a delta and deletes the ones that reach 0
        /// </summary>
        internal void SlideGCDs(float delta, bool isOver) {
            if (delta <= 0) return; //avoid problem with float precision
            var ogcdsNew = new Dictionary<float, AbilityTiming>();
            foreach (var (k, (v,vt)) in ogcds) {
                if (k < -0.1) { } //remove from dictionary
                else if (k < delta && v > delta) {
                    ogcdsNew[k] = new(v - delta, vt);
                } else if (k > delta) {
                    ogcdsNew[k - delta] = new(v, vt);
                } else if (isOver && k + v > TotalGCD) {
                    ogcdsNew[0] = new(k + v - delta, vt);
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

        private bool ShouldStartABC() {
            abcBlocker = true;
            // compare cached target object ID at the time of action use to the current target object ID
            return DataStore.ClientState.LocalPlayer.TargetObjectId == targetBuffer;
        }

        private void FlagAlerts(PluginUI ui){
            bool inCombat = DataStore.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat];
            if(conf.ClipAlertEnabled && (!conf.HideAlertsOutOfCombat || inCombat)){
                if (checkClip && ShouldStartClip()) {
                    ui.StartAlert(true, lastClipDelta);
                    lastClipDelta = 0;
                }
            }
            if (conf.abcAlertEnabled && (!conf.HideAlertsOutOfCombat || inCombat)){
                if (!clippedGCD && checkABC && !abcBlocker && ShouldStartABC()) {
                    ui.StartAlert(false, 0);
                    abcOnThisGCD = true;
                }
            }
        }

        private void InvokeAlerts(float relx, float rely, PluginUI ui){
            if (conf.ClipAlertEnabled && clippedGCD)
                ui.DrawAlert(relx, rely, conf.ClipTextSize, conf.ClipTextColor, conf.ClipBackColor, conf.ClipAlertPrecision);
            if (conf.abcAlertEnabled && (abcOnThisGCD || abcOnLastGCD))
                ui.DrawAlert(relx, rely, conf.abcTextSize, conf.abcTextColor, conf.abcBackColor, 3);
           }

        public Vector4 BackgroundColor(){
            var bg = conf.backCol;
            if (conf.ColorClipEnabled && clippedGCD)
                bg = conf.clipCol;
            if (conf.ColorABCEnabled && (abcOnLastGCD || abcOnThisGCD))
                bg = conf.abcCol;
            return bg;
        }

        public void DrawGCDWheel(PluginUI ui) {
            float gcdTotal = TotalGCD;
            float gcdTime = lastElapsedGCD;
            if (conf.ShowOnlyGCDRunning && HelperMethods.IsTeleport(DataStore.Action->CastId)) {
                lastActionTP = true;
                return;
            }
            if (HelperMethods.IsCasting() && DataStore.Action->ElapsedCastTime >= gcdTotal && !HelperMethods.IsTeleport(DataStore.Action->CastId))
                gcdTime = gcdTotal;
            if (gcdTotal < 0.1f) return;
            FlagAlerts(ui);
            InvokeAlerts(0.5f, 0, ui);
            // Background
            ui.DrawCircSegment(0f, 1f, 6f * ui.Scale, conf.backColBorder);
            ui.DrawCircSegment(0f, 1f, 3f * ui.Scale, BackgroundColor());
            if (conf.QueueLockEnabled) {
                ui.DrawCircSegment(0.8f, 1, 9f * ui.Scale, conf.backColBorder);
                ui.DrawCircSegment(0.8f, 1, 6f * ui.Scale, BackgroundColor());
            }
            ui.DrawCircSegment(0f, Math.Min(gcdTime / gcdTotal, 1f), 20f * ui.Scale, conf.frontCol);
            foreach (var (ogcd, (anlock, iscast)) in ogcds) {
                var isClipping = CheckClip(iscast, ogcd, anlock, gcdTotal, gcdTime);
                ui.DrawCircSegment(ogcd / gcdTotal, (ogcd + anlock) / gcdTotal, 21f * ui.Scale, isClipping ? conf.clipCol : conf.anLockCol);
                if (!iscast) ui.DrawCircSegment(ogcd / gcdTotal, (ogcd + 0.04f) / gcdTotal, 23f * ui.Scale, conf.ogcdCol);
            }
        }

        public void DrawGCDBar(PluginUI ui) {
            float gcdTotal = TotalGCD;
            float gcdTime = lastElapsedGCD;

            if (conf.ShowOnlyGCDRunning && HelperMethods.IsTeleport(DataStore.Action->CastId)) {
                lastActionTP = true;
                return;
            }
            if (HelperMethods.IsCasting() && DataStore.Action->ElapsedCastTime >= gcdTotal && !HelperMethods.IsTeleport(DataStore.Action->CastId))
                gcdTime = gcdTotal;
            if (gcdTotal < 0.1f) return;
            FlagAlerts(ui);
            InvokeAlerts((conf.BarWidthRatio + 1) / 2.1f, -0.3f, ui);
            DrawBarElements(ui, false, shortCastFinished, gcdTime / gcdTotal, gcdTime, gcdTotal);

            // Gonna re-do this, but for now, we flag when we need to carryover from the castbar to the GCDBar
            // and dump all the crap here to draw on top. 
            if (shortCastFinished) {
                string abilityNameOutput = shortCastCachedSpellName;
                if (queuedAbilityName != " " && conf.CastBarShowQueuedSpell)
                    abilityNameOutput += " -> " + queuedAbilityName;
                if (!string.IsNullOrEmpty(abilityNameOutput))
                    DrawBarText(ui, abilityNameOutput);
            }

        }

        public void DrawCastBar (PluginUI ui) {
            float gcdTotal = DataStore.Action->TotalGCD;
            float castTotal = DataStore.Action->TotalCastTime;
            float castElapsed = DataStore.Action->ElapsedCastTime;
            float castbarProgress = castElapsed / castTotal;
            float castbarEnd = 1f;
            float slidecastOffset = 0.5f;
            float slidecastStart = Math.Max((castTotal - slidecastOffset) / castTotal, 0f);
            float slidecastEnd = castbarEnd;

            // handle short casts
            if (gcdTotal > castTotal) {
                castbarEnd = castTotal / gcdTotal;
                slidecastStart = Math.Max((castTotal - slidecastOffset) / gcdTotal, 0f);
                slidecastEnd = conf.SlideCastFullBar ? 1f : castbarEnd;
            }

            DrawBarElements(ui, true, gcdTotal > castTotal, castbarProgress * castbarEnd, slidecastStart, slidecastEnd);

            // Text
            // reset the queued name when we start to cast.
            if (castbarProgress <= 0.25f)
                queuedAbilityName = " ";
            if (!string.IsNullOrEmpty(GetCastbarContents())) {
                if (castbarEnd - castbarProgress <= 0.01f && gcdTotal > castTotal) {
                    shortCastFinished = true;
                    shortCastCachedSpellName = GetCastbarContents();
                }
                string abilityNameOutput = GetCastbarContents();
                if (conf.castTimePosition == 0 && conf.CastTimeEnabled)
                    abilityNameOutput += " (" + remainingCastTimeString + ")";
                if (queuedAbilityName != " " && conf.CastBarShowQueuedSpell)
                    abilityNameOutput += " -> " + queuedAbilityName;
                    
                DrawBarText(ui, abilityNameOutput);
            }
        }

        private void DrawBarText(PluginUI ui, string abilityName){
            int barWidth = (int)(ui.w_size.X * conf.BarWidthRatio);
            string combinedText = abilityName + remainingCastTimeString + "!)/|";
            Vector2 spellNamePos = new(ui.w_cent.X - ((float)barWidth / 2.05f), ui.w_cent.Y);
            Vector2 spellTimePos = new(ui.w_cent.X + ((float)barWidth / 2.05f), ui.w_cent.Y);

            // probably better to move the check for conf.EnableCastText to 
            // happen before we query the text contents from the game,
            // but I put it here for now so that we'd be "ready" if someone
            // was playing with the checkbox while casting (e.g. there'd be stuff
            // to draw right away)
            if (conf.EnableCastText) {
                if (!string.IsNullOrEmpty(abilityName))
                    ui.DrawCastBarText(abilityName, combinedText, spellNamePos, conf.CastBarTextSize, false);
                if (!string.IsNullOrEmpty(remainingCastTimeString) && conf.castTimePosition == 1 && conf.CastTimeEnabled)
                    ui.DrawCastBarText(remainingCastTimeString, combinedText, spellTimePos, conf.CastBarTextSize, true);
            }
        }

        public class BarInfo {
            public float CenterX { get; }
            public float CenterY { get; }
            public int Width { get; }
            public int HalfWidth { get; }
            public int RawHalfWidth { get; }
            public int Height { get; }
            public int HalfHeight { get; }
            public int RawHalfHeight { get; }
            public int BorderSize { get; }
            public int HalfBorderSize { get; }
            public int BorderSizeAdj { get; }
            public float BorderWidthPercent { get; }
            public float CurrentPos { get; }
            public float GCDTime_SlidecastStart { get; } 
            public float GCDTotal_SlidecastEnd { get; }
            public int TriangleOffset { get; }
            public bool IsCastBar { get; } 
            public bool IsShortCast { get; } 
            public Vector2 StartVertex { get; }
            public Vector2 EndVertex { get; }
            public Vector2 ProgressVertex { get; }

            public BarInfo(
                float sizeX, 
                float centX, 
                float widthRatio, 
                float sizeY, 
                float centY, 
                float heightRatio, 
                float borderFloat,
                float castBarCurrentPos,
                float gcdTime_slidecastStart, 
                float gcdTotal_slidecastEnd,
                int triangleOffset,
                bool isCastBar, 
                bool isShortCast) {

                CenterX = centX;
                CenterY = centY;
                Width = (int)(sizeX * widthRatio);
                HalfWidth = Width % 2 == 0 ? (Width / 2) : (Width / 2) + 1;
                RawHalfWidth = Width / 2;
                Height = (int)(sizeY * heightRatio);
                HalfHeight = Height % 2 == 0 ? (Height / 2) : (Height / 2) + 1;
                RawHalfHeight = Height / 2;
                BorderSize = (int)borderFloat;
                HalfBorderSize = BorderSize % 2 == 0 ? (BorderSize / 2) : (BorderSize / 2) + 1;
                BorderSizeAdj = BorderSize >= 1 ? BorderSize : 1;
                BorderWidthPercent = (float)BorderSizeAdj / (float)Width;
                CurrentPos = castBarCurrentPos;
                GCDTime_SlidecastStart = gcdTime_slidecastStart;
                GCDTotal_SlidecastEnd = gcdTotal_slidecastEnd;
                TriangleOffset = triangleOffset;
                IsCastBar = isCastBar;
                IsShortCast = isShortCast;
                StartVertex = new(
                    (int)(CenterX - RawHalfWidth), 
                    (int)(CenterY - RawHalfHeight)
                );
                EndVertex = new(
                    (int)(CenterX + HalfWidth), 
                    (int)(CenterY + HalfHeight)
                );
                ProgressVertex = new(
                    (int)(CenterX + ((CurrentPos + ((float)BorderSizeAdj / Width)) * Width) - HalfWidth),
                    (int)(CenterY + HalfHeight)
                );
            }
        }

        private class SlideCastStartVertices {
            public Vector2 TL_C { get; }

            public Vector2 BL_C { get; }
            public Vector2 BL_X { get; }
            public Vector2 BL_Y { get; }

            public Vector2 BR_C { get; }
            public Vector2 BR_X { get; }
            public Vector2 BR_Y { get; }

            public SlideCastStartVertices(BarInfo bar, BarDecisionHelper go) {
                int rightClamp = (int)(bar.CenterX + ((go.Slide_Bar_Start + bar.BorderWidthPercent) * bar.Width) - bar.HalfWidth);
                    rightClamp += bar.TriangleOffset + 1;
                if (rightClamp >= bar.EndVertex.X)
                    rightClamp = (int)bar.EndVertex.X;
                
                TL_C = new(                    
                    (int)(bar.CenterX + (go.Slide_Bar_Start * bar.Width) - bar.HalfWidth),
                    (int)(bar.CenterY - bar.RawHalfHeight)
                );
                
                BL_C = new(
                    (int)(bar.CenterX + (go.Slide_Bar_Start * bar.Width) - bar.HalfWidth),
                    (int)(bar.CenterY + bar.HalfHeight)
                );
                BL_X = new(
                    BL_C.X - bar.TriangleOffset,
                    BL_C.Y
                );
                BL_Y = new(
                    BL_C.X,
                    BL_C.Y - bar.TriangleOffset
                );

                BR_C = new(
                    (int)(bar.CenterX + ((go.Slide_Bar_Start + ((float)bar.BorderSizeAdj / bar.Width)) * bar.Width) - bar.HalfWidth),
                    (int)(bar.CenterY + bar.HalfHeight)
                );
                BR_X = new(
                    rightClamp,
                    BR_C.Y
                );
                BR_Y = new(
                    BR_C.X,
                    BR_C.Y - (bar.TriangleOffset + 1)
                );
            }
        }

        private class SlideCastEndVertices {
            public Vector2 TL_C { get; }

            public Vector2 BL_C { get; }
            public Vector2 BL_X { get; }
            public Vector2 BL_Y { get; }

            public Vector2 BR_C { get; }
            public Vector2 BR_X { get; }
            public Vector2 BR_Y { get; }

            public SlideCastEndVertices(BarInfo bar, BarDecisionHelper go) {
                int rightClamp = (int)(bar.CenterX + ((go.Slide_Bar_End + bar.BorderWidthPercent) * bar.Width) - bar.HalfWidth);
                    rightClamp += bar.TriangleOffset + 1;
                if (rightClamp >= bar.EndVertex.X)
                    rightClamp = (int)bar.EndVertex.X;
                
                TL_C = new(                    
                    (int)(bar.CenterX + (go.Slide_Bar_End * bar.Width) - bar.HalfWidth),
                    (int)(bar.CenterY - bar.RawHalfHeight)
                );
                
                BL_C = new(
                    (int)(bar.CenterX + (go.Slide_Bar_End * bar.Width) - bar.HalfWidth),
                    (int)(bar.CenterY + bar.HalfHeight)
                );
                BL_X = new(
                    BL_C.X - bar.TriangleOffset,
                    BL_C.Y
                );
                BL_Y = new(
                    BL_C.X,
                    BL_C.Y - bar.TriangleOffset
                );

                BR_C = new(
                    (int)(bar.CenterX + ((go.Slide_Bar_End + ((float)bar.BorderSizeAdj / bar.Width)) * bar.Width) - bar.HalfWidth),
                    (int)(bar.CenterY + bar.HalfHeight)
                );
                BR_X = new(
                    rightClamp,
                    BR_C.Y
                );
                BR_Y = new(
                    BR_C.X,
                    BR_C.Y - (bar.TriangleOffset + 1)
                );
            }
        }

        private class QueueLockVertices {
            public Vector2 TL_C { get; }
            public Vector2 TL_X { get; }
            public Vector2 TL_Y { get; }

            public Vector2 TR_C { get; }
            public Vector2 TR_X { get; }
            public Vector2 TR_Y { get; }

            public Vector2 BL_C { get; }
            public Vector2 BL_X { get; }
            public Vector2 BL_Y { get; }

            public Vector2 BR_C { get; }
            public Vector2 BR_X { get; }
            public Vector2 BR_Y { get; }

            public QueueLockVertices(BarInfo bar, BarDecisionHelper go) {
                int rightClamp = (int)(bar.CenterX + ((go.Queue_Lock_Start + bar.BorderWidthPercent) * bar.Width) - bar.HalfWidth);
                    rightClamp += bar.TriangleOffset + 1;
                if (rightClamp >= bar.EndVertex.X)
                    rightClamp = (int)bar.EndVertex.X;
                
                TL_C = new(                    
                    (int)(bar.CenterX + (go.Queue_Lock_Start * bar.Width) - bar.HalfWidth),
                    (int)(bar.CenterY - bar.RawHalfHeight)
                );
                TL_X = new(
                    TL_C.X - bar.TriangleOffset, 
                    TL_C.Y
                );
                TL_Y = new(
                    TL_C.X, 
                    TL_C.Y + bar.TriangleOffset
                );

                TR_C = new(
                    (int)(bar.CenterX + ((go.Queue_Lock_Start + ((float)bar.BorderSizeAdj/ bar.Width)) * bar.Width) - bar.HalfWidth),
                    (int)(bar.CenterY - bar.RawHalfHeight)
                );
                TR_X = new(
                    rightClamp,
                    TR_C.Y
                );
                TR_Y = new(
                    TR_C.X,
                    TR_C.Y + (bar.TriangleOffset + 1)
                );
                
                BL_C = new(
                    (int)(bar.CenterX + (go.Queue_Lock_Start * bar.Width) - bar.HalfWidth),
                    (int)(bar.CenterY + bar.HalfHeight)
                );
                BL_X = new(
                    BL_C.X - bar.TriangleOffset,
                    BL_C.Y
                );
                BL_Y = new(
                    BL_C.X,
                    BL_C.Y - bar.TriangleOffset
                );

                BR_C = new(
                    (int)(bar.CenterX + ((go.Queue_Lock_Start + ((float)bar.BorderSizeAdj / bar.Width)) * bar.Width) - bar.HalfWidth),
                    (int)(bar.CenterY + bar.HalfHeight)
                );
                BR_X = new(
                    rightClamp,
                    BR_C.Y
                );
                BR_Y = new(
                    BR_C.X,
                    BR_C.Y - (bar.TriangleOffset + 1)
                );
            }
        }

        public class BarDecisionHelper {
            private static BarDecisionHelper instance;
            public bool Queue_VerticalBar { get; private set; }
            public bool Queue_TopTriangle { get; private set; }
            public bool Queue_BottomLeftTri { get; private set; }
            public bool Queue_BottomRightTri { get; private set; }
            public bool SlideStart_VerticalBar { get; private set; }
            public bool SlideEnd_VerticalBar { get; private set; }
            public bool SlideStart_LeftTri { get; private set; }
            public bool SlideStart_RightTri { get; private set; }
            public bool SlideEnd_RightTri { get; private set; }
            public bool Slide_Background { get; private set; }
            public float Slide_Bar_Start { get; private set; }
            public float Slide_Bar_End { get; private set; }
            public float Queue_Lock_Start { get; private set;}
            
            private BarDecisionHelper() { }
            public static BarDecisionHelper Instance {
                get {
                    instance ??= new BarDecisionHelper();
                    return instance;
                }
            }

            public void Update(BarInfo bar, Configuration conf, bool isRunning) {                
                if (isRunning) {
                    if (bar.CurrentPos < 0.04f) {
                        if (conf.QueueLockEnabled && (!bar.IsCastBar || bar.IsShortCast)) {
                            Queue_VerticalBar = conf.QueueLockEnabled;
                            Queue_TopTriangle = conf.QueueLockEnabled && conf.ShowQueuelockTriangles;
                            Queue_BottomRightTri = (bar.IsShortCast && !conf.SlideCastFullBar && 
                                conf.ShowSlidecastTriangles && conf.SlideCastEnabled && 
                                ((0.81f - bar.GCDTotal_SlidecastEnd) <= 0.02f)
                            );
                        }
                        if (conf.SlideCastEnabled && bar.IsCastBar) {
                            Slide_Background = true;
                            SlideStart_VerticalBar = true;
                            SlideEnd_VerticalBar = !conf.SlideCastFullBar && ((0.81f - bar.GCDTotal_SlidecastEnd > 0.02f) || !conf.QueueLockEnabled);
                            Slide_Bar_Start = bar.GCDTime_SlidecastStart;
                            SlideStart_LeftTri = conf.ShowSlidecastTriangles;
                            SlideStart_RightTri = conf.SlideCastFullBar && conf.ShowSlidecastTriangles;
                            SlideEnd_RightTri = SlideEnd_VerticalBar && conf.ShowSlidecastTriangles;
                            Slide_Bar_End = conf.SlideCastFullBar ? 1f : bar.GCDTotal_SlidecastEnd; // - borderwidth?
                        }
                    }
                    if (conf.SlideCastEnabled) {
                        if (Slide_Bar_Start < bar.CurrentPos)
                            Slide_Bar_Start = bar.CurrentPos;
                    }
                    if (SlideStart_LeftTri && Slide_Bar_Start >= Slide_Bar_End) {
                        SlideEnd_VerticalBar = false;
                        SlideEnd_RightTri = false;
                        Slide_Background = false;
                        SlideStart_LeftTri = true;
                        SlideStart_RightTri = true;
                    }
                    if (conf.QueueLockEnabled && (SlideStart_LeftTri || SlideStart_RightTri) && Slide_Bar_Start >= 0.8f) {
                        SlideStart_LeftTri = false;
                        SlideStart_RightTri = false;
                        SlideStart_VerticalBar = false;
                        Queue_BottomLeftTri = true;
                        Queue_BottomRightTri = true;
                        Queue_VerticalBar = true;
                    }
                    if (bar.IsCastBar && !bar.IsShortCast) {
                        Queue_VerticalBar = false;
                        Queue_TopTriangle = false;
                        Queue_BottomLeftTri = false;
                        Queue_BottomRightTri = false;
                        SlideEnd_RightTri = false;
                        SlideEnd_VerticalBar = false;
                        SlideStart_VerticalBar = true;
                        SlideStart_VerticalBar = true;
                    }
                    if (bar.IsCastBar && !bar.IsShortCast && conf.ShowSlidecastTriangles) {
                        SlideStart_LeftTri =  conf.ShowTrianglesOnHardCasts;
                        SlideStart_RightTri =  conf.ShowTrianglesOnHardCasts;
                    }
                }

                if (!isRunning || bar.CurrentPos <= 0.02f) {
                    Queue_VerticalBar = conf.BarQueueLockWhenIdle && conf.QueueLockEnabled;
                    Queue_TopTriangle = conf.BarQueueLockWhenIdle && conf.QueueLockEnabled && conf.ShowQueuelockTriangles;
                    Queue_BottomLeftTri = false;
                    Queue_BottomRightTri = false;
                    SlideStart_VerticalBar = false;
                    SlideEnd_VerticalBar = false;
                    SlideStart_LeftTri = false;
                    SlideStart_RightTri = false;
                    SlideEnd_RightTri = false;
                    Slide_Background = false;
                    Slide_Bar_Start = 0f;
                    Slide_Bar_End = 0f;
                }

                Queue_Lock_Start = 0.8f;
                if (conf.QueueLockEnabled && !(!bar.IsShortCast && bar.IsCastBar)) {
                    if ((bar.CurrentPos >= 0.8f) && conf.BarQueueLockSlide)
                        Queue_Lock_Start = bar.CurrentPos;
                        if (bar.CurrentPos >= 1f - bar.BorderWidthPercent) //??
                            Queue_Lock_Start = 1f - bar.BorderWidthPercent; //??
                }
            }
        }

        private void DrawBarElements(PluginUI ui, bool isCastBar, bool isShortCast, float castBarCurrentPos, float gcdTime_slidecastStart, float gcdTotal_slidecastEnd) {
            
            var bar = new BarInfo(
                ui.w_size.X,
                ui.w_cent.X,
                conf.BarWidthRatio,
                ui.w_size.Y,
                ui.w_cent.Y,
                conf.BarHeightRatio,
                conf.BarBorderSize,
                castBarCurrentPos,
                gcdTime_slidecastStart, 
                gcdTotal_slidecastEnd,
                conf.triangleSize,
                isCastBar, 
                isShortCast
            );

            var go = BarDecisionHelper.Instance;
                go.Update(bar, conf, isRunning);

            var sc_sv = new SlideCastStartVertices(bar, go);
            var sc_ev = new SlideCastEndVertices(bar, go);

            var ql_v = new QueueLockVertices(bar, go);

            float barGCDClipTime = 0;
            
            // in both modes:
            // draw the background
            if (!isCastBar)
                bgCache = BackgroundColor();
            if (isCastBar && castBarCurrentPos < 0.25f)
                bgCache = BackgroundColor();
            ui.DrawRectFilledNoAA(bar.StartVertex, bar.EndVertex, bgCache);

            // in both modes:
            // draw cast/gcd progress (main) bar
            if(bar.CurrentPos > 0.001f)
                ui.DrawRectFilledNoAA(bar.StartVertex, bar.ProgressVertex, conf.frontCol);
            
            // in Castbar mode:
            // draw the slidecast bar
            if (conf.SlideCastEnabled){
                DrawSlideCast(ui, sc_sv, sc_ev, go);
                }

            // in GCDBar mode:
            // draw oGCDs and clips
            if (!isCastBar && !(shortCastFinished && conf.HideAnimationLock)) {
                float gcdTime = gcdTime_slidecastStart;
                float gcdTotal = gcdTotal_slidecastEnd;

                foreach (var (ogcd, (anlock, iscast)) in ogcds) {
                    var isClipping = CheckClip(iscast, ogcd, anlock, gcdTotal, gcdTime);
                    float ogcdStart = (conf.BarRollGCDs && gcdTotal - ogcd < 0.2f) ? 0 + barGCDClipTime : ogcd;
                    float ogcdEnd = ogcdStart + anlock;

                    // Ends next GCD
                    if (conf.BarRollGCDs && ogcdEnd > gcdTotal) {
                        ogcdEnd = gcdTotal;
                        barGCDClipTime += ogcdStart + anlock - gcdTotal;
                        //prevent red bar when we "clip" a hard-cast ability
                        if (!isHardCast) {
                            // create end vertex
                            Vector2 clipEndVector = new(
                                (int)(bar.CenterX + ((barGCDClipTime / gcdTotal) * bar.Width) - bar.HalfWidth),
                                (int)(bar.CenterY + bar.HalfHeight)
                            );
                            // Draw the clipped part at the beginning
                            ui.DrawRectFilledNoAA(bar.StartVertex, clipEndVector, conf.clipCol);
                        }
                    }

                    Vector2 oGCDStartVector = new(
                        (int)(bar.CenterX + ((ogcdStart / gcdTotal) * bar.Width) - bar.RawHalfWidth),
                        (int)(bar.CenterY - bar.RawHalfHeight)
                    );
                    Vector2 oGCDEndVector = new(
                        (int)(bar.CenterX + ((ogcdEnd / gcdTotal) * bar.Width) - bar.HalfWidth),
                        (int)(bar.CenterY + bar.RawHalfHeight)
                    );

                    ui.DrawRectFilledNoAA(oGCDStartVector, oGCDEndVector, isClipping ? conf.clipCol : conf.anLockCol);
                    if (!iscast && (!isClipping || ogcdStart > 0.01f)) {
                        Vector2 clipPos = new(
                            bar.CenterX + (ogcdStart / gcdTotal * bar.Width) - bar.RawHalfWidth,
                            bar.CenterY - bar.RawHalfHeight + 1f
                        );
                        ui.DrawRectFilledNoAA(clipPos,
                            clipPos + new Vector2(2f * ui.Scale, bar.Height - 2f),
                            conf.ogcdCol);
                    }
                }
            }

            //in both modes:
            //draw the queuelock (if enabled)
            DrawQueueLock(ui, ql_v, go);

            // in both modes:
            // draw borders
            if (bar.BorderSize > 0) {
                ui.DrawRect(
                    bar.StartVertex - new Vector2(bar.HalfBorderSize, bar.HalfBorderSize),
                    bar.EndVertex + new Vector2(bar.HalfBorderSize, bar.HalfBorderSize),
                    conf.BarBackColBorder, bar.BorderSize);
            }
        }

        private void DrawSlideCast(PluginUI ui, SlideCastStartVertices sc_sv, SlideCastEndVertices sc_ev, BarDecisionHelper go){
            // draw slidecast bar
            if (go.Slide_Background)
                ui.DrawRectFilledNoAA(sc_sv.TL_C, sc_ev.BR_C, conf.slideCol);
            // draw sidecast (start) vertical line
            if (go.SlideStart_VerticalBar)
                ui.DrawRectFilledNoAA(sc_sv.TL_C, sc_sv.BR_C, conf.BarBackColBorder);
            //draw sidlecast (end) vertical line
            if (go.SlideEnd_VerticalBar)
                ui.DrawRectFilledNoAA(sc_ev.TL_C, sc_ev.BR_C, conf.BarBackColBorder);
            //bottom left
            if (go.SlideStart_LeftTri)
                ui.DrawRightTriangle(sc_sv.BL_C, sc_sv.BL_X, sc_sv.BL_Y, conf.BarBackColBorder);
            //bottom right
            if (go.SlideStart_RightTri)
                ui.DrawRightTriangle(sc_sv.BR_C, sc_sv.BR_X, sc_sv.BR_Y, conf.BarBackColBorder);
            //end right
            if (go.SlideEnd_RightTri)
                ui.DrawRightTriangle(sc_ev.BR_C, sc_ev.BR_X, sc_ev.BR_Y, conf.BarBackColBorder);
        }

        private void DrawQueueLock(PluginUI ui, QueueLockVertices ql_v, BarDecisionHelper go) {
            //top triangle
            if (go.Queue_TopTriangle) {
                ui.DrawRightTriangle(ql_v.TL_C, ql_v.TL_X, ql_v.TL_Y, conf.BarBackColBorder);
                ui.DrawRightTriangle(ql_v.TR_C, ql_v.TR_X, ql_v.TR_Y, conf.BarBackColBorder);
            }
            //bottom left triangle
            if(go.Queue_BottomLeftTri)
                ui.DrawRightTriangle(ql_v.BL_C, ql_v.BL_X, ql_v.BL_Y, conf.BarBackColBorder);
            //bottom right triangle
            if (go.Queue_BottomRightTri)
                ui.DrawRightTriangle(ql_v.BR_C, ql_v.BR_X, ql_v.BR_Y, conf.BarBackColBorder); 
            //vertical bar
            if (go.Queue_VerticalBar)
            ui.DrawRectFilledNoAA(ql_v.TL_C, ql_v.BR_C, conf.BarBackColBorder); 
        }

        private bool CheckClip(bool iscast, float ogcd, float anlock, float gcdTotal, float gcdTime) =>
            !iscast && !isHardCast && DateTime.Now > lastGCDEnd + TimeSpan.FromMilliseconds(50)  &&
            (
                (ogcd < (gcdTotal - 0.05f) && ogcd + anlock > gcdTotal) // You will clip next GCD
                || (gcdTime < 0.001f && ogcd < 0.001f && (anlock > (lastActionCast? 0.125:0.025))) // anlock when no gcdRolling nor CastEndAnimation
            );

        private void EndCurrentGCD(float GCDtime) {
            SlideGCDs(GCDtime, true);
            if (lastElapsedGCD > 0 && !isHardCast) checkClip = true;
            lastElapsedGCD = DataStore.Action->ElapsedGCD;
            lastGCDEnd = DateTime.Now;
            //I'm sure there's a better way to accomplish this
            abcOnLastGCD = abcOnThisGCD;
            abcOnThisGCD = false;
            shortCastFinished = false;
        }

        public void UpdateAnlock(float oldLock, float newLock) {
            if (oldLock == newLock) return; //Ignore autoattacks
            if (ogcds.Count == 0) return;
            if (oldLock == 0) { //End of cast
                lastActionCast = false;
                return;
            }
            var ctime = DataStore.Action->ElapsedGCD;

            var items = ogcds.Where(x => x.Key <= ctime && ctime < x.Key + x.Value.AnimationLock);
            if (!items.Any()) return;
            var item = items.First(); //Should always be one

            ogcds[item.Key] = new(ctime - item.Key + newLock, item.Value.IsCasted);
            var diff = newLock - oldLock;
            var toSlide = ogcds.Where(x => x.Key > ctime).ToList();
            foreach (var ogcd in toSlide)
                ogcds[ogcd.Key + diff] = ogcd.Value;
            foreach (var ogcd in toSlide)
                ogcds.Remove(ogcd.Key);
        }
    }
}
