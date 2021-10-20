using Dalamud.Game;
using Dalamud.Game.ClientState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GCDTracker.Data
{
    static unsafe class DataStore
    {
        public static Combo* combo;
        public static Action* action;
        public static ClientState clientState;

        public static void Init(SigScanner scanner,ClientState cs)
        {
            var comboPtr = scanner.GetStaticAddressFromSig("48 89 2D ?? ?? ?? ?? 85 C0");
            var ActionManagerPtr = scanner.GetStaticAddressFromSig("E8 ?? ?? ?? ?? 33 C0 E9 ?? ?? ?? ?? 8B 7D 0C");

            combo = (Combo*)comboPtr;
            action = (Action*)ActionManagerPtr;
            clientState = cs;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct Action
    {
        [FieldOffset(0x0)] public void* ActionManager;
        [FieldOffset(0x8)] public float AnimationLock;
        [FieldOffset(0x60)] public float ComboTimer;
        [FieldOffset(0x64)] public uint ComboID;
        [FieldOffset(0x68)] public bool InQueue1;
        [FieldOffset(0x68)] public bool InQueue2;
        [FieldOffset(0x70)] public uint QueuedAction;
        //[FieldOffset(0x98)] public float dunno; //always 2.01 when queuing stuff
        [FieldOffset(0x810)] public float AnimationTimer;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x14)]
    public unsafe struct RecastTimer
    {
        [FieldOffset(0x0)] public byte IsActive;
        [FieldOffset(0x4)] public uint ActionID;
        [FieldOffset(0x8)] public float Elapsed;
        [FieldOffset(0xC)] public float Total;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x8)]
    public struct Combo
    {
        [FieldOffset(0x00)] public float Timer;
        [FieldOffset(0x04)] public uint Action;
    }
}
