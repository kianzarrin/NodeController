namespace NodeController {
    using System;
    using UnityEngine;
    using KianCommons;
    using CSURUtil = Util.CSURUtil;
    using CSUtil.Commons;
    using Log = KianCommons.Log;
    using TrafficManager.Traffic.Impl;

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
        public NetSegment.Flags DefaultFlags;

        // cache
        public bool HasPedestrianLanes;
        public float CurveRaduis0;
        public int PedestrianLaneCount;

        // Configurable
        public float CornerOffset;
        public bool NoCrossings;
        public bool NoMarkings;
        public bool NoJunctionTexture;
        public bool NoJunctionProps; // excluding TL
        public bool NoTLProps;

        // shortcuts
        public ref NetSegment Segment => ref SegmentID.ToSegment();
        public ref NetNode Node => ref NodeID.ToNode();
        public NodeData NodeData => NodeManager.Instance.buffer[NodeID];

        public SegmentEndData(ushort segmentID, ushort nodeID) {
            NodeID = nodeID;
            SegmentID = segmentID;

            Calculate();
            CornerOffset = DefaultCornerOffset;
        }

        public void Calculate() {
            DefaultFlags = Segment.m_flags;
            PedestrianLaneCount = Info.CountPedestrianLanes();
            Refresh();
        }

        public bool IsDefault() {
            bool  ret = Mathf.Abs(CornerOffset - DefaultCornerOffset) < 0.5f;
            ret &= NoCrossings == false;
            ret &= NoMarkings == false;
            ret &= NoJunctionTexture == false;
            ret &= NoJunctionProps == false;
            ret &= NoTLProps == false;
            return ret;
        }

        public void ResetToDefault() {
            CornerOffset = DefaultCornerOffset;
            NoCrossings = false;
            NoMarkings = false;
            NoJunctionTexture = false;
            NoJunctionProps = false;
            NoTLProps = false;
            NetManager.instance.UpdateNode(NodeID);
        }

        public void Refresh() {
            if (!CanModifyOffset()) {
                Log.Debug("SegmentEndData.Refresh(): setting CornerOffset = DefaultCornerOffset");
                CornerOffset = DefaultCornerOffset;
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
        public bool CanModifyOffset() => NodeData?.CanModifyOffset() ?? true;
        public bool ShowClearMarkingsToggle() {
            if (IsCSUR) return false;
            if (NodeData == null) return true;
            return NodeData.NodeType == NodeTypeT.Custom;
        }

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
