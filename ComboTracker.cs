using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Logging;
using GCDTracker.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace GCDTracker
{
    public unsafe class ComboTracker : Module
    {
        public List<uint> ComboUsed;
        private ClientState cs;

        [StructLayout(LayoutKind.Explicit, Size = 0x8)]
        public struct Combo
        {
            [FieldOffset(0x00)] public float Timer;
            [FieldOffset(0x04)] public uint Action;
        }
        public Combo* combo;
        public ComboTracker(IntPtr ComboPtr, ClientState cs)
        {
            this.ComboUsed = new List<uint>();
            this.combo = (Combo*)ComboPtr;
            this.cs = cs;

        }
        public override unsafe void onActionUse(byte ret, IntPtr actionManager, uint actionType, uint actionID, long targetedActorID, uint param, uint useType, int pvp)
        {
            var elapsedGCD = *(float*)(actionManager + 0x618);
            uint[][] combos;
            if (!ComboStore.COMBOS.TryGetValue(cs.LocalPlayer.ClassJob.Id, out combos) || elapsedGCD> 0.001f || ret!=1)
                return;

            if (combo->Timer > 0 && combo->Action != 0)
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
            if (combo->Timer <= 0)
                ComboUsed.Clear();
        }
    }
}
