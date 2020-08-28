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
    using ColossalFramework;

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

        public static void FixCornerPos(Vector3 nodePos, Vector3 segmentEndDir, ref Vector3 cornerPos) {
            // NetSegment.FindDirection() calculates segmentEndDir such that lenxz = 1 regardless of y
            float d = DotXZ(cornerPos - nodePos, segmentEndDir);  
            cornerPos.y = nodePos.y + d * segmentEndDir.y;
        }

        public static void FixCornerPosMinor(Vector3 nodePos, Vector3 neighbourEndDir, ref Vector3 cornerDir, ref Vector3 cornerPos) {
            float d = DotXZ(cornerPos - nodePos, neighbourEndDir);
            cornerPos.y = nodePos.y + d * neighbourEndDir.y;

            float acos = DotXZ(cornerDir, neighbourEndDir);
            cornerDir.y = neighbourEndDir.y * acos;

        }

        /// <param name="segmentID">segment to calculate corner</param>
        /// <param name="start">true for start node</param>
        /// <param name="leftSide">going away from the node</param>
        public static void Postfix(
            ushort segmentID, bool start, bool leftSide,
            ref Vector3 cornerPos, ref Vector3 cornerDirection)
        {
            SegmentEndData data = SegmentEndManager.Instance.GetAt(segmentID, start);
            ushort nodeID = segmentID.ToSegment().GetNode(start);
            bool middle = nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Middle);
            if (!middle) {
                bool flatJunctions = data?.FlatJunctions ?? segmentID.ToSegment().Info.m_flatJunctions;
                bool slope = !flatJunctions;
                if (slope) {
                    FixCornerPos(
                        nodeID.ToNode().m_position,
                        segmentID.ToSegment().GetDirection(nodeID),
                        ref cornerPos);
                } else {
                    // left segment going away from the node is right segment going toward the node.
                    ushort neighbourSegmentID = leftSide
                        ? segmentID.ToSegment().GetRightSegment(nodeID)
                        : segmentID.ToSegment().GetLeftSegment(nodeID);
                    var neighbourData = SegmentEndManager.Instance.GetAt(neighbourSegmentID, nodeID);

                    bool neighbourFlatJunctions = neighbourData?.FlatJunctions ?? neighbourSegmentID.ToSegment().Info.m_flatJunctions;
                    bool neighbourslope = !neighbourFlatJunctions;
                    bool twist = true;// segmentID.ToSegment().Info.m_twistSegmentEnds;
                    if (twist && neighbourslope) {
                        FixCornerPosMinor(
                            nodePos: nodeID.ToNode().m_position,
                            neighbourEndDir: neighbourSegmentID.ToSegment().GetDirection(nodeID),
                            cornerDir: ref cornerDirection,
                            cornerPos: ref cornerPos);
                    }
                }
            }
            //Log.DebugWait($"flat junction is {FlatJunctions} at {this}", seconds: 0.1f);
            // manual adjustments:
            data?.ApplyCornerAdjustments(ref cornerPos, ref cornerDirection, leftSide);
        }
    }
}
