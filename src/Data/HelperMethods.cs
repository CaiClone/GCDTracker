using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
                DataStore.ActionManager->GetAdditionalRecastGroup(actionType, actionID));
        }

        internal static bool _isWeaponSkill(int recastGroup, int additionalRecast) => recastGroup == 57 || additionalRecast == 57;

        public static bool IsComboPreserving(uint actionID) => DataStore.ComboPreserving.ContainsKey((int)actionID);

        public static bool IsCasting() => DataStore.ClientState.LocalPlayer.CurrentCastTime > 0;

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

        public static uint? GetParentJob(uint jobId) => DataStore.ClassSheet.GetRow(jobId).ClassJobParent.Value?.RowId;
        internal static bool IsTeleport(uint castId) => DataStore.TeleportIds.Contains(castId);
        
        public static string ReadStringFromPointer(byte** ptr) { 
            if (ptr == null || *ptr == null) return "";
            return Marshal.PtrToStringUTF8(new nint(*ptr));
        }
    }
}