namespace NodeController.Patches.Corner {
    using ColossalFramework;
    using HarmonyLib;
    using JetBrains.Annotations;
    using KianCommons;
    using NodeController.GUI;
    using System;
    using System.Reflection;
    using UnityEngine;
    using static ColossalFramework.Math.VectorUtils;

    [UsedImplicitly]
    [HarmonyPatch]
    static class CalculateCorner_MainPatch {
        [UsedImplicitly]
        static MethodBase TargetMethod() {
            // public void CalculateCorner(ushort segmentID, bool heightOffset, bool start, bool leftSide,
            // out Vector3 cornerPos, out Vector3 cornerDirection, out bool smooth)
            return typeof(NetSegment).GetMethod(
                    nameof(NetSegment.CalculateCorner),
                    BindingFlags.Public | BindingFlags.Instance) ??
                    throw new System.Exception("CalculateCornerPatch Could not find target method.");
        }

        /// <summary>
        /// give slope to junction
        /// </summary>
        public static void FixCornerPos(Vector3 nodePos, Vector3 segmentEndDir, ref Vector3 cornerPos) {
            // NetSegment.FindDirection() calculates segmentEndDir such that lenxz = 1 regardless of y
            float d = DotXZ(cornerPos - nodePos, segmentEndDir);
            cornerPos.y = nodePos.y + d * segmentEndDir.y;
        }


        /// <summary>
        /// embank segment end to match slope of the junction.
        /// </summary>
        public static void FixCornerPosMinor(Vector3 nodePos, Vector3 neighbourEndDir, ref Vector3 cornerDir, ref Vector3 cornerPos) {
            float d = DotXZ(cornerPos - nodePos, neighbourEndDir);
            cornerPos.y = nodePos.y + d * neighbourEndDir.y;

            float acos = DotXZ(cornerDir, neighbourEndDir);
            cornerDir.y = neighbourEndDir.y * acos;
        }

        static void ApplySlope(
            ushort segmentID, bool start, bool leftSide,
            ref Vector3 cornerPos, ref Vector3 cornerDirection) {
            SegmentEndData data = SegmentEndManager.Instance.GetAt(segmentID, start);
            ushort nodeID = segmentID.ToSegment().GetNode(start);
            bool middle = nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Middle);
            bool untouchable = nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Untouchable);
            if (!middle) {
                bool flatJunctions = data?.FlatJunctions ??
                    untouchable || segmentID.ToSegment().Info.m_flatJunctions;
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
                    //var neighbourData = SegmentEndManager.Instance.GetAt(neighbourSegmentID, nodeID);
                    //bool neighbourFlatJunctions = neighbourData?.FlatJunctions ?? neighbourSegmentID.ToSegment().Info.m_flatJunctions;

                    bool twist;
                    if (data != null)
                        twist = data.CanModifyTwist() && data.Twist;
                    else {
                        twist = !untouchable && segmentID.ToSegment().Info.m_flatJunctions;
                        twist = twist && SegmentEndData.CanTwist(segmentID: segmentID, nodeID: nodeID);
                    }

                    if (twist) {
                        Vector3 nodePos = nodeID.ToNode().m_position;
                        Vector3 neighbourEndDir = neighbourSegmentID.ToSegment().GetDirection(nodeID);
                        //if (data != null) {
                        //    Log.Debug($"calling FixCornerPosMinor(" +
                        //        $"nodePos: {nodePos}, neighbourEndDir: {neighbourEndDir}, \n" +
                        //        $"cornerDir: ref {cornerDirection}, cornerPos: ref {cornerPos}) : {data} ");
                        //}

                        FixCornerPosMinor(
                            nodePos: nodePos,
                            neighbourEndDir: neighbourEndDir,
                            cornerDir: ref cornerDirection,
                            cornerPos: ref cornerPos);

                        //if (data != null) {
                        //    Log.Debug($"output FixCornerPosMinor->" +
                        //        $"(cornerDir: ref {cornerDirection}, cornerPos: ref {cornerPos}) : {data} ");
                        //}
                    }
                }
            }
        }

        /// <param name="segmentID">segment to calculate corner</param>
        /// <param name="start">true for start node</param>
        /// <param name="leftSide">going away from the node</param>
        public static void Postfix(
            ushort segmentID, bool start, bool leftSide,
            ref Vector3 cornerPos, ref Vector3 cornerDirection) {
            try {

                SegmentEndData data = SegmentEndManager.Instance.GetAt(segmentID, start);
                Assertion.AssertNotNull(NCSettings.GameConfig, "Settings.GameConfig");
                if (data == null && !NCSettings.GameConfig.UnviversalSlopeFixes)
                    return;

                //Log.Debug($"CalculateCorner2.PostFix(segmentID={segmentID} start={start} leftSide={leftSide}): cornerDir={cornerDirection}");
                ApplySlope(segmentID, start, leftSide, ref cornerPos, ref cornerDirection);

                if (data != null) {
                    // manual adjustments:
                    data.ApplyCornerAdjustments(ref cornerPos, ref cornerDirection, leftSide);
                } else {
                    // if vector dir is not limited inside ApplyCornerAdjustments then do it here.
                    // this must NOT be done before ApplyCornerAdjustments().
                    float absY = Mathf.Abs(cornerDirection.y);
                    if (absY > 2) {
                        // fix dir length so that y is 2:
                        cornerDirection *= 2 / absY;
                    }
                }
            } catch(Exception ex) { ex.Log(); }
        }

        public static void Finalizer(Exception __exception) => __exception?.Log();
    }
}
