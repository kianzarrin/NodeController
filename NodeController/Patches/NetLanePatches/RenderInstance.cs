using HarmonyLib;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace NodeController.Patches.NetLanePatches {
    using KianCommons;
    using KianCommons.Patches;

   [HarmonyPatch]
    public static class RenderInstance {
        // public void NetLane.RenderInstance(RenderManager.CameraInfo cameraInfo, ushort segmentID, uint laneID, NetInfo.Lane laneInfo, NetNode.Flags startFlags, NetNode.Flags endFlags, Color         static MethodInfo Target => typeof(global::NetLane).GetMethod("RenderInstance", BindingFlags.NonPublic | BindingFlags.Instance);
        static MethodInfo Target = typeof(NetLane).GetMethod(nameof(NetLane.RenderInstance), BindingFlags.Public | BindingFlags.Instance);
        static MethodBase TargetMethod() => Target;
        
        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator il, IEnumerable<CodeInstruction> instructions) {
            try {
                var codes = instructions.ToCodeList();
                PropDisplacementCommons.Patch( codes, Target);
                Log.Debug("successfully patched NetLane.RenderInstance");
                return codes;
            }
            catch (Exception e) {
                Log.Exception(e);
                throw e;
            }
        }
    } // end class
} // end name space