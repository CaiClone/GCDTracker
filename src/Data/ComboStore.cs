
using Dalamud.Data;
using Dalamud.Logging;
using Lumina.Excel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GCDTracker.Data
{
    public static class ComboStore
    {
        private static Dictionary<(uint, uint, bool, Dictionary<uint, bool>), Dictionary<uint, List<uint>>> comboCache;
        private static Configuration conf;
        public static void Init(Configuration config)
        {
            conf = config;
            comboCache = new Dictionary<(uint, uint, bool, Dictionary<uint, bool>), Dictionary<uint, List<uint>>>();
        }

        private static Dictionary<uint,List<uint>> getCombos(uint jobclass, uint level, bool isPvp)
        {
            PluginLog.Verbose($"Get combos for class: {jobclass} at level {level}");
            return DataStore.ActionSheet
                .Where(row => row.ActionCombo.Value.RowId != 0
                              && (row.ClassJobCategory.Value?.Name.RawString.Contains(DataStore.ClassSheet.GetRow(jobclass).Abbreviation) ?? false)
                              && row.ClassJobLevel <= level
                              && row.Name.RawString.Length > 0
                              && row.IsPvP == isPvp)
                .GroupBy(row => row.ActionCombo.Value.RowId)
                .ToDictionary(row => row.Key, row => row.Select(act => act.RowId).ToList());
        }

        public static Dictionary<uint, List<uint>> GetCombos()
        {
            var par = (DataStore.ClientState.LocalPlayer.ClassJob.Id, DataStore.ClientState.LocalPlayer.Level, false,conf.EnabledCTJobs);

            par.EnabledCTJobs.TryGetValue(par.Id, out bool enabled);
            if (!enabled) return new Dictionary<uint, List<uint>>();
            if(comboCache.TryGetValue(par, out var comboDict))
                return comboDict;
            comboDict = getCombos(par.Id, par.Level, par.Item3);
            applyManual(ref comboDict, par.Id, par.Level);
            comboCache.Add(par, comboDict);
            return comboDict;
        }

        private static void applyManual(ref Dictionary<uint, List<uint>> comboDict, uint jobclass, uint level)
        {
            if (DataStore.ManualCombo.TryGetValue(jobclass,out var modifications))
            {
                foreach (var (condition, effect) in modifications) {
                    try {
                        if (condition(level)) effect(comboDict);
                    }
                    catch(Exception e) {
                        PluginLog.Error("Couldn't apply modification: " + e);
                    }
                }
            }
        }
    }
}
