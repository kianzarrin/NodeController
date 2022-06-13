namespace NodeController.Patches.NetLanePatches {
    using HarmonyLib;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using KianCommons;
    using static KianCommons.ReflectionHelpers;
    using KianCommons.Patches;
    using NodeController.Manager;

    [HarmonyPatch]
    public static class RenderInstance {
        // public void NetLane.RenderInstance(RenderManager.CameraInfo cameraInfo, ushort segmentID, uint laneID, NetInfo.Lane laneInfo, NetNode.Flags startFlags, NetNode.Flags endFlags, Color         static MethodInfo Target => typeof(global::NetLane).GetMethod("RenderInstance", BindingFlags.NonPublic | BindingFlags.Instance);
        static MethodInfo Target = typeof(NetLane).GetMethod(nameof(NetLane.RenderInstance), BindingFlags.Public | BindingFlags.Instance);
        static MethodBase TargetMethod() => Target;

        static FieldInfo f_flags = GetField<NetLane>(nameof(NetLane.m_flags));
        static MethodInfo mProcessFlags = GetMethod(typeof(RenderInstance), nameof(ProcessFlags));

        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator il, IEnumerable<CodeInstruction> instructions, MethodBase original) {
            instructions = PropDisplacementCommons.Patch(instructions, Target);
            foreach (var code in instructions) {
                yield return code;
                if (code.LoadsField(f_flags)) {
                    yield return TranspilerUtils.GetLDArg(original, "laneID");
                    yield return new CodeInstruction(OpCodes.Call, mProcessFlags);
                }
            }
        }

        public static NetLane.Flags ProcessFlags(NetLane.Flags flags, uint laneID) {
            if (LaneCache.Instance.ShouldHideArrows(laneID)) {
                return flags & ~(NetLane.Flags.LeftForwardRight);
            } else {
                return flags;
            }
        }
    } // end class
} // end name space