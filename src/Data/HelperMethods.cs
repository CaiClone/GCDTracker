using Dalamud.Logging;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Reflection;

namespace GCDTracker.Data
{
    static unsafe class HelperMethods
    {
        public delegate byte UseActionDelegate(IntPtr actionManager, ActionType actionType, uint actionID, long targetID, uint param, uint useType, int pvp, IntPtr a7);
        public delegate void ReceiveActionEffectDetour(int sourceActorID, IntPtr sourceActor, IntPtr vectorPosition, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail);

        public static bool IsWeaponSkill(ActionType actionType, uint actionID)
        {
            var recast_group = DataStore.actionManager->GetRecastGroup((int)actionType, actionID);
            if (recast_group == 57) return true;
            if (DataStore.WS_CooldownGroups.TryGetValue(DataStore.clientState.LocalPlayer.ClassJob.Id, out var ws_groups))
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
        public static bool IsAddingToQueue(ActionType actionType, uint actionID)
        {
            var weaponSkill = IsWeaponSkill(actionType, actionID);
            var act = DataStore.action;
            return act->InQueue && ((weaponSkill && (
                    (act->ElapsedGCD<act->TotalGCD && act->ElapsedGCD > 0) || (act->AnimationLock!=0f && act->AnimationLock!=0.5f && act->AnimationLock!=0.6f && act->AnimationLock != 0.35f))) || //Weaponskills
                    (!weaponSkill && act->AnimationLock!= 0.5f && act->AnimationLock!= 0.6f)); //OGCDS
        }

        public static uint? GetParentJob(uint jobId)
        {
            return DataStore.ClassSheet.GetRow(jobId).ClassJobParent.Value?.RowId;
        }
    }
}
 