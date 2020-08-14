namespace NodeController.Patches {
    using KianCommons;
    using HarmonyLib;
    using JetBrains.Annotations;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using static KianCommons.Patches.TranspilerUtils;
    using NodeController.Util;
    using UnityEngine;
    using ColossalFramework.Math;

    [UsedImplicitly]
    [HarmonyPatch]
    static class CalculateCornerPatch2 {
        [UsedImplicitly]
        static MethodBase TargetMethod() {
    // public void CalculateCorner(ushort segmentID, bool heightOffset, bool start, bool leftSide,
    // out Vector3 cornerPos, out Vector3 cornerDirection, out bool smooth)
            return typeof(NetSegment).GetMethod(
                    nameof(NetSegment.CalculateCorner),
                    BindingFlags.Public | BindingFlags.Instance) ??
                    throw new System.Exception("CalculateCornerPatch Could not find target method.");
        }

        public static void FixCornerPos(Vector3 nodePos, Vector3 cornerDir, ref Vector3 cornerPos) {
            float d = VectorUtils.DotXZ(cornerPos - nodePos, cornerDir);  
            cornerPos.y = nodePos.y + d * cornerDir.y;
        }

        /// <param name="segmentID">segment to calculate corner</param>
        /// <param name="start">true for start node</param>
        /// <param name="leftSide">going away from the node</param>
        public static void Postfix(
            ushort segmentID, bool heightOffset, bool start, bool leftSide,
            ref Vector3 cornerPos, ref Vector3 cornerDirection) {
            SegmentEndData data = SegmentEndManager.Instance.GetAt(segmentID, start);
            bool flatJunctions = data?.FlatJunctions ?? segmentID.ToSegment().Info.m_flatJunctions;
            if (!flatJunctions) {
                ushort nodeID = segmentID.ToSegment().GetNode(start);
                FixCornerPos(nodeID.ToNode().m_position, cornerDirection, ref cornerPos);
            }

            // manual adjustments:
            data?.ModifyCorner(ref cornerPos, ref cornerDirection, leftSide);
        }
    }
}
