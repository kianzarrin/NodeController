namespace NodeController.Patches.NetLanePatches {
    using HarmonyLib;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    [HarmonyPatch]
    public static class RenderInstance {
        // public void NetLane.RenderInstance(RenderManager.CameraInfo cameraInfo, ushort segmentID, uint laneID, NetInfo.Lane laneInfo, NetNode.Flags startFlags, NetNode.Flags endFlags, Color         static MethodInfo Target => typeof(global::NetLane).GetMethod("RenderInstance", BindingFlags.NonPublic | BindingFlags.Instance);
        static MethodInfo Target = typeof(NetLane).GetMethod(nameof(NetLane.RenderInstance), BindingFlags.Public | BindingFlags.Instance);
        static MethodBase TargetMethod() => Target;

        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator il, IEnumerable<CodeInstruction> instructions)
            => PropDisplacementCommons.Patch(instructions, Target);
    } // end class
} // end name space