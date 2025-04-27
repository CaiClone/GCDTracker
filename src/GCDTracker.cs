using System;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Common.Math;
using GCDTracker.Attributes;
using GCDTracker.Data;
using GCDTracker.UI;

#pragma warning disable CS8618,CS8600,CS8602,CS8604 // Properties with [PluginService] are initialized by Dalamud.
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

        private GCDHelper helper;
        private readonly ComboTracker ct;

        public GCDTracker() {
            config = (Configuration)PluginInterface.GetPluginConfig() ?? new Configuration();
            config.Initialize(PluginInterface);
            config.Migrate();

            DataStore.Init(Data, ClientState, Condition);
            ComboStore.Init(config);

            ui = new PluginUI(config);
            helper = new GCDHelper(config);
            bool inCombat = DataStore.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat];
            bool noUI = DataStore.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.OccupiedInQuestEvent]
                        || DataStore.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas]
                        || DataStore.ClientState.IsPvP;
            ct = new ComboTracker(config, helper);
            var abilityManager = AbilityManager.Instance;
            var gcdBar = new GCDBar(config, helper, abilityManager);

            ui.bar = gcdBar;
            ui.Windows = new() {
                gcdBar,
                new GCDWheel(config, helper, abilityManager),
                new FloatingAlerts(config, helper),
                ct,
            };

            PluginInterface.UiBuilder.Draw += ui.Draw;
            PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
            PluginInterface.UiBuilder.OpenMainUi += OpenConfig;
            Framework.Update += ct.Update;
            Framework.Update += helper.Update;
            Framework.Update += gcdBar.Update;

            commandManager = new PluginCommandManager<GCDTracker>(this, Commands);

            UseActionHook = GameInteropProvider.HookFromAddress<HelperMethods.UseActionDelegate>((nint)ActionManager.MemberFunctionPointers.UseAction, UseActionDetour);
            ReceiveActionEffectHook = GameInteropProvider.HookFromAddress<HelperMethods.ReceiveActionEffectDetour>((nint)ActionEffectHandler.MemberFunctionPointers.Receive, ReceiveActionEffect);
            UseActionHook.Enable();
            ReceiveActionEffectHook.Enable();
        }

        private byte UseActionDetour(ActionManager* actionManager, ActionType actionType, uint actionID, ulong targetedActorID, uint param, uint useType, int pvp, nint a7) {
            var ret = UseActionHook.Original(actionManager, actionType, actionID, targetedActorID, param, useType, pvp, a7);
            helper.OnActionUse(ret, actionManager, actionType, actionID, targetedActorID, param, useType, pvp);
            ct.OnActionUse(ret, actionManager, actionType, actionID, targetedActorID, param, useType, pvp);
            return ret;
        }

        private void ReceiveActionEffect(uint casterEntityId, Character* casterPtr, Vector3* targetPos, ActionEffectHandler.Header* header, ActionEffectHandler.TargetEffects* effects, GameObjectId* targetEntityIds) {
            var oldLock = DataStore.Action->AnimationLock;
            ReceiveActionEffectHook.Original(casterEntityId, casterPtr, targetPos, header, effects, targetEntityIds);
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
#pragma warning restore CS8618,CS8600,CS8602,CS8604 
