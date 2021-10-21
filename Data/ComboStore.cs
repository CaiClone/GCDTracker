
using Dalamud.Data;
using Dalamud.Logging;
using Lumina.Excel;
using System.Collections.Generic;
using System.Linq;

namespace GCDTracker.Data
{
    public static class ComboStore
    {
        public static ExcelSheet<Lumina.Excel.GeneratedSheets.Action> ActionSheet;
        public static ExcelSheet<Lumina.Excel.GeneratedSheets.ClassJob> ClassSheet;

        private static Dictionary<(uint, uint, bool), Dictionary<uint, List<uint>>> comboCache;
        public static void Init(DataManager data)
        {
            ActionSheet = data.Excel.GetSheet<Lumina.Excel.GeneratedSheets.Action>();
            ClassSheet = data.Excel.GetSheet<Lumina.Excel.GeneratedSheets.ClassJob>();

            comboCache = new Dictionary<(uint, uint, bool), Dictionary<uint, List<uint>>>();
        }

        //TODO: Cache
        //TODO: Use Data and ignore parameters
        private static Dictionary<uint,List<uint>> getCombos(uint jobclass, uint level, bool isPvp)
        {
            //PluginLog.Log($"{ActionSheet.GetRow(3538).Name.RawString}");
            //PluginLog.Log($"{HelperMethods.GetAdjustedActionId(21)}");
            PluginLog.Log($"Get combos for class: {jobclass} at level {level}");

            return ActionSheet
                .Where(row => row.ActionCombo.Value.RowId != 0
                              && (row.ClassJobCategory.Value?.Name.RawString.Contains(ClassSheet.GetRow(jobclass).Abbreviation) ?? false)
                              && row.ClassJobLevel <= level
                              && row.Name.RawString.Length > 0
                              && row.IsPvP == isPvp)
                .GroupBy(row => row.ActionCombo.Value.RowId)
                .ToDictionary(row => row.Key, row => row.Select(act => act.RowId).ToList());
        }
        public static Dictionary<uint, List<uint>> GetCombos()
        {
            var par = (DataStore.clientState.LocalPlayer.ClassJob.Id, DataStore.clientState.LocalPlayer.Level, false);
            if(comboCache.TryGetValue(par, out var comboDict))
                return comboDict;
            comboDict = getCombos(par.Id, par.Level, par.Item3);
            comboCache.Add(par, comboDict);
            return comboDict;
        }

        public static Dictionary<uint, uint[][]> COMBOS = new Dictionary<uint, uint[][]>(){
            //Gladiator
            {1,new uint[][] {
                new uint[] {9,15,21},
                new uint[] {7383,16457}
            }},
            //Paladin
            {19,new uint[][] {
                new uint[] {9,15,3638},
                new uint[] {9,15,3538},
                new uint[] {7381,16457},
            }},     
            //Lancer
            {4,new uint[][] {
                new uint[] {75,78,84},
                new uint[] {75, 87, 88},
            }},
            //Dragoon
            {22,new uint[][] {
                new uint[] {75,78,84},
                new uint[] {75, 87, 88},
            }}
        };
    }
}
