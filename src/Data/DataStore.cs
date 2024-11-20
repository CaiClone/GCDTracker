using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using System.Linq;
using Dalamud.Plugin.Services;

namespace GCDTracker.Data {
    static unsafe class DataStore {
        public static IDataManager Lumina;
        public static ComboDetail Combo;
        public static Action* Action;
        public static ActionManager* ActionManager;
        public static AtkStage* AtkStage;
        public static IClientState ClientState;
        public static ICondition Condition;
        public static ExcelSheet<Lumina.Excel.Sheets.Action> ActionSheet;
        public static ExcelSheet<Lumina.Excel.Sheets.ClassJob> ClassSheet;

        public static Dictionary<int, bool> ComboPreserving;

        public static void Init(IDataManager data, IClientState cs, ICondition cond) {
            Lumina = data;
            ActionSheet = data.Excel.GetSheet<Lumina.Excel.Sheets.Action>();
            ClassSheet = data.Excel.GetSheet<Lumina.Excel.Sheets.ClassJob>();

            ActionManager = FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
            AtkStage = FFXIVClientStructs.FFXIV.Component.GUI.AtkStage.Instance();
            ComboPreserving = ActionSheet.Where(row => row.PreservesCombo).ToDictionary(row => (int)row.RowId, _ => true);

            Combo = ActionManager->Combo;
            Action = (Action*)ActionManager;
            ClientState = cs;
            Condition = cond;
        }
        /*
        * Dict of manual changes to combo dict with the structure
        * (jobClass, List<condition(level), action>)
        */
        public static readonly Dictionary<uint, List<(Predicate<uint>, Action<Dictionary<uint, List<uint>>>)>> ManualCombo = new() {
            {
                19,
                new(){ //PLD
                (lvl=>lvl>=60,comboDict=> {comboDict[15].Remove(21);comboDict[15].Reverse();}), //Delete Rage of Halone after Royal Authority (also reverse to keep consistency with RoH position)
                }
            },
            {
                22,
                new(){ //DRG
                (lvl=>lvl>=56,comboDict=>comboDict[84] = [3554]),                //Add True Thrust to Fang and Claw
                (lvl=>lvl>=58,comboDict=>comboDict[88] = [3556+500]),            //Add Chaos Thrust to Wheeling Thrust
                (lvl=>lvl>=64,comboDict=>comboDict[3554] = [3556]),              //Add Wheeling Thrust
                (lvl=>lvl>=64,comboDict=>comboDict[3556+500] = [3554+500]),        //Add Fang and Claw
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
        public static readonly List<uint> TeleportIds = [5, 6];
    }

    [StructLayout(LayoutKind.Explicit)]
    public readonly unsafe struct Action {
        [FieldOffset(0x0)] public readonly void* ActionManager;
        [FieldOffset(0x8)] public readonly float AnimationLock;
        [FieldOffset(0x24)] public readonly uint CastId;
        [FieldOffset(0x28)] public readonly bool IsCast;
        [FieldOffset(0x30)] public readonly float ElapsedCastTime;
        [FieldOffset(0x34)] public readonly float TotalCastTime;
        [FieldOffset(0x60)] public readonly float ComboTimer;
        [FieldOffset(0x64)] public readonly uint ComboID;
        [FieldOffset(0x68)] public readonly bool InQueue;
        [FieldOffset(0x70)] public readonly uint QueuedAction;
        [FieldOffset(0x78)] public readonly float dunno; //always 2.01 when queuing stuff
        [FieldOffset(0x5F0)] public readonly float ElapsedGCD;
        [FieldOffset(0x5F4)] public readonly float TotalGCD;
        [FieldOffset(0x7E8)] public readonly float AnimationTimer;
    }
}
