using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel;
using Dalamud.Data;
using System.Linq;

namespace GCDTracker.Data
{
    static unsafe class DataStore
    {
        public static Combo* combo;
        public static Action* action;
        public static ActionManager* actionManager;
        public static ClientState clientState;
        public static Condition condition;
        public static ExcelSheet<Lumina.Excel.GeneratedSheets.Action> ActionSheet;
        public static ExcelSheet<Lumina.Excel.GeneratedSheets.ClassJob> ClassSheet;

        public static Dictionary<int, bool> ComboPreserving;
        public static void Init(DataManager data, SigScanner scanner,ClientState cs,Condition cond)
        {
            ActionSheet = data.Excel.GetSheet<Lumina.Excel.GeneratedSheets.Action>();
            ClassSheet = data.Excel.GetSheet<Lumina.Excel.GeneratedSheets.ClassJob>();

            var comboPtr = scanner.GetStaticAddressFromSig("48 89 2D ?? ?? ?? ?? 85 C0");
            actionManager = ActionManager.Instance();

            ComboPreserving = ActionSheet.Where(row => row.PreservesCombo).ToDictionary(row => (int)row.RowId, row => true);

            combo = (Combo*)comboPtr;
            action = (Action*)actionManager;
            clientState = cs;
            condition = cond;
        }
        
        /*
        * Dict of manual changes to combo dict with the structure
        * (jobClass, List<condition(level), action>)
        */
        public static readonly Dictionary<uint, List<(Predicate<uint>, Action<Dictionary<uint, List<uint>>>)>> ManualCombo = new()
        {
            {
                19,
                new(){ //PLD
                (lvl=>lvl>=60,comboDict=> {comboDict[15].Remove(21);comboDict[15].Reverse();}), //Delete Rage of Halone after Royal Authority (also reverse to keep consistency with RoH position)
                }
            },
            {
                22,
                new(){ //DRG
                (lvl=>lvl>=56,comboDict=>comboDict[84]= new(){3554}),                //Add Fang and Claw
                (lvl=>lvl>=58,comboDict=>comboDict[3554]= new(){3556}),              //Add Wheeling Thrust
                }
            },
            {
                31, //MCH
                new()
                {
                ///Replace with heated
                (lvl => lvl >= 54, comboDict => {comboDict[7411]=comboDict[2866]; comboDict.Remove(2866); }),
                (lvl => lvl >= 60, comboDict => {comboDict[7412]=comboDict[2868]; comboDict.Remove(2868); comboDict[7411].Remove(2868); }),
                (lvl => lvl >= 64, comboDict => comboDict[7412].Remove(2873))
                }
            },
            {
                35, //RDM
                new()
                {
                //Remove enchanted
                (lvl => lvl >= 35, comboDict => comboDict[7504].Remove(7528)),
                (lvl => lvl >= 50, comboDict => comboDict[7512].Remove(7529)),
                }
            }
        };

        /*
         * Dict of cooldown groups considered weapon skills(which have a gcd) on each class other than 57
         */
        public static readonly Dictionary<uint, List<int>> WS_CooldownGroups = new()
        {
            //SAM
            {34, new() {10} },
            //NIN
            {30, new() {8} },
            //MCH
            {31, new() {6,7,8,11,12} },
            //DNC
            {38, new() {8, 19} },
            //RPR
            {39, new() {4} },
            //SGE
            {40, new() {18} },
        };
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly unsafe struct Action
    {
        [FieldOffset(0x0)] public readonly void* ActionManager;
        [FieldOffset(0x8)] public readonly float AnimationLock;
        [FieldOffset(0x28)] public readonly bool IsCast;
        [FieldOffset(0x30)] public readonly float CastTime;
        [FieldOffset(0x60)] public readonly float ComboTimer;
        [FieldOffset(0x64)] public readonly uint ComboID;
        [FieldOffset(0x68)] public readonly bool InQueue1;
        [FieldOffset(0x68)] public readonly bool InQueue2;
        [FieldOffset(0x70)] public readonly uint QueuedAction;
        [FieldOffset(0x78)] public readonly float dunno; //always 2.01 when queuing stuff
        [FieldOffset(0x618)] public readonly float ElapsedGCD;
        [FieldOffset(0x61C)] public readonly float TotalGCD;
        [FieldOffset(0x810)] public readonly float AnimationTimer;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x8)]
    public readonly struct Combo
    {
        [FieldOffset(0x00)] public readonly float Timer;
        [FieldOffset(0x04)] public readonly uint Action;
    }
}
