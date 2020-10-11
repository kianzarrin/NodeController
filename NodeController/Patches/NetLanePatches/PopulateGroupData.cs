namespace NodeController.Patches.NetLanePatches {
    using HarmonyLib;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    [HarmonyPatch]
    public static class PopulateGroupData {
        //public void PopulateGroupData(ushort segmentID, uint laneID, NetInfo.Lane laneInfo, bool destroyed, NetNode.Flags startFlags, NetNode.Flags endFlags, float startAngle, float
        static MethodInfo Target = typeof(NetLane).GetMethod(nameof(NetLane.PopulateGroupData), BindingFlags.Public | BindingFlags.Instance);
        static MethodBase TargetMethod() => Target;

        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator il, IEnumerable<CodeInstruction> instructions)
            => PropDisplacementCommons.Patch(instructions, Target);
    } // end class
} // end name space