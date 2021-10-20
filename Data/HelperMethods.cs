using Dalamud.Game;
using System;
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

        public delegate byte UseActionDelegate(IntPtr actionManager, uint actionType, uint actionID, long targetedActorID, uint param, uint useType, int pvp);

        public static void Init(SigScanner scanner)
        {

            var GetRecastGroupPtr = scanner.ScanText("E8 ?? ?? ?? ?? 8B D0 48 8B CD 8B F0");
            var GetAdjustedActionIdPtr = scanner.ScanText("E8 ?? ?? ?? ?? 8B F8 3B DF");

            getRecastGroup = Marshal.GetDelegateForFunctionPointer<GetRecastGroupDelegate>(GetRecastGroupPtr);
            getAdjustedActionId = Marshal.GetDelegateForFunctionPointer<GetAdjustedActionIdDelegate>(GetAdjustedActionIdPtr);
        }

        public static bool IsWeaponSkill(uint actionType, uint actionID)
        {
            return GetRecastGroup(actionType, actionID) == 57;
        }
    }
}
 