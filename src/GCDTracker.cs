using System;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Plugin;
using FFXIVClientStructs;
using FFXIVClientStructs.FFXIV.Client.Game;
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
        private CommandManager Commands { get; init; }

        [PluginService]
        [RequiredVersion("1.0")]
        public static Framework Framework { get; private set; }

        [PluginService]
        [RequiredVersion("1.0")]
        private ClientState ClientState { get; init; }

        [PluginService]
        [RequiredVersion("1.0")]
        private Dalamud.Game.SigScanner Scanner { get; init; }

        [PluginService]
        [RequiredVersion("1.0")]
        private DataManager Data { get; init; }

        [PluginService]
        [RequiredVersion("1.0")]
        private Condition Condition { get; init; }

        private readonly PluginCommandManager<GCDTracker> commandManager;
        private readonly Configuration config;
        private readonly PluginUI ui;

        public string Name => "GCDTracker";

        private readonly Hook<HelperMethods.UseActionDelegate> UseActionHook;
        private readonly Hook<HelperMethods.ReceiveActionEffectDetour> ReceiveActionEffectHook;

        private readonly GCDWheel gcd;
        private readonly ComboTracker ct;

        public GCDTracker()
        {
            Resolver.Initialize();
            this.config = (Configuration)PluginInterface.GetPluginConfig() ?? new Configuration();
            this.config.Initialize(PluginInterface);

            DataStore.Init(Data,Scanner,ClientState,Condition);
            ComboStore.Init(config);

            this.ui = new PluginUI(this.config);
            this.gcd = new GCDWheel();
            this.ct = new ComboTracker();

            ui.gcd = this.gcd;
            ui.ct = this.ct;

            PluginInterface.UiBuilder.Draw += this.ui.Draw;
            PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
            Framework.Update += this.ct.Update;
            Framework.Update += this.gcd.Update;

            this.commandManager = new PluginCommandManager<GCDTracker>(this, Commands);

            UseActionHook = new ((IntPtr)ActionManager.fpUseAction, UseActionDetour);
            ReceiveActionEffectHook = new (Scanner.ScanText("E8 ?? ?? ?? ?? 48 8B 8D F0 03 00 00"), ReceiveActionEffect);
            UseActionHook.Enable();
            ReceiveActionEffectHook.Enable();
        }
        private byte UseActionDetour(IntPtr actionManager, ActionType actionType, uint actionID, long targetedActorID, uint param, uint useType, int pvp, IntPtr a7)
        {
            var ret = UseActionHook.Original(actionManager, actionType, actionID, targetedActorID, param, useType, pvp, a7);
            gcd.OnActionUse(ret, actionManager, actionType, actionID, targetedActorID, param, useType, pvp);
            ct.OnActionUse(ret,actionManager, actionType, actionID, targetedActorID, param, useType, pvp);
            return ret;
        }
        private void ReceiveActionEffect(int sourceActorID, IntPtr sourceActor, IntPtr vectorPosition, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail)
        {
            var oldLock = DataStore.Action->AnimationLock;
            ReceiveActionEffectHook.Original(sourceActorID, sourceActor, vectorPosition, effectHeader, effectArray, effectTrail);
            var newLock = DataStore.Action->AnimationLock;

            this.gcd.UpdateAnlock(oldLock, newLock);
        }
        private void OpenConfig() { this.config.configEnabled = true; }

        [Command("/gcdtracker")]
        [HelpMessage("Open GCDTracker settings.")]
        public void GCDTrackerCommand(string command, string args)
        {
            this.OpenConfig();
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            UseActionHook?.Disable();
            UseActionHook?.Dispose();
            ReceiveActionEffectHook?.Disable();
            ReceiveActionEffectHook?.Dispose();

            this.commandManager.Dispose();

            PluginInterface.SavePluginConfig(this.config);

            PluginInterface.UiBuilder.Draw -= this.ui.Draw;
            PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;
            Framework.Update -= this.ct.Update;
            Framework.Update -= this.gcd.Update;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
