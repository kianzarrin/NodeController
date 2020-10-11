namespace NodeController.Patches.NetLanePatches {
    using HarmonyLib;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    [HarmonyPatch]
    public static class RefreshInstance {
        // public void RefreshInstance(uint laneID, NetInfo.Lane laneInfo, float startAngle, float endAngle, bool invert, ref RenderManager.Instance data, ref int propIndex)
        static MethodInfo Target = typeof(NetLane).GetMethod(nameof(NetLane.RefreshInstance), BindingFlags.Public | BindingFlags.Instance);
        static MethodBase TargetMethod() => Target;

        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator il, IEnumerable<CodeInstruction> instructions)
            => PropDisplacementCommons.Patch(instructions, Target);
    } // end class
} // end name space