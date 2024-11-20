using System;
using System.Collections.Generic;
using System.Linq;

namespace GCDTracker.Data {
    public static class ComboStore {
        private static Dictionary<(uint, uint, bool, Dictionary<uint, bool>), Dictionary<uint, List<uint>>> comboCache;
        private static Configuration conf;
        public static void Init(Configuration config) {
            conf = config;
            comboCache = [];
        }

        private static Dictionary<uint, List<uint>> GetCombos(uint jobclass, uint level, bool isPvp) {
            GCDTracker.Log.Verbose($"Get combos for class: {jobclass} at level {level}");
            return DataStore.ActionSheet
                .Where(row => row.ActionCombo.Value.RowId != 0
                              && row.ClassJobCategory.Value.Name.ExtractText().Contains(DataStore.ClassSheet.GetRow(jobclass).Abbreviation.ExtractText())
                              && row.ClassJobLevel <= level
                              && !row.Name.IsEmpty
                              && row.IsPvP == isPvp)
                .GroupBy(row => row.ActionCombo.Value.RowId)
                .ToDictionary(row => row.Key, row => row.Select(act => act.RowId).ToList());
        }

        public static Dictionary<uint, List<uint>> GetCombos() {
            var par = (DataStore.ClientState.LocalPlayer.ClassJob.RowId, DataStore.ClientState.LocalPlayer.Level, false, conf.EnabledCTJobs);
            par.EnabledCTJobs.TryGetValue(par.RowId, out bool enabled);
            if (!enabled) return [];
            if (comboCache.TryGetValue(par, out var comboDict))
                return comboDict;
            comboDict = GetCombos(par.RowId, par.Level, par.Item3);
            ApplyManual(ref comboDict, par.RowId, par.Level);
            comboCache.Add(par, comboDict);
            return comboDict;
        }

        private static void ApplyManual(ref Dictionary<uint, List<uint>> comboDict, uint jobclass, uint level) {
            if (DataStore.ManualCombo.TryGetValue(jobclass, out var modifications)) {
                foreach (var (condition, effect) in modifications) {
                    try {
                        if (condition(level)) effect(comboDict);
                    } catch (Exception e) {
                        GCDTracker.Log.Error("Couldn't apply modification: " + e);
                    }
                }
            }
        }
    }
}
