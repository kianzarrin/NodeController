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
    internal static class ClipSegmentEndPatch {
        internal static bool GetClipSegmentEnd(bool clipSegmentEnd0, ushort nodeID, ushort segmentID) {
            var segmentData = SegmentEndManager.Instance.GetAt(segmentID: segmentID, nodeID: nodeID);
            bool nodeless = segmentData?.IsNodeless ?? false;
            return clipSegmentEnd0 && !nodeless;
        }

        [UsedImplicitly]
        static MethodBase TargetMethod() {
            return typeof(NetSegment).GetMethod(
                    nameof(NetSegment.CalculateCorner),
                    BindingFlags.Public | BindingFlags.Static,
                    throwOnError: true);
        }

        static FieldInfo f_clipSegmentEnds =>
            ReflectionHelpers.GetField<NetInfo>(nameof(NetInfo.m_clipSegmentEnds));

        static MethodInfo mGetClipSegmentEnd = ReflectionHelpers.GetMethod(
            typeof(ClipSegmentEndPatch), nameof(GetClipSegmentEnd));

        public static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions) {
            CodeInstruction ldNodeID = GetLDArg(original, "startNodeID");
            CodeInstruction ldSegmentID = GetLDArg(original, "ignoreSegmentID");
            CodeInstruction callGetClipSegmentEnd = new CodeInstruction(OpCodes.Call, mGetClipSegmentEnd);

            int n = 0;
            foreach (var instruction in instructions) {
                yield return instruction;
                if (instruction.LoadsField(f_clipSegmentEnds)) {
                    n++;
                    yield return ldNodeID.Clone();
                    yield return ldSegmentID.Clone();
                    yield return callGetClipSegmentEnd.Clone();
                }
            }

            Log.Succeeded($"patched {n} instances of {f_clipSegmentEnds} in {original}");
        }
    }
}
