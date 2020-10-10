namespace NodeController.Patches.NetLanePatches {
    using HarmonyLib;
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Collections.Generic;
    using KianCommons;
    using KianCommons.Patches;

   [HarmonyPatch]
    public static class RefreshInstance {
        // public void RefreshInstance(uint laneID, NetInfo.Lane laneInfo, float startAngle, float endAngle, bool invert, ref RenderManager.Instance data, ref int propIndex)
        static MethodInfo Target = typeof(NetLane).GetMethod(nameof(NetLane.RefreshInstance), BindingFlags.Public | BindingFlags.Instance);
        static MethodBase TargetMethod() => Target;
        
        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator il, IEnumerable<CodeInstruction> instructions) {
            try {
                var codes = instructions.ToCodeList();
                PropDisplacementCommons.Patch( codes, Target);
                Log.Debug("successfully patched NetLane.RefreshInstance");
                return codes;
            }
            catch (Exception e) {
                Log.Exception(e);
                throw e;
            }
        }
    } // end class
} // end name space