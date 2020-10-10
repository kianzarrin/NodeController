namespace NodeController.Patches.NetLanePatches {
    using HarmonyLib;
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Collections.Generic;
    using KianCommons;
    using KianCommons.Patches;

   [HarmonyPatch]
    public static class PopulateGroupData {
        //public void PopulateGroupData(ushort segmentID, uint laneID, NetInfo.Lane laneInfo, bool destroyed, NetNode.Flags startFlags, NetNode.Flags endFlags, float startAngle, float
        static MethodInfo Target = typeof(NetLane).GetMethod(nameof(NetLane.PopulateGroupData), BindingFlags.Public | BindingFlags.Instance);
        static MethodBase TargetMethod() => Target;
        
        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator il, IEnumerable<CodeInstruction> instructions) {
            try {
                var codes = instructions.ToCodeList();
                PropDisplacementCommons.Patch( codes, Target);
                Log.Debug("successfully patched NetLane.PopulateGroupData");
                return codes;
            }
            catch (Exception e) {
                Log.Exception(e);
                throw e;
            }
        }
    } // end class
} // end name space