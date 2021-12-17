using Dalamud.Game;
using Dalamud.Logging;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace GCDTracker.Data
{
    static unsafe class HelperMethods
    {
        private delegate uint GetAdjustedActionIdDelegate(void* actionManager, uint actionID);
        private static GetAdjustedActionIdDelegate getAdjustedActionId;
        public static uint GetAdjustedActionId(uint actionID) { return getAdjustedActionId(DataStore.action->ActionManager, actionID); }

        private delegate ulong GetRecastGroupDelegate(void* actionManager, uint actionType, uint actionID);
        private static GetRecastGroupDelegate getRecastGroup;
        public static ulong GetRecastGroup(uint actionType, uint actionID) { return getRecastGroup(DataStore.action->ActionManager, actionType, actionID); }

        public delegate byte UseActionDelegate(IntPtr actionManager, uint actionType, uint actionID, long targetedActorID, uint param, uint useType, int pvp, IntPtr a7);
        public delegate void ReceiveActionEffectDetour(int sourceActorID, IntPtr sourceActor, IntPtr vectorPosition, IntPtr effectHeader, IntPtr effectArray, IntPtr effectTrail);

        public static void Init(SigScanner scanner)
        {

            var GetRecastGroupPtr = scanner.ScanText("E8 ?? ?? ?? ?? 8B D0 48 8B CD 8B F0");
            var GetAdjustedActionIdPtr = scanner.ScanText("E8 ?? ?? ?? ?? 8B F8 3B DF");

            getRecastGroup = Marshal.GetDelegateForFunctionPointer<GetRecastGroupDelegate>(GetRecastGroupPtr);
            getAdjustedActionId = Marshal.GetDelegateForFunctionPointer<GetAdjustedActionIdDelegate>(GetAdjustedActionIdPtr);
        }

        public static bool IsWeaponSkill(uint actionType, uint actionID)
        {
            return new ulong[] { 57, 9}.Contains(GetRecastGroup(actionType, actionID)); 
        }

        public static bool IsComboPreserving(uint actionID)
        {
            return ComboStore.ComboPreserving.ContainsKey((int)actionID);
        }

        /// <summary>
        /// Describes if a skill is being added to the Queue, 0.5 and 0.6 are the default Animation locks with and without noclippy respectively
        /// </summary>
        /// <returns></returns>
        public static bool IsAddingToQueue(uint actionType, uint actionID)
        {
            var weaponSkill = IsWeaponSkill(actionType, actionID);
            var act = DataStore.action;
            return act->InQueue1 && ((weaponSkill && (
                    (act->ElapsedGCD<act->TotalGCD && act->ElapsedGCD > 0) || (act->AnimationLock!=0f && act->AnimationLock!=0.5f && act->AnimationLock!=0.6f))) || //Weaponskills
                    (!weaponSkill && act->AnimationLock!= 0.5f && act->AnimationLock!= 0.6f)); //OGCDS
        }
    }
}
 