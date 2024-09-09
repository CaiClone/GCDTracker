using System;
using Dalamud.Plugin.Services;
using GCDTracker.Data;

namespace GCDTracker.UI {
    public unsafe class GCDWheel(Configuration conf, GCDHelper helper, AbilityManager abilityManager) {
        protected readonly Configuration conf = conf;
        protected readonly GCDHelper helper = helper;
        protected readonly AbilityManager abilityManager = abilityManager;

        public void Draw(PluginUI ui) {
            float gcdTotal = helper.TotalGCD;
            float gcdTime = helper.lastElapsedGCD;

            if (GameState.IsCasting() && DataStore.Action->ElapsedCastTime >= gcdTotal && !GameState.IsCastingTeleport())
                gcdTime = gcdTotal;
            if (gcdTotal < 0.1f) return;
            helper.MiscEventChecker();
            helper.WheelCheckQueueEvent(conf, gcdTime / gcdTotal);

            var notify = GCDEventHandler.Instance;
            notify.Update(null, conf, ui);

            // Background
            ui.DrawCircSegment(0f, 1f, 6f * notify.WheelScale, conf.backColBorder);
            ui.DrawCircSegment(0f, 1f, 3f * notify.WheelScale, helper.BackgroundColor());
            if (conf.QueueLockEnabled) {
                ui.DrawCircSegment(0.8f, 1, 9f * notify.WheelScale, conf.backColBorder);
                ui.DrawCircSegment(0.8f, 1, 6f * notify.WheelScale, helper.BackgroundColor());
            }
            ui.DrawCircSegment(0f, Math.Min(gcdTime / gcdTotal, 1f), 20f * notify.WheelScale, conf.frontCol);
            foreach (var (ogcd, (anlock, iscast)) in abilityManager.ogcds) {
                var isClipping = helper.CheckClip(iscast, ogcd, anlock, gcdTotal, gcdTime);
                ui.DrawCircSegment(ogcd / gcdTotal, (ogcd + anlock) / gcdTotal, 21f * notify.WheelScale, isClipping ? conf.clipCol : conf.anLockCol);
                if (!iscast) ui.DrawCircSegment(ogcd / gcdTotal, (ogcd + 0.04f) / gcdTotal, 23f * notify.WheelScale, conf.ogcdCol);
            }
        }
    }
}