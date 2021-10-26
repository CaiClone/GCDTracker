
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

        private static Dictionary<(uint, uint, bool,Dictionary<uint,bool>), Dictionary<uint, List<uint>>> comboCache;
        private static Configuration conf;
        public static void Init(DataManager data,Configuration config)
        {
            ActionSheet = data.Excel.GetSheet<Lumina.Excel.GeneratedSheets.Action>();
            ClassSheet = data.Excel.GetSheet<Lumina.Excel.GeneratedSheets.ClassJob>();

            conf = config;

            comboCache = new Dictionary<(uint, uint, bool, Dictionary<uint, bool>), Dictionary<uint, List<uint>>>();
        }

        private static Dictionary<uint,List<uint>> getCombos(uint jobclass, uint level, bool isPvp)
        {
            PluginLog.Verbose($"Get combos for class: {jobclass} at level {level}");
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
            var par = (DataStore.clientState.LocalPlayer.ClassJob.Id, DataStore.clientState.LocalPlayer.Level, false,conf.EnabledCTJobs);

            par.EnabledCTJobs.TryGetValue(par.Id, out bool enabled);
            if (!enabled) return new Dictionary<uint, List<uint>>();
            if(comboCache.TryGetValue(par, out var comboDict))
                return comboDict;
            comboDict = getCombos(par.Id, par.Level, par.Item3);
            comboCache.Add(par, comboDict);
            return comboDict; 
        }

        public static uint? GetParentJob(uint jobId)
        {
            return ClassSheet.GetRow(jobId).ClassJobParent.Value?.RowId;
        }

    }
}
