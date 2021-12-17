using Dalamud.Game;
using Dalamud.Logging;
using GCDTracker.Data;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace GCDTracker
{
    public unsafe class ComboTracker
    {
        public List<uint> ComboUsed;

        private DateTime actTime;

        public ComboTracker()
        {
            this.ComboUsed = new List<uint>();
        }
        public unsafe void onActionUse(byte ret, IntPtr actionManager, uint actionType, uint actionID, long targetedActorID, uint param, uint useType, int pvp)
        {
            var comboDict = ComboStore.GetCombos();

            Data.Action* act = DataStore.action;
            var cAct = HelperMethods.GetAdjustedActionId(actionID);
            var isWeaponSkill = HelperMethods.IsWeaponSkill(actionType, cAct);
            var AddingToQueue = HelperMethods.IsAddingToQueue(actionType, cAct);
            var ExecutingQueued = (act->InQueue1 && !AddingToQueue);

            if(ret ==1 &&  isWeaponSkill && (ExecutingQueued || !act->InQueue1))
                actTime = DateTime.Now + TimeSpan.FromMilliseconds(500);

            if (comboDict.Count == 0 || ExecutingQueued || ret != 1)
                return;

            if (!HelperMethods.IsComboPreserving(actionID))
            {
                //If it's not any continuation let's first clear the combo
                if (!comboDict.Any(comb => comb.Value.Contains(cAct)))
                    ComboUsed.Clear();
                ComboUsed.Add(cAct);
            }
        }

        public void Update(Framework framework)
        {
            if (ComboUsed.Count>0 && framework.LastUpdate > actTime && DataStore.combo->Timer <= 0)
                ComboUsed.Clear();
        }

        /// <summary>
        /// Draw lines representing the combos of the current localplayer, such as:
        ///   1 -> 2 -> 3
        ///     \> 4 -> 5
        ///   6 -> 7
        /// 
        /// Algorithm: Given the combo as (skill, follow ups[]), for example (1,[2,4]),(2,[3]),(4,[5]),(6,[7]), pictured Above.
        ///  - We first draw the lines, drawing one line for each follow up in each node, while storing the positions.
        ///  - Reuse the stored position if it exists
        ///  - finally draw action nodes on each of the stored positions
        ///</summary>
        public void DrawComboLines(PluginUI ui, Configuration conf)
        {
            var xsep = conf.ctsep.X * ui.Scale;
            var ysep = conf.ctsep.Y* ui.Scale;
            var circRad = 8 * ui.Scale;

            var combos = ComboStore.GetCombos();
            var nodepos = new Dictionary<uint, Vector2>();

            var startpos = ui.w_cent - (ui.w_size/3);
            Vector2 cpos;
            foreach (var (node, follows) in combos)
            {
                if (!nodepos.TryGetValue(node, out cpos)) {
                    startpos += new Vector2(0, ysep); //New combo, advance position
                    cpos = startpos;
                    nodepos.Add(node, cpos);
                }

                var followPositions = getFollowupPos(cpos, follows.Count, xsep, ysep, circRad);
                if (followPositions.Length > 1) startpos += new Vector2(0, ysep);
                for (int i = 0; i < followPositions.Length; i++)
                {
                    ui.DrawConnectingLine(cpos, followPositions[i],circRad);
                    nodepos.Add(follows[i], followPositions[i]);
                }
            }
            foreach ((var actionId,var pos) in nodepos)
            {
                ui.DrawActionCircle(pos,circRad,actionId);
            }
        }

        private Vector2[] getFollowupPos(Vector2 cpos, int nChild,float xsep, float ysep, float circRad)
        {
            Vector2[] positions = new Vector2[nChild];
            for(int i = 0; i < nChild; i++)
            {
                positions[i] = cpos + new Vector2((xsep + circRad * 2), ysep * (i==2? -1:i));
            }
            return positions;
        }
    }
}
