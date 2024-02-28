using System;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.Interop;
using GCDTracker.Attributes;
using GCDTracker.Data;

namespace GCDTracker
{
    public unsafe class GCDTracker : IDalamudPlugin
    {
        [PluginService]
        [RequiredVersion("1.0")]
        private DalamudPluginInterface PluginInterface { get; init; }

        [PluginService]
        [RequiredVersion("1.0")]
        private ICommandManager Commands { get; init; }

        [PluginService]
        [RequiredVersion("1.0")]
        public static IFramework Framework { get; private set; }

        [PluginService]
        [RequiredVersion("1.0")]
        private IClientState ClientState { get; init; }

        [PluginService]
        [RequiredVersion("1.0")]
        private IDataManager Data { get; init; }

        [PluginService]
        [RequiredVersion("1.0")]
        private ICondition Condition { get; init; }

        [PluginService]
        [RequiredVersion("1.0")]
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

        private readonly GCDWheel gcd;
        private readonly ComboTracker ct;

        public GCDTracker() {
            Resolver.GetInstance.SetupSearchSpace();
            Resolver.GetInstance.Resolve();
            config = (Configuration)PluginInterface.GetPluginConfig() ?? new Configuration();
            config.Initialize(PluginInterface);

            DataStore.Init(Data,ClientState,Condition);
            ComboStore.Init(config);

            ui = new PluginUI(config);
            gcd = new GCDWheel();
            ct = new ComboTracker();

            ui.gcd = gcd;
            ui.ct = ct;

            PluginInterface.UiBuilder.Draw += ui.Draw;
            PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
            Framework.Update += ct.Update;
            Framework.Update += gcd.Update;

            commandManager = new PluginCommandManager<GCDTracker>(this, Commands);

            UseActionHook = GameInteropProvider.HookFromAddress<HelperMethods.UseActionDelegate>((nint)ActionManager.MemberFunctionPointers.UseAction, UseActionDetour);
            ReceiveActionEffectHook = GameInteropProvider.HookFromAddress<HelperMethods.ReceiveActionEffectDetour>(SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B 8D F0 03 00 00"), ReceiveActionEffect);
            UseActionHook.Enable();
            ReceiveActionEffectHook.Enable();
        }

        private byte UseActionDetour(ActionManager* actionManager, ActionType actionType, uint actionID, ulong targetedActorID, uint param, uint useType, int pvp, nint a7) {
            var ret = UseActionHook.Original(actionManager, actionType, actionID, targetedActorID, param, useType, pvp, a7);
            gcd.OnActionUse(ret, actionManager, actionType, actionID, targetedActorID, param, useType, pvp);
            ct.OnActionUse(ret,actionManager, actionType, actionID, targetedActorID, param, useType, pvp);
            return ret;
        }

        private void ReceiveActionEffect(int sourceActorID, IntPtr sourceActor, IntPtr vectorPosition, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail) {
            var oldLock = DataStore.Action->AnimationLock;
            ReceiveActionEffectHook.Original(sourceActorID, sourceActor, vectorPosition, effectHeader, effectArray, effectTrail);
            var newLock = DataStore.Action->AnimationLock;

            gcd.UpdateAnlock(oldLock, newLock);
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

            PluginInterface.SavePluginConfig(config);

            PluginInterface.UiBuilder.Draw -= ui.Draw;
            PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;
            Framework.Update -= ct.Update;
            Framework.Update -= gcd.Update;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
