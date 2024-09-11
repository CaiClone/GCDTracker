using Dalamud.Plugin.Services;
using GCDTracker.Data;
using ImGuiNET;

namespace GCDTracker.UI {
    public class GCDDisplay {
        protected readonly Configuration conf;
        protected readonly GCDHelper helper;

        private readonly GCDBar gcdBar;
        private readonly GCDWheel gcdWheel;
        private readonly ComboTracker comboTracker;
        private readonly FloatingAlerts floatingAlerts;

        public GCDDisplay(Configuration conf, GCDHelper helper, AbilityManager abilityManager) {
            this.conf = conf;
            this.helper = helper;

            gcdBar = new GCDBar(conf, helper, abilityManager);
            gcdWheel = new GCDWheel(conf, helper, abilityManager);
            comboTracker = new ComboTracker();
            floatingAlerts = new FloatingAlerts(conf);
        }

        public void Draw(PluginUI ui) {
            bool inCombat = DataStore.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat];
            bool noUI = DataStore.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedInQuestEvent]
                        || DataStore.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas]
                        || DataStore.ClientState.IsPvP;

            conf.EnabledGBJobs.TryGetValue(DataStore.ClientState.LocalPlayer.ClassJob.Id, out var enabledJobGB);
            conf.EnabledGWJobs.TryGetValue(DataStore.ClientState.LocalPlayer.ClassJob.Id, out var enabledJobGW);
            

            bool shouldShowGCDWheel = conf.WheelEnabled && !noUI;
            bool isGCDWheelMoveable = conf.WindowMoveableGW;
            bool showGCDWheelInCombat = enabledJobGW && 
                                        (conf.ShowOutOfCombat || inCombat);
            bool showGCDWheelWhenGCDNotRunning = !conf.ShowOnlyGCDRunning || 
                                                (helper.idleTimerAccum < helper.GCDTimeoutBuffer && 
                                                !helper.lastActionTP);

            if (shouldShowGCDWheel && 
                (isGCDWheelMoveable || 
                (showGCDWheelInCombat && 
                showGCDWheelWhenGCDNotRunning))) 
            {
                ui.SetupWindow("GCDTracker_GCDWheel", conf.WindowMoveableGW);
                gcdWheel.Draw(ui);
                ImGui.End();
            }

            bool shouldShowBar = conf.BarEnabled && 
                                !noUI;
            bool isBarMoveable = conf.BarWindowMoveable;
            bool showBarInCombat = enabledJobGB && 
                                (conf.ShowOutOfCombat || inCombat);
            bool showBarWhenGCDNotRunning = !conf.ShowOnlyGCDRunning || 
                                            (helper.idleTimerAccum < 
                                            helper.GCDTimeoutBuffer);
            bool showCastBarOrNoLastActionTP = conf.CastBarEnabled || 
                                            !helper.lastActionTP;

            if (shouldShowBar && 
                (isBarMoveable || 
                (showBarInCombat && 
                showBarWhenGCDNotRunning && 
                showCastBarOrNoLastActionTP))) 
            {
                ui.SetupWindow("GCDTracker_Bar", conf.BarWindowMoveable);
                // Hide the GCDBar if the castbar is active.
                // This seems to work fine, but if it ever becomes a problem,
                // might try using string.IsNullOrEmpty(GetCastbarContents())
                // instead of GameState.IsCasting() since that comes
                // directly from the game's castbar.
                gcdBar.Draw(ui);
                ImGui.End();
            }

            bool shouldShowTrianges = conf.FloatingTrianglesEnable && 
                                !noUI;
            bool isTriMoveable = conf.WindowMoveableSQI;
            bool showTriInCombat = conf.ShowOutOfCombat || inCombat;
            bool showTriWhenGCDNotRunning = !conf.ShowOnlyGCDRunning || 
                                            (helper.idleTimerAccum < 
                                            helper.GCDTimeoutBuffer);

            if (shouldShowTrianges && 
                (isTriMoveable || 
                (showTriInCombat && 
                showTriWhenGCDNotRunning))) 
            {
                ui.SetupWindow("GCDTracker_SlideQueueIndicators", conf.WindowMoveableSQI);
                floatingAlerts.Draw(ui);
                ImGui.End();
            }
        }
    }
}