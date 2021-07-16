namespace NodeController.Patches.Nodeless {
    using HarmonyLib;
    using JetBrains.Annotations;
    using KianCommons;
    using NodeController.Util;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using KianCommons.Patches;
    using static KianCommons.Patches.TranspilerUtils;

    [UsedImplicitly]
    [HarmonyPatch]
    static class ClipSegmentEndPatch {
        /// <param name="leftSide">left side going away from the junction</param>
        static bool GetClipSegmentEnd(bool clipSegmentEnd0, ushort nodeID, ushort segmentID) {
            var segmentData = SegmentEndManager.Instance.
                GetAt(segmentID: segmentID, nodeID: nodeID);
            if (segmentData == null)
                return clipSegmentEnd0;
            return !segmentData.Nodeless;
        }

        [UsedImplicitly]
        static IEnumerable<MethodBase> TargetMethods() {
            yield return typeof(NetSegment).GetMethod(
                    nameof(NetSegment.CalculateCorner),
                    BindingFlags.Public | BindingFlags.Static,
                    throwOnError: true);

            // yield return typeof(NetSegment).GetMethod(nameof(NetSegment.OverlapQuad), throwOnError: true);
        }

        static FieldInfo f_clipSegmentEnds =>
            ReflectionHelpers.GetField<NetInfo>(nameof(NetInfo.m_clipSegmentEnds));

        static MethodInfo mGetClipSegmentEnd = ReflectionHelpers.GetMethod(
            typeof(ClipSegmentEndPatch), nameof(GetClipSegmentEnd));

        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions, MethodBase original) {

            CodeInstruction ldarg_nodeID = GetLDArg(original, "startNodeID");
            CodeInstruction ldarg_segmentID = GetLDArg(original, "ignoreSegmentID");
            CodeInstruction call_GetMinCornerOffset = new CodeInstruction(OpCodes.Call, mGetClipSegmentEnd);

            int n = 0;
            foreach (var instruction in instructions) {
                yield return instruction;
                if (instruction.LoadsField(f_clipSegmentEnds)) {
                    n++;
                    yield return ldarg_nodeID;
                    yield return ldarg_segmentID;
                    yield return call_GetMinCornerOffset;
                }
            }

            Log.Succeeded($"f_clipSegmentEnds {n} instances of {f_clipSegmentEnds} in {original}");
            yield break;
        }
    }
}
