using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Common.Math;
using System.Globalization;
using System.Runtime.CompilerServices;
using static FFXIVClientStructs.FFXIV.Client.Game.Character.ActionEffectHandler;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

[assembly: InternalsVisibleTo("Tests")]
namespace GCDTracker.Data {
    public unsafe static class HelperMethods {
        public delegate byte UseActionDelegate(ActionManager* actionManager, ActionType actionType, uint actionID, ulong targetID, uint param, uint useType, int pvp, nint a7);
        public delegate void ReceiveActionEffectDetour(uint casterEntityId, Character* casterPtr, Vector3* targetPos, Header* header, TargetEffects* effects, GameObjectId* targetEntityIds);

        private static readonly TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
        public static bool IsWeaponSkill(ActionType actionType, uint actionID) {
            return _isWeaponSkill(
                DataStore.ActionManager->GetRecastGroup((int)actionType, actionID),
                DataStore.ActionManager->GetAdditionalRecastGroup(actionType, actionID));
        }

        internal static bool _isWeaponSkill(int recastGroup, int additionalRecast) => recastGroup == 57 || additionalRecast == 57;

        public static bool IsComboPreserving(uint actionID) => DataStore.ComboPreserving.ContainsKey((int)actionID);

        /// <summary>
        /// Describes if a skill is being added to the Queue, 0.5 and 0.6 are the default Animation locks with and without noclippy respectively
        /// </summary>
        public static bool IsAddingToQueue(bool isWeaponSkill, Action* act) =>
            _isAddingToQueue(
                isWeaponSkill,
                act->InQueue,
                act->ElapsedGCD,
                act->TotalGCD,
                act->AnimationLock,
                act->ElapsedCastTime,
                act->TotalCastTime);

        internal static bool _isAddingToQueue(bool isWeaponSkill, bool InQueue, float ElapsedGCD, float TotalGCD, float AnimationLock, float ElapsedCastTime, float TotalCastTime) {
            return InQueue && (
                    (isWeaponSkill && (
                        (ElapsedGCD < TotalGCD && ElapsedGCD > 0.001f) ||
                        (ElapsedCastTime < TotalCastTime && ElapsedCastTime > 0.001f) ||
                        (AnimationLock != 0f && AnimationLock != 0.5f && AnimationLock != 0.64000005f
                            && AnimationLock != 0.35f //Mudra
                            && AnimationLock != 0.74000007f //Gnashing Fang
                            && AnimationLock != 0.54f //Savage Claw
                            && AnimationLock != 0.81000006f // Wicked Talon
                            ))) || //Weaponskills
                    (!isWeaponSkill && AnimationLock != 0.5f && AnimationLock != 0.64000005f)); //OGCDS
        }

        public static uint? GetParentJob(uint jobId) => DataStore.ClassSheet.GetRow(jobId).ClassJobParent.ValueNullable?.RowId;

        public static string ReadStringFromPointer(byte** ptr) {
            if (ptr == null || *ptr == null) return "";
            return MemoryHelper.ReadSeStringNullTerminated(new nint(*ptr)).TextValue;
        }

        public static string GetAbilityName(uint actionID, ActionType actionType) {
            var lumina = DataStore.Lumina;
            var objectKind = DataStore.ClientState?.LocalPlayer?.TargetObject?.ObjectKind ?? ObjectKind.None;

            return objectKind switch {
                ObjectKind.Aetheryte => "Attuning...",
                ObjectKind.EventObj or ObjectKind.EventNpc => "Interacting...",
                _ when actionID == 1 && actionType != ActionType.Mount => "Interacting...",
                _ => actionType switch {
                    ActionType.Ability
                    or ActionType.Action
                    or ActionType.BgcArmyAction
                    or ActionType.CraftAction
                    or ActionType.PetAction
                    or ActionType.PvPAction =>
                        lumina?.GetExcelSheet<Lumina.Excel.Sheets.Action>()?.GetRow(actionID).Name.ExtractText() ?? "Unknown Ability",

                    ActionType.Companion =>
                        lumina?.GetExcelSheet<Lumina.Excel.Sheets.Companion>()?.GetRow(actionID) is var companion && companion != null
                        ? CapitalizeOutput(companion.Value.Singular.ExtractText())
                        : "Unknown Companion",

                    ActionType.Item
                    or ActionType.KeyItem =>
                        lumina?.GetExcelSheet<Lumina.Excel.Sheets.Item>()?.GetRow(actionID).Name.ExtractText() ?? "Unknown Item",

                    ActionType.Mount =>
                        lumina?.GetExcelSheet<Lumina.Excel.Sheets.Mount>()?.GetRow(actionID) is var mount && mount != null
                        ? CapitalizeOutput(mount.Value.Singular.ExtractText())
                        : "Unknown Mount",

                    _ => "Casting..."
                }
            };
        }

        private static string CapitalizeOutput(string input) {
            if (string.IsNullOrEmpty(input))
                return input;

            return textInfo.ToTitleCase(input.ToLower());
        }
    }
}