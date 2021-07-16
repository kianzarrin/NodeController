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
            try {
                var codes = TranspilerUtils.ToCodeList(instructions);
                Patch1(original, codes, occurance: 1);
                Patch1(original, codes, occurance: 2);
                Patch2(original, codes, occurance: 3);
                Patch1(original, codes, occurance: 4);
                Patch2(original, codes, occurance: 5);
                return codes;
            } catch (Exception ex) {
                ex.Log();
                throw ex;
            }
        }

        public static void Patch1(MethodBase original, List<CodeInstruction> codes, int occurance) {
            int index = codes.Search(c => c.LoadsField(f_clipSegmentEnds), count: occurance);

            CodeInstruction ldNodeID = GetLDArg(original, "startNodeID");
            CodeInstruction ldSegmentID = GetLDArg(original, "ignoreSegmentID");
            CodeInstruction call = new CodeInstruction(OpCodes.Call, mGetClipSegmentEnd);

            var newCodes = new[] {
                ldNodeID,
                ldSegmentID,
                call,
            };

            codes.InsertInstructions(index + 1, codes, false);
        }

        public static void Patch2(MethodBase original, List<CodeInstruction> codes, int occurance) {
            int index = codes.Search(c => c.LoadsField(f_clipSegmentEnds), count: occurance);

            CodeInstruction ldNodeID = GetLDArg(original, "startNodeID");
            CodeInstruction ldSegmentID = NodesLengthCommons.BuildSegmentLDLocFromPrevSTLoc(codes, index);
            CodeInstruction call = new CodeInstruction(OpCodes.Call, mGetClipSegmentEnd);

            var newCodes = new[] {
                ldNodeID,
                ldSegmentID,
                call,
            };

            codes.InsertInstructions(index + 1, codes, false);
        }
    }
}
