
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

        public static void Init(DataManager data)
        {
            ActionSheet = data.Excel.GetSheet<Lumina.Excel.GeneratedSheets.Action>();
            ClassSheet = data.Excel.GetSheet<Lumina.Excel.GeneratedSheets.ClassJob>();
        }

        public static Dictionary<uint, uint[][]> GetCombos(uint jobclass, uint level, bool isPvp=false)
        {
            PluginLog.Log($"{ActionSheet.GetRow(3538).Name.RawString}");
            PluginLog.Log($"{HelperMethods.GetAdjustedActionId(21)}");
            PluginLog.Log($"Get combos for class: {jobclass} at level {level}");

            var comboActions = ActionSheet
                .Where(row => row.ActionCombo.Value.RowId != 0
                              && (row.ClassJobCategory.Value?.Name.RawString.Contains(ClassSheet.GetRow(jobclass).Abbreviation) ?? false)
                              && row.ClassJobLevel <= level
                              && row.Name.RawString.Length>0
                              && row.IsPvP==isPvp)
                .GroupBy(row => row.ActionCombo.Value.RowId);
                
            foreach (var kvp in comboActions)
            {
                PluginLog.Log($"{kvp.Key},{kvp.ToList().Select(act=>act.RowId).Aggregate("",(a,n) => a+=" "+n)}");
            }
            return null;
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
