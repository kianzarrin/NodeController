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
    using UnityEngine.UI;
    using static ColossalFramework.Math.VectorUtils;

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
            float d = DotXZ(cornerPos - nodePos, NormalizeXZ(cornerDir));  
            cornerPos.y = nodePos.y + d * cornerDir.y;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodePos"></param>
        /// <param name="cornerDir"></param>
        /// <param name="cornerPos"></param>
        /// <param name="leftSide">going away from the junction</param>
        public static void FixCornerPosMinor(Vector3 nodePos, Vector3 neighbourEndDir,
            ref Vector3 cornerDir, ref Vector3 cornerPos) {
            Vector3 nighbourEndDirXZ = NormalizeXZ(neighbourEndDir);
            float d = DotXZ(cornerPos - nodePos, nighbourEndDirXZ);
            cornerPos.y = nodePos.y + d * neighbourEndDir.y;

            Vector3 dirXZ = NormalizeXZ(cornerDir);
            float acos = DotXZ(nighbourEndDirXZ, dirXZ);
            cornerDir.y = neighbourEndDir.y * acos;
        }
        /// <param name="segmentID">segment to calculate corner</param>
        /// <param name="start">true for start node</param>
        /// <param name="leftSide">going away from the node</param>
        public static void Postfix(
            ushort segmentID, bool heightOffset, bool start, bool leftSide,
            ref Vector3 cornerPos, ref Vector3 cornerDirection) {
            SegmentEndData data = SegmentEndManager.Instance.GetAt(segmentID, start);
            bool flatJunctions = data?.FlatJunctions ?? segmentID.ToSegment().Info.m_flatJunctions;
            ushort nodeID = segmentID.ToSegment().GetNode(start);
            if (!flatJunctions) {
                FixCornerPos(nodeID.ToNode().m_position, cornerDirection, ref cornerPos);
            } else {
                // left segment going away from the node is right segment going toward the node.
                ushort neighbourSegmentID = leftSide
                    ? segmentID.ToSegment().GetRightSegment(nodeID)
                    : segmentID.ToSegment().GetLeftSegment(nodeID);
                var neighbourData = SegmentEndManager.Instance.GetAt(neighbourSegmentID, start);
                bool neighbourFlatJunctions = neighbourData?.FlatJunctions ?? neighbourSegmentID.ToSegment().Info.m_flatJunctions;
                if (neighbourFlatJunctions) {
                    FixCornerPosMinor(
                        nodeID.ToNode().m_position,
                        neighbourSegmentID.ToSegment().GetDirection(nodeID),
                        cornerDir: ref cornerDirection,
                        cornerPos: ref cornerPos);
                }
            }

            // manual adjustments:
            data?.ApplyCornerAdjustments(ref cornerPos, ref cornerDirection, leftSide);
        }
    }
}
