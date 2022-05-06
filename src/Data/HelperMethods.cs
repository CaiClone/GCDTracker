using Dalamud.Logging;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Tests")]
namespace GCDTracker.Data
{
    public static unsafe class HelperMethods
    {
        public delegate byte UseActionDelegate(IntPtr actionManager, ActionType actionType, uint actionID, long targetID, uint param, uint useType, int pvp, IntPtr a7);
        public delegate void ReceiveActionEffectDetour(int sourceActorID, IntPtr sourceActor, IntPtr vectorPosition, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail);

        public static bool IsWeaponSkill(ActionType actionType, uint actionID)
        {
            return _isWeaponSkill(
                DataStore.actionManager->GetRecastGroup((int)actionType, actionID),
                DataStore.clientState.LocalPlayer.ClassJob.Id);
        }

        internal static bool _isWeaponSkill(int recast_group, uint job)
        {
            if (recast_group == 57) return true;
            if (DataStore.WS_CooldownGroups.TryGetValue(job, out var ws_groups))
                return ws_groups.Contains(recast_group);
            return false;
        }

        public static bool IsComboPreserving(uint actionID)
        {
            return DataStore.ComboPreserving.ContainsKey((int)actionID);
        }

        public static bool IsCasting()
        {
            return DataStore.clientState.LocalPlayer.CurrentCastTime > 0;
        }

        public static string GetSignature<T>(string methodName)
        {
            MethodBase method = typeof(T).GetMethod(methodName);
            MemberFunctionAttribute attribute = (MemberFunctionAttribute)method.GetCustomAttributes(typeof(MemberFunctionAttribute), true)[0];
            return attribute.Signature;
        }

        /// <summary>
        /// Describes if a skill is being added to the Queue, 0.5 and 0.6 are the default Animation locks with and without noclippy respectively
        /// </summary>
        /// <returns></returns>
        public static bool IsAddingToQueue(bool isWeaponSkill, Action* act)
        {
            return _isAddingToQueue(
                isWeaponSkill,
                act->InQueue,
                act->ElapsedGCD,
                act->TotalGCD,
                act->AnimationLock);
        }

        internal static bool _isAddingToQueue(bool isWeaponSkill, bool InQueue, float ElapsedGCD, float TotalGCD, float AnimationLock)
        {
            return InQueue && (
                    (isWeaponSkill && (
                        (ElapsedGCD < TotalGCD && ElapsedGCD > 0.001f) ||
                        (AnimationLock != 0f && AnimationLock != 0.5f && AnimationLock != 0.64f && AnimationLock != 0.35f))) || //Weaponskills
                    (!isWeaponSkill && AnimationLock != 0.5f && AnimationLock != 0.64f)); //OGCDS

        }

        public static uint? GetParentJob(uint jobId)
        {
            return DataStore.ClassSheet.GetRow(jobId).ClassJobParent.Value?.RowId;
        }
    }
}
 