namespace NodeController.Patches.Nodeless {
    using HarmonyLib;
    using KianCommons;
    using KianCommons.Patches;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using UnityEngine;
    using static KianCommons.Patches.TranspilerUtils;

    [HarmonyPatch]
    static class ClipSegmentEndPatch2 {
        static IEnumerable<MethodBase> TargetMethods() {
            yield return typeof(NetSegment).GetMethod(nameof(NetSegment.OverlapQuad), throwOnError: true);
            yield return typeof(ZoneBlock).GetMethod("CalculateImplementation1", throwOnError: true);
        }

        static bool GetClipSegmentStart(bool clipSegmentEnd0, ushort segmentID) =>
            ClipSegmentEndPatch.GetClipSegmentEnd(clipSegmentEnd0, segmentID.ToSegment().m_startNode, segmentID);

        static bool GetClipSegmentEnd(bool clipSegmentEnd0, ushort segmentID) =>
            ClipSegmentEndPatch.GetClipSegmentEnd(clipSegmentEnd0, segmentID.ToSegment().m_endNode, segmentID);

        static FieldInfo f_clipSegmentEnds =>
            ReflectionHelpers.GetField<NetInfo>(nameof(NetInfo.m_clipSegmentEnds));

        static MethodInfo mGetClipSegmentStart = ReflectionHelpers.GetMethod(
            typeof(ClipSegmentEndPatch2), nameof(GetClipSegmentStart));

        static MethodInfo mGetClipSegmentEnd = ReflectionHelpers.GetMethod(
            typeof(ClipSegmentEndPatch2), nameof(GetClipSegmentEnd));

        public static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions) {
            CodeInstruction ldSegmentID = GetLDArg(original, "segmentID");
            CodeInstruction callGetClipSegmentStart = new CodeInstruction(OpCodes.Call, mGetClipSegmentStart);
            CodeInstruction callGetClipSegmentEnd = new CodeInstruction(OpCodes.Call, mGetClipSegmentEnd);

            int n = 0;
            foreach (var instruction in instructions) {
                yield return instruction;
                if (instruction.LoadsField(f_clipSegmentEnds)) {
                    yield return ldSegmentID.Clone();

                    // first one is for start node and second one is for end node.
                    switch(n) {
                        case 0:
                            yield return callGetClipSegmentStart.Clone();
                            break;
                        case 1:
                            yield return callGetClipSegmentEnd.Clone();
                            break;
                        default:
                            new Exception("expected only 2 occurrences").Log();
                            break;
                    }

                    n++;
                }
            }

            Log.Succeeded($"patched {n} instances of {f_clipSegmentEnds} in {original}");
        }
    }
}
