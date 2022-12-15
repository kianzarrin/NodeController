namespace NodeController.Patches.Corner {
    using HarmonyLib;
    using JetBrains.Annotations;
    using KianCommons;
    using NodeController;
    using NodeController.Patches;
    using NodeController.Util;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using static KianCommons.Patches.TranspilerUtils;

    [UsedImplicitly]
    [HarmonyPatch]
    static class CalculateCorner_MinCornerOffsetPatch {
        /// <param name="leftSide">left side going away from the junction</param>
        static float FixMinCornerOffset(float cornerOffset0, ushort nodeID, ushort segmentID, bool leftSide) {
            var segmentData = SegmentEndManager.Instance.
                GetAt(segmentID: segmentID, nodeID: nodeID);
            if (segmentData == null)
                return cornerOffset0;
            if (segmentData.IsNodeless)
                return 0;
            return segmentData.Corner(leftSide).Offset;
        }

        [UsedImplicitly]
        static MethodBase TargetMethod() {
            return typeof(NetSegment).GetMethod(
                    nameof(NetSegment.CalculateCorner),
                    BindingFlags.Public | BindingFlags.Static) ??
                    throw new Exception("CalculateCornerPatch Could not find target method.");
        }

        [HarmonyBefore(CSURUtil.HARMONY_ID)]
        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions, MethodBase original) {
            FieldInfo f_minCornerOffset =
                typeof(NetInfo).GetField(nameof(NetInfo.m_minCornerOffset)) ??
                throw new Exception("f_minCornerOffset is null");
            MethodInfo m_GetMinCornerOffset =
                typeof(NetAI).GetMethod(nameof(NetAI.GetMinCornerOffset), throwOnError: true);

            MethodInfo m_FixMinCornerOffset = ReflectionHelpers.GetMethod(
                typeof(CalculateCorner_MinCornerOffsetPatch), nameof(FixMinCornerOffset));

            // apply the flat junctions transpiler
            instructions = FlatJunctionsCommons.ModifyFlatJunctionsTranspiler(instructions, original);

            CodeInstruction ldarg_startNodeID = GetLDArg(original, "startNodeID"); // push startNodeID into stack,
            CodeInstruction ldarg_segmentID = GetLDArg(original, "ignoreSegmentID");
            CodeInstruction ldarg_leftSide = GetLDArg(original, "leftSide");
            CodeInstruction call_GetMinCornerOffset = new CodeInstruction(OpCodes.Call, m_FixMinCornerOffset);

            int n = 0;
            foreach (var instruction in instructions) {
                yield return instruction;
                bool is_ldfld_minCornerOffset = instruction.LoadsField(f_minCornerOffset);
                bool callsGetMinCornerOffset = instruction.Calls(m_GetMinCornerOffset);
                if (is_ldfld_minCornerOffset || callsGetMinCornerOffset) {
                    n++;
                    yield return ldarg_startNodeID;
                    yield return ldarg_segmentID;
                    yield return ldarg_leftSide;
                    yield return call_GetMinCornerOffset;
                }
            }

            Log.Debug($"TRANSPILER CalculateCornerPatch: Successfully patched NetSegment.CalculateCorner(). " +
                $"found {n} instances of Ldfld NetInfo.m_minCornerOffset or GetMinCornerOffset()");
            yield break;
        }
    }
}
