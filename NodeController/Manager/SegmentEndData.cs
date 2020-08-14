namespace NodeController {
    using System;
    using UnityEngine;
    using KianCommons;
    using CSURUtil = Util.CSURUtil;
    using CSUtil.Commons;
    using Log = KianCommons.Log;
    using TrafficManager.Traffic.Impl;
    using System.Drawing.Drawing2D;

    [Serializable]
    public class SegmentEndData {
        // intrinsic
        public ushort NodeID;
        public ushort SegmentID;

        public override string ToString() {
            return GetType().Name + $"(segment:{SegmentID} node:{NodeID})";
        }

        /// <summary>clone</summary>
        public SegmentEndData(SegmentEndData template) =>
            HelpersExtensions.CopyProperties(this, template);

        public SegmentEndData Clone() => new SegmentEndData(this);

        // defaults
        public float DefaultCornerOffset => CSURUtil.GetMinCornerOffset(NodeID);
        public bool DefaultFlatJunctions => NodeID.ToNode().Info.m_flatJunctions;
        public NetSegment.Flags DefaultFlags;

        // cache
        public bool HasPedestrianLanes;
        public float CurveRaduis0;
        public int PedestrianLaneCount;

        // Configurable
        public float CornerOffset;
        public bool FlatJunctions;
        public bool NoCrossings;
        public bool NoMarkings;
        public bool NoJunctionTexture;
        public bool NoJunctionProps; // excluding TL
        public bool NoTLProps;
        public Vector3 LeftCornerPos, LeftCornerDir, RightCornerPos, RightCornerDir;


        // shortcuts
        public ref NetSegment Segment => ref SegmentID.ToSegment();
        public ref NetNode Node => ref NodeID.ToNode();
        public NodeData NodeData => NodeManager.Instance.buffer[NodeID];

        public SegmentEndData(ushort segmentID, ushort nodeID) {
            NodeID = nodeID;
            SegmentID = segmentID;

            Calculate();
            CornerOffset = DefaultCornerOffset;
            FlatJunctions = DefaultFlatJunctions;
        }

        public void Calculate() {
            DefaultFlags = Segment.m_flags;
            PedestrianLaneCount = Info.CountPedestrianLanes();
            Refresh();
        }

        public bool IsDefault() {
            bool  ret = Mathf.Abs(CornerOffset - DefaultCornerOffset) < 0.5f;
            ret &= FlatJunctions == DefaultFlatJunctions;
            ret &= NoCrossings == false;
            ret &= NoMarkings == false;
            ret &= NoJunctionTexture == false;
            ret &= NoJunctionProps == false;
            ret &= NoTLProps == false;
            ret &= RightCornerPos == Vector3.zero;
            ret &= RightCornerDir == Vector3.zero;
            ret &= LeftCornerPos == Vector3.zero;
            ret &= LeftCornerDir == Vector3.zero;

            return ret;
        }

        public void ResetToDefault() {
            CornerOffset = DefaultCornerOffset;
            FlatJunctions = DefaultFlatJunctions;
            NoCrossings = false;
            NoMarkings = false;
            NoJunctionTexture = false;
            NoJunctionProps = false;
            NoTLProps = false;
            RightCornerDir = RightCornerDir = LeftCornerPos = LeftCornerDir = default;
            NetManager.instance.UpdateNode(NodeID);
        }

        public void Refresh() {
            if (!CanModifyOffset()) {
                Log.Debug("SegmentEndData.Refresh(): setting CornerOffset = DefaultCornerOffset");
                CornerOffset = DefaultCornerOffset;
            }
            if (!CanModifyFlatJunctions()) {
                FlatJunctions = DefaultFlatJunctions;
            }
            Log.Debug($"SegmentEndData.Refresh() Updating segment:{SegmentID} node:{NodeID} CornerOffset={CornerOffset}");
            if (HelpersExtensions.VERBOSE)
                Log.Debug(Environment.StackTrace);

            NetManager.instance.UpdateNode(NodeID);
        }

        bool CrossingIsRemoved() =>
            HideCrosswalks.Patches.CalculateMaterialCommons.
            ShouldHideCrossing(NodeID, SegmentID);

        public bool IsCSUR => NetUtil.IsCSUR(Info);
        public NetInfo Info => Segment.Info;
        public bool CanModifyOffset() => NodeData?.CanModifyOffset() ?? false;
        public bool CanModifyFlatJunctions() => NodeData?.CanModifyFlatJunctions()??false;

        public bool ShowClearMarkingsToggle() {
            if (IsCSUR) return false;
            if (NodeData == null) return true;
            return NodeData.NodeType == NodeTypeT.Custom;
        }

        /// <param name="leftSide">left side going away from the junction</param>
        public void ModifyCorner(ref Vector3 cornerPos, ref Vector3 cornerDir, bool leftSide) {
            Vector3 leftwardDir = Vector3.Cross(Vector3.up, cornerDir).normalized; // going away from the junction
            Vector3 rightwardDir = -leftwardDir;
            Vector3 forwardDir = new Vector3(cornerDir.x, 0, cornerDir.z).normalized; // going away from the junction

            Vector3 deltaPos;
            Vector3 deltaDir;

            if (leftSide) {
                deltaPos = TransformCoordinates(LeftCornerPos, leftwardDir, Vector3.up, forwardDir);
                deltaDir = TransformCoordinates(LeftCornerDir, leftwardDir, Vector3.up, forwardDir);
            } else {
                deltaPos = TransformCoordinates(RightCornerPos, rightwardDir, Vector3.up, forwardDir);
                deltaDir = TransformCoordinates(RightCornerDir, rightwardDir, Vector3.up, forwardDir);
            }
            cornerPos += deltaPos;
            cornerDir += deltaDir;
        }

        /// <summary>
        /// tranforms input vector from relative (to x y x inputs) coordinate to absulute coodinate.
        /// </summary>
        public static Vector3 TransformCoordinates(Vector3 v, Vector3 x, Vector3 y, Vector3 z) 
            => v.x * x + v.y * y + v.z * z;

        #region External Mods
        public TernaryBool ShouldHideCrossingTexture() {
            if (NodeData !=null && NodeData.NodeType == NodeTypeT.Stretch)
                return TernaryBool.False; // always ignore.
            if (NoMarkings)
                return TernaryBool.True; // always hide
            return TernaryBool.Undefined; // default.
        }
        #endregion
    }
}
