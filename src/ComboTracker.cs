using Dalamud.Game;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using GCDTracker.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace GCDTracker
{
    public class ComboTracker
    {
        public List<uint> ComboUsed;

        private DateTime actTime;
        //list because last combo actio might have multiple ids (empowered/not empowered)
        public List<uint> LastComboActionUsed;

        public ComboTracker() {
            ComboUsed = [];
            LastComboActionUsed = [0,0];
        }

        #pragma warning disable RCS1163
        public unsafe void OnActionUse(byte ret, ActionManager* actionManager, ActionType actionType, uint actionID, ulong targetedActorID, uint param, uint useType, int pvp) {
            #pragma warning restore RCS1163
            var comboDict = ComboStore.GetCombos();

            Data.Action* act = DataStore.Action;
            var adjActionID = DataStore.ActionManager->GetAdjustedActionId(actionID);
            var isWeaponSkill = HelperMethods.IsWeaponSkill(actionType, adjActionID);
            var addingToQueue = HelperMethods.IsAddingToQueue(isWeaponSkill, act);
            var executingQueued = act->InQueue && !addingToQueue;
            adjActionID = AdjustDRGCombo(adjActionID);
            actionID = AdjustDRGCombo(actionID);

            if(ret == 1 && isWeaponSkill && (executingQueued || !act->InQueue))
                actTime = new[] {DateTime.Now + TimeSpan.FromMilliseconds(500),actTime}.Max();

            if (comboDict.Count == 0 || executingQueued || ret != 1)
                return;
            //Check continuation of currentCombo
            var cCombo = LastComboActionUsed.Where(comboDict.ContainsKey).Select(x => comboDict[x]).FirstOrDefault();
            bool isContinuation = cCombo?.Contains(adjActionID) == true || cCombo?.Contains(actionID) == true;
            bool isComboStart = comboDict.ContainsKey(adjActionID);
            if (!HelperMethods.IsComboPreserving(adjActionID) || isContinuation || isComboStart) {
                actTime = DateTime.Now + TimeSpan.FromMilliseconds(5000);
                if (!isContinuation) {
                    ComboUsed.Clear();
                    actTime = DateTime.Now + TimeSpan.FromMilliseconds(500);
                }
                LastComboActionUsed = [adjActionID, actionID];
                ComboUsed.Add(adjActionID);
                ComboUsed.Add(actionID);
            }
        }

        public unsafe void Update(IFramework framework) {
            if (DataStore.ClientState.LocalPlayer == null)
                return;
            if (ComboUsed.Count>0 && framework.LastUpdate > actTime && DataStore.Action->ComboTimer <= 0) {
                ComboUsed.Clear();
                LastComboActionUsed = [0,0];
            }
        }

        /// <summary>
        /// Draw lines representing the combos of the current localplayer, such as:
        ///   1 -> 2 -> 3
        ///     \> 4 -> 5
        ///   6 -> 7
        /// Algorithm: Given the combo as (skill, follow ups[]), for example (1,[2,4]),(2,[3]),(4,[5]),(6,[7]), pictured Above.
        ///  - We first draw the lines, drawing one line for each follow up in each node, while storing the positions.
        ///  - Reuse the stored position if it exists
        ///  - finally draw action nodes on each of the stored positions
        ///</summary>
        public unsafe void DrawComboLines(PluginUI ui, Configuration conf) {
            var xsep = conf.ctsep.X * ui.Scale;
            var ysep = conf.ctsep.Y* ui.Scale;
            var circRad = 8 * ui.Scale;

            var combos = ComboStore.GetCombos();
            var nodepos = new Dictionary<uint, Vector2>();

            var startpos = ui.w_cent - (ui.w_size/3);
            foreach (var (node, follows) in combos.OrderBy(x => x.Key)) {
                if (!nodepos.TryGetValue(node, out Vector2 cpos)) {
                    startpos += new Vector2(0, ysep); //New combo, advance position
                    cpos = startpos;
                    nodepos.Add(node, cpos);
                }

                var followPositions = GetFollowupPos(cpos, follows.Count, xsep, ysep, circRad);
                if (followPositions.Length > 1) startpos += new Vector2(0, ysep);
                for (int i = 0; i < followPositions.Length; i++) {
                    ui.DrawConnectingLine(cpos, followPositions[i], circRad);
                    nodepos.Add(follows[i], followPositions[i]);
                }
            }
            var lastAction = DataStore.Action->ComboID;
            lastAction = AdjustDRGCombo(lastAction);
            foreach (var (actionId, pos) in nodepos)
                ui.DrawActionCircle(pos, circRad, actionId, lastAction);
        }

        private static Vector2[] GetFollowupPos(Vector2 cpos, int nChild,float xsep, float ysep, float circRad) {
            Vector2[] positions = new Vector2[nChild];
            for(int i = 0; i < nChild; i++)
                positions[i] = cpos + new Vector2(xsep + (circRad * 2), ysep * (i==2? -1:i));
            return positions;
        }

        private uint AdjustDRGCombo(uint actionID) {
            // Hardcoded DRG Wheeling Thrust -> Fang and Claw, if done after Chaos Thrust we add +500 to the actionID
            if ((actionID == 3556 || actionID == 3554) && ComboUsed.Contains(88)) {
                return actionID + 500;
            }
            return actionID;
        }
    }
}
