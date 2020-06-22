namespace NodeController.Patches {
    using NodeController.Util;
    using HarmonyLib;
    using JetBrains.Annotations;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using static TranspilerUtils;
    using static Util.HelpersExtensions;

    [UsedImplicitly]
    [HarmonyPatch]
    static class CalculateCornerPatch {
        static float GetMinCornerOffset(float cornerOffset0, ushort nodeID) {
            var data = NodeManager.Instance.buffer[nodeID];
            return data?.CornerOffset ?? cornerOffset0;
        }

        [UsedImplicitly]
        static MethodBase TargetMethod() {
            return typeof(NetSegment).GetMethod(
                    nameof(NetSegment.CalculateCorner),
                    BindingFlags.Public | BindingFlags.Static) ??
                    throw new System.Exception("CalculateCornerPatch Could not find target method.");
        }

        static FieldInfo f_minCornerOffset =
            typeof(NetInfo).GetField(nameof(NetInfo.m_minCornerOffset)) ??
            throw new Exception("f_minCornerOffset is null");

        static MethodInfo mGetMinCornerOffset = AccessTools.DeclaredMethod(
            typeof(CalculateCornerPatch), nameof(GetMinCornerOffset)) ??
            throw new Exception("mGetMinCornerOffset is null");

        static MethodInfo targetMethod_ = TargetMethod() as MethodInfo;

        [HarmonyBefore(CSURUtil.HARMONY_ID)]
        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator il, IEnumerable<CodeInstruction> instructions) {
            // apply the flat junctions traspiler
            // instructions = FlatJunctionsCommons.ModifyFlatJunctionsTranspiler(instructions, targetMethod_);

            CodeInstruction ldarg_startNodeID = GetLDArg(targetMethod_, "startNodeID"); // push startNodeID into stack,
            CodeInstruction call_GetMinCornerOffset = new CodeInstruction(OpCodes.Call, mGetMinCornerOffset);

            int n = 0;
            foreach (var instruction in instructions) {
                yield return instruction;
                bool is_ldfld_minCornerOffset =
                    instruction.opcode == OpCodes.Ldfld && instruction.operand == f_minCornerOffset;
                if (is_ldfld_minCornerOffset) {
                    n++;
                    yield return ldarg_startNodeID;
                    yield return call_GetMinCornerOffset;
                }
            }

            Log.Debug($"TRANSPILER CalculateCornerPatch: Successfully patched NetSegment.CalculateCorner(). " +
                $"found {n} instances of Ldfld NetInfo.m_minCornerOffset");
            yield break;
        }
    }
}
