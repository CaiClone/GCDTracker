using Dalamud.Game;
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
            var elapsedGCD = *(float*)(actionManager + 0x618);
            uint[][] combos;
            if (!ComboStore.COMBOS.TryGetValue(DataStore.clientState.LocalPlayer.ClassJob.Id, out combos) || elapsedGCD> 0.001f || ret!=1)
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

        public unsafe void DrawComboLines(PluginUI ui, Configuration conf)
        {
            var xsep = conf.ctxsep * ui.Scale;
            var ysep = conf.ctysep * ui.Scale;
            uint[][] combos;
            var circRad = 8 * ui.Scale;

            if (ComboStore.COMBOS.TryGetValue(DataStore.clientState.LocalPlayer.ClassJob.Id, out combos))
            {
                var startPos = ui.w_cent + new Vector2((ui.w_size.X * 0.3f) + circRad + 2, -(combos.Length * ysep) / 2);
                var draw = ImGui.GetBackgroundDrawList();
                for (uint i = 0; i < combos.Length; i++)
                {
                    var combo = combos[i];
                    var cpos = startPos + new Vector2(0, i * ysep);
                    var bifurc = false;
                    for (uint j = 0; j < combo.Length; j++)
                    {
                        if (i > 0 && combos[i - 1][j] == combo[j]) //skip if same start as previous combo
                            bifurc = true;
                        else
                        {
                            if (bifurc) //Draw previous line if it comes from another combo
                            {
                                cpos += new Vector2((xsep + circRad * 2) * j - 1, 0);
                                ui.DrawBackLine(draw, cpos, xsep, ysep, circRad);
                                bifurc = false;
                            }
                            cpos += new Vector2(xsep, 0);
                            ui.DrawActionCircle(draw, cpos, circRad, combo[j]);
                            if (j < (combo.Length - 1))
                                ui.DrawFrontLine(draw, cpos, xsep, ysep, circRad);
                            cpos += new Vector2(circRad * 2, 0);
                        }
                    }
                }
            }
        }
    }
}
