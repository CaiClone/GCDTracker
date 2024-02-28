using Dalamud.Logging;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.Interop.Attributes;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tests")]
namespace GCDTracker.Data
{
    public unsafe static class HelperMethods
    {
        public delegate byte UseActionDelegate(ActionManager* actionManager, ActionType actionType, uint actionID, ulong targetID, uint param, uint useType, int pvp, nint a7);
        public delegate void ReceiveActionEffectDetour(int sourceActorID, IntPtr sourceActor, IntPtr vectorPosition, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail);

        public static bool IsWeaponSkill(ActionType actionType, uint actionID) {
            return _isWeaponSkill(
                DataStore.ActionManager->GetRecastGroup((int)actionType, actionID),
                DataStore.ClientState.LocalPlayer.ClassJob.Id);
        }

        internal static bool _isWeaponSkill(int recastGroup, uint job) {
            if (recastGroup == 57) return true;
            if (DataStore.WS_CooldownGroups.TryGetValue(job, out var ws_groups))
                return ws_groups.Contains(recastGroup);
            return false;
        }

        public static bool IsComboPreserving(uint actionID) => DataStore.ComboPreserving.ContainsKey((int)actionID);

        public static bool IsCasting() => DataStore.ClientState.LocalPlayer.CurrentCastTime > 0;

        public static string GetSignature<T>(string methodName) {
            MethodBase method = typeof(T).GetMethod(methodName);
            MemberFunctionAttribute attribute = (MemberFunctionAttribute)method.GetCustomAttributes(typeof(MemberFunctionAttribute), true)[0];
            return attribute.Signature;
        }

        /// <summary>
        /// Describes if a skill is being added to the Queue, 0.5 and 0.6 are the default Animation locks with and without noclippy respectively
        /// </summary>
        public static bool IsAddingToQueue(bool isWeaponSkill, Action* act) =>
            _isAddingToQueue(
                isWeaponSkill,
                act->InQueue,
                act->ElapsedGCD,
                act->TotalGCD,
                act->AnimationLock);

        internal static bool _isAddingToQueue(bool isWeaponSkill, bool InQueue, float ElapsedGCD, float TotalGCD, float AnimationLock) {
            return InQueue && (
                    (isWeaponSkill && (
                        (ElapsedGCD < TotalGCD && ElapsedGCD > 0.001f) ||
                        (AnimationLock != 0f && AnimationLock != 0.5f && AnimationLock != 0.64000005f
                            && AnimationLock != 0.35f //Mudra
                            && AnimationLock != 0.74000007f //Gnashing Fang
                            && AnimationLock != 0.54f //Savage Claw
                            && AnimationLock != 0.81000006f // Wicked Talon
                            ))) || //Weaponskills
                    (!isWeaponSkill && AnimationLock != 0.5f && AnimationLock != 0.64000005f)); //OGCDS
        }

        public static uint? GetParentJob(uint jobId) => DataStore.ClassSheet.GetRow(jobId).ClassJobParent.Value?.RowId;
        internal static bool IsTeleport(uint castId) => DataStore.TeleportIds.Contains(castId);
    }
}