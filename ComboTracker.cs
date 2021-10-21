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
    public unsafe class ComboTracker : Module
    {
        public List<uint> ComboUsed;

        public ComboTracker()
        {
            this.ComboUsed = new List<uint>();
        }
        public override unsafe void onActionUse(byte ret, IntPtr actionManager, uint actionType, uint actionID, long targetedActorID, uint param, uint useType, int pvp)
        {
            uint[][] combos;
            if (!ComboStore.COMBOS.TryGetValue(DataStore.clientState.LocalPlayer.ClassJob.Id, out combos) || DataStore.action->ElapsedGCD> 0.001f || ret!=1)
                return;

            if (DataStore.combo->Timer > 0 && DataStore.combo->Action != 0 && actionType==1)
            {
                //If combo Start delete previous combo
                if (!combos.Any(x => x.Skip(1).Any(y => y == actionID))){ 
                    ComboUsed.Clear();
                }
                ComboUsed.Add(actionID);
            }
            else
                ComboUsed.Clear();
        }
        public void Update(Framework framework)
        {
            if (DataStore.combo->Timer <= 0)
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
            var xsep = conf.ctxsep * ui.Scale;
            var ysep = conf.ctysep * ui.Scale;
            var circRad = 8 * ui.Scale;

            var combos = ComboStore.GetCombos();
            var nodepos = new Dictionary<uint, Vector2>();

            var startpos = ui.w_cent + new Vector2((ui.w_size.X * 0.3f) + circRad*3, -(3 * ysep) / 2); //assume average 3 combos, hard to know how many beforehand
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
