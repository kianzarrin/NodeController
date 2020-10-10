namespace NodeController.Patches.NetLanePatches {
    using HarmonyLib;
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Collections.Generic;
    using KianCommons;
    using KianCommons.Patches;

   [HarmonyPatch]
    public static class RenderDestroyedInstance {
        // public void RenderDestroyedInstance(RenderManager.CameraInfo cameraInfo, ushort segmentID, uint laneID, NetInfo netInfo, NetInfo.Lane laneInfo, NetNode.Flags startFlags,
        static MethodInfo Target = typeof(NetLane).GetMethod(nameof(NetLane.RenderDestroyedInstance), BindingFlags.Public | BindingFlags.Instance);
        static MethodBase TargetMethod() => Target;
        
        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator il, IEnumerable<CodeInstruction> instructions) {
            try {
                var codes = instructions.ToCodeList();
                PropDisplacementCommons.Patch( codes, Target);
                Log.Debug("successfully patched NetLane.RenderDestroyedInstance");
                return codes;
            }
            catch (Exception e) {
                Log.Exception(e);
                throw e;
            }
        }
    } // end class
} // end name space