using System;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using GCDTracker.Attributes;
using GCDTracker.Data;
using GCDTracker.UI;

namespace GCDTracker {
    public unsafe class GCDTracker : IDalamudPlugin {
        [PluginService]
        private IDalamudPluginInterface PluginInterface { get; init; }

        [PluginService]
        private ICommandManager Commands { get; init; }

        [PluginService]
        public static IFramework Framework { get; private set; }

        [PluginService]
        private IClientState ClientState { get; init; }

        [PluginService]
        private IDataManager Data { get; init; }

        [PluginService]
        private ICondition Condition { get; init; }

        [PluginService]
        private IGameInteropProvider GameInteropProvider { get; init; }

        [PluginService]
        private ISigScanner SigScanner { get; init; }

        [PluginService]
        public static IPluginLog Log { get; private set; }

        private readonly PluginCommandManager<GCDTracker> commandManager;
        private readonly Configuration config;
        private readonly PluginUI ui;

        public string Name => "GCDTracker";

        private readonly Hook<HelperMethods.UseActionDelegate> UseActionHook;
        private readonly Hook<HelperMethods.ReceiveActionEffectDetour> ReceiveActionEffectHook;

        private  GCDHelper helper;
        private GCDEventHandler notify;
        private  GCDDisplay gcd;
        private readonly ComboTracker ct;

        public GCDTracker() {
            config = (Configuration)PluginInterface.GetPluginConfig() ?? new Configuration();
            config.Initialize(PluginInterface);
            config.Migrate();

            DataStore.Init(Data,ClientState,Condition);
            ComboStore.Init(config);

            ui = new PluginUI(config);
            notify = new GCDEventHandler(config);
            helper = new GCDHelper(config, notify);
            gcd = new GCDDisplay(config, Data, helper, notify);
    
            ct = new ComboTracker();

            ui.gcd = gcd;
            ui.helper = helper;
            ui.ct = ct;

            PluginInterface.UiBuilder.Draw += ui.Draw;
            PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
            PluginInterface.UiBuilder.OpenMainUi += OpenConfig;
            Framework.Update += ct.Update;
            Framework.Update += helper.Update;

            commandManager = new PluginCommandManager<GCDTracker>(this, Commands);

            UseActionHook = GameInteropProvider.HookFromAddress<HelperMethods.UseActionDelegate>((nint)ActionManager.MemberFunctionPointers.UseAction, UseActionDetour);
            ReceiveActionEffectHook = GameInteropProvider.HookFromAddress<HelperMethods.ReceiveActionEffectDetour>(SigScanner.ScanModule("40 55 56 57 41 54 41 55 41 56 48 8D AC 24"), ReceiveActionEffect);
            UseActionHook.Enable();
            ReceiveActionEffectHook.Enable();
        }

        private byte UseActionDetour(ActionManager* actionManager, ActionType actionType, uint actionID, ulong targetedActorID, uint param, uint useType, int pvp, nint a7) {
            var ret = UseActionHook.Original(actionManager, actionType, actionID, targetedActorID, param, useType, pvp, a7);
            helper.OnActionUse(ret, actionManager, actionType, actionID, targetedActorID, param, useType, pvp);
            ct.OnActionUse(ret,actionManager, actionType, actionID, targetedActorID, param, useType, pvp);
            return ret;
        }

        private void ReceiveActionEffect(int sourceActorID, IntPtr sourceActor, IntPtr vectorPosition, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail) {
            var oldLock = DataStore.Action->AnimationLock;
            ReceiveActionEffectHook.Original(sourceActorID, sourceActor, vectorPosition, effectHeader, effectArray, effectTrail);
            var newLock = DataStore.Action->AnimationLock;

            helper.UpdateAnlock(oldLock, newLock);
        }
        private void OpenConfig() { config.configEnabled = true; }

        [Command("/gcdtracker")]
        [HelpMessage("Open GCDTracker settings.")]
        public void GCDTrackerCommand(string _, string _2) => OpenConfig();

        #region IDisposable Support
        protected virtual void Dispose(bool disposing) {
            if (!disposing) return;

            UseActionHook?.Disable();
            UseActionHook?.Dispose();
            ReceiveActionEffectHook?.Disable();
            ReceiveActionEffectHook?.Dispose();

            commandManager.Dispose();

            config.Save();

            PluginInterface.UiBuilder.Draw -= ui.Draw;
            PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;
            Framework.Update -= ct.Update;
            Framework.Update -= helper.Update;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
