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
    using UnityEngine;

    internal static class NodesLengthCommons {
        // set node length to 0 when nodeless
        static int NodeLength(int length0, ushort nodeID, ushort segmentID) {
            var segmentData = SegmentEndManager.Instance.
                GetAt(segmentID: segmentID, nodeID: nodeID);
            bool nodeless;
            if (segmentData == null)
                nodeless = !segmentID.ToSegment().Info.m_clipSegmentEnds;
            else
                nodeless = segmentData.Nodeless;

            if (nodeless)
                return 0;
            else
                return length0;
        }

        static FieldInfo f_nodes =>
            ReflectionHelpers.GetField<NetInfo>(nameof(NetInfo.m_nodes));

        static MethodInfo mNodeLength = ReflectionHelpers.GetMethod(
            typeof(NodesLengthCommons), nameof(NodeLength));


        internal static void Patch(
            List<CodeInstruction> codes, MethodBase original, int occurance, int counterGetSegment) {
            int iNodesLength = codes.Search(
                predicate: (int i) => i > 1 && codes[i - 1].LoadsField(f_nodes) && codes[i].opcode == OpCodes.Ldlen,
                count: occurance);
            Assertion.Assert(codes[iNodesLength].opcode == OpCodes.Ldlen);

            CodeInstruction ldargNodeID = GetLDArg(original, "nodeID");
            CodeInstruction loadSegmentID = BuildSegmentLDLocFromPrevSTLoc(codes, iNodesLength, counterGetSegment); ;
            CodeInstruction callNodeLength = new CodeInstruction(OpCodes.Call, mNodeLength);

            var newCodes = new[] {
                ldargNodeID,
                loadSegmentID,
                callNodeLength,
            };

            codes.InsertInstructions(iNodesLength + 1 /*insert after*/, newCodes, moveLabels:false);
        }

        static MethodInfo mGetSegment => typeof(NetNode).GetMethod("GetSegment", throwOnError: true);
        public static CodeInstruction BuildSegmentLDLocFromPrevSTLoc(
            List<CodeInstruction> codes, int index, int counter = 1) {
            if (counter == 0)
                return new CodeInstruction(OpCodes.Ldc_I4_0); // load 0u

            index = codes.Search(c => c.Calls(mGetSegment), startIndex: index, count: counter * -1);
            index = codes.Search(c => c.IsStloc(), startIndex: index);
            return codes[index].BuildLdLocFromStLoc();
        }
    }
}
