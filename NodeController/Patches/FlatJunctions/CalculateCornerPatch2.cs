namespace NodeController.Patches {
    using ColossalFramework;
    using HarmonyLib;
    using JetBrains.Annotations;
    using KianCommons;
    using System.Reflection;
    using UnityEngine;
    using static ColossalFramework.Math.VectorUtils;
    using NodeController.GUI;
    using KianCommons.Plugins;
    using ColossalFramework.Math;

    [UsedImplicitly]
    [HarmonyPatch]
    static class CalculateCornerPatch2 {
        static void Prepare(MethodBase method) {
            if (method == null) {
                AdaptiveRoadsUtil.OverrideARSharpner();
            }
        }

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
        /// TODO: also give slope if segment comes at an angle.
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

        public static void Sharpen2(
            ushort segmentID1, bool startNode, bool leftSide,
            ref Vector3 cornerPos, ref Vector3 cornerDirection) {
            ref NetSegment segment1 = ref segmentID1.ToSegment();
            ushort nodeID = segment1.GetNode(startNode);

            bool sharp;
            if (NodeManager.Instance.buffer[nodeID] is NodeData nodeData) {
                sharp = nodeData.CanModifySharpCorners() && nodeData.SharpCorners;
            } else {
                sharp = AdaptiveRoadsUtil.GetARSharpCorners(segment1.Info);
            }

            if (!sharp)
                return;

            ref NetNode node = ref nodeID.ToNode();
            int nSegments = node.CountSegments();
            if (nSegments < 2) {
                return;
            }

            ushort segmentId2;
            if (leftSide /*right going toward junction*/) {
                segmentId2 = segment1.GetRightSegment(nodeID);
            } else {
                segmentId2 = segment1.GetLeftSegment(nodeID);
            }
            ref NetSegment segment2 = ref segmentId2.ToSegment();

            Vector3 pos = node.m_position;
            float hw1 = segment1.Info.m_halfWidth;
            float hw2 = segment2.Info.m_halfWidth;
            Vector3 dir1 = NormalizeXZ(segment1.GetDirection(nodeID));
            Vector3 dir2 = NormalizeXZ(segment2.GetDirection(nodeID));
            float sin = Vector3.Cross(dir1, dir2).y;
            sin = -sin;

            static bool SameDir(Vector3 tangent, Vector3 dir) {
                return DotXZ(tangent, NormalizeXZ(dir)) > 0.95f;
            }

            Vector3 otherPos1 = segment1.GetOtherNode(nodeID).ToNode().m_position;
            Vector3 otherPos2 = segment2.GetOtherNode(nodeID).ToNode().m_position;
            bool isStraight1 = SameDir(dir1, otherPos1 - pos);
            bool isStraight2 = SameDir(dir2, otherPos2 - pos);
            if (!isStraight1 || !isStraight2) {
                //var otherDir1 = otherPos1 - pos;
                //var otherDir1n = NormalizeXZ(otherDir1);
                //float dotxz1 = DotXZ(dir1, otherDir1n);
                //Log.Debug($"isStraight1={isStraight1} dir1={dir1} otherDir1={otherDir1} otherDir1.normalizedXZ={otherDir1n} dotxz1={dotxz1}");
                //var otherDir2 = otherPos2 - pos;
                //var otherDir2n = NormalizeXZ(otherDir2);
                //float dotxz2 = DotXZ(dir2, otherDir2n);
                //Log.Debug($"isStraight2={isStraight2} dir2={dir2} otherDir2={otherDir2} otherDir2.normalizedXZ={otherDir2n} dotxz2={dotxz2}");
                return;
            }


            Log.Debug($"p3: node:{nodeID}, segment:{segmentID1} segmentId2:{segmentId2} leftSide={leftSide} sin={sin}", false);
            if (Mathf.Abs(sin) > 0.001) {
                float scale = 1 / sin;
                if (!leftSide)
                    scale = -scale;

                pos += dir2 * hw1 * scale;
                pos += dir1 * hw2 * scale; // intersection.

                const float OFFSET_SAFETYNET = 0.02f;
                cornerPos = pos + dir1 * OFFSET_SAFETYNET;
            }
        }

        /// <param name="segmentID">segment to calculate corner</param>
        /// <param name="start">true for start node</param>
        /// <param name="leftSide">going away from the node</param>
        public static void Postfix(
            ushort segmentID, bool start, bool leftSide,
            ref Vector3 cornerPos, ref Vector3 cornerDirection) {
            Sharpen2(
                segmentID1: segmentID, startNode: start, leftSide: leftSide,
                cornerPos: ref cornerPos, cornerDirection: ref cornerDirection);

            SegmentEndData data = SegmentEndManager.Instance.GetAt(segmentID, start);
            Assertion.AssertNotNull(Settings.GameConfig, "Settings.GameConfig");
            if (data == null && !Settings.GameConfig.UnviversalSlopeFixes)
                return;

            //Log.Debug($"CalculateCorner2.PostFix(segmentID={segmentID} start={start} leftSide={leftSide}): cornerDir={cornerDirection}");
            ApplySlope( segmentID, start, leftSide, ref cornerPos, ref cornerDirection);

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
        }
    }
}
