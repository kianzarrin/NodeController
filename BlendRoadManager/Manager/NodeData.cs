

namespace BlendRoadManager {
    using ColossalFramework;
    using ColossalFramework.Math;
    using System;
    using UnityEngine;
    using Util;
    using TernaryBool = CSUtil.Commons.TernaryBool;
  
    public enum NodeTypeT {
        NoNode=0,
        Crossing=1, // change dataMatrix.w to render crossings in the middle.
        UTurn=2, // set offset to 5.
        CustomNode=3,
        ContinuesSegment=4,
    }

    [Serializable]
    public class NodeData {
        public ushort NodeID;
        public int SegmentCount;

        public float DefaultCornerOffset => NodeID.ToNode().Info.m_minCornerOffset;
        public NetNode.Flags DefaultFlags;

        public NodeTypeT NodeType;
        public NodeTypeT DefaultNodeType;
        public float CornerOffset;

        public float HWDiff;
        public bool HasPedestrianLanes;
        public bool IsStraight;

        public NodeData(ushort nodeID) {
            NodeID = nodeID;
            SegmentCount = nodeID.ToNode().CountSegments();
            DefaultFlags = nodeID.ToNode().m_flags;

            if (DefaultFlags.IsFlagSet(NetNode.Flags.Middle))
                DefaultNodeType = NodeTypeT.NoNode;
            else if (DefaultFlags.IsFlagSet(NetNode.Flags.Junction))
                DefaultNodeType = NodeTypeT.CustomNode;
            else
                throw new NotImplementedException("unsupported node flags: " + DefaultFlags);

            NodeType = DefaultNodeType;
            CornerOffset = DefaultCornerOffset;

            if (SegmentCount == 2) {
                float hw0 = 0;
                Vector2 dir0 = default;
                foreach (ushort segmetnID in NetUtil.GetSegmentsCoroutine(nodeID)) {
                    if (hw0 == 0) {
                        hw0 = segmetnID.ToSegment().Info.m_halfWidth;
                        dir0 = VectorUtils.XZ(segmetnID.ToSegment().GetDirection(nodeID));
                    } else {
                        HWDiff = Mathf.Abs(segmetnID.ToSegment().Info.m_halfWidth - hw0);
                        Vector2 dir1 = VectorUtils.XZ(segmetnID.ToSegment().GetDirection(nodeID));
                        IsStraight = Mathf.Abs(Vector2.Dot(dir0, dir1)+1) < 0.0001f;
                    }
                }
            }

            foreach (ushort segmetnID in NetUtil.GetSegmentsCoroutine(nodeID))
                HasPedestrianLanes |= segmetnID.ToSegment().Info.m_hasPedestrianLanes;
        }

        public bool IsDefault() {
            bool ret = CornerOffset - DefaultCornerOffset < 0.5f;
            ret &= NodeType == DefaultNodeType;
            return ret;
        }

        public void Refresh() {
            if (!CanModifyOffset()) {
                if (NodeType == NodeTypeT.UTurn)
                    CornerOffset = 8f;
                else
                    CornerOffset = DefaultCornerOffset;
            }
            NetManager.instance.UpdateNode(NodeID);
        }

        public bool NeedMiddleFlag() => NodeType == NodeTypeT.NoNode && !IsStraight;
        public bool NeedBendFlag() => NodeType == NodeTypeT.NoNode && IsStraight;
        public bool NeedJunctionFlag() => !NeedMiddleFlag() && !NeedBendFlag();
        public bool WantsTrafficLight() => NodeType == NodeTypeT.CustomNode || NodeType == NodeTypeT.Crossing;
        public bool CanModifyOffset() => NodeType == NodeTypeT.ContinuesSegment || NodeType == NodeTypeT.CustomNode;
        public bool NeedsTransitionFlag() =>
            SegmentCount == 2 &&
            (NodeType == NodeTypeT.CustomNode ||
            NodeType == NodeTypeT.Crossing ||
            NodeType == NodeTypeT.UTurn);


        public static bool IsSupported(ushort nodeID) {
            var flags = nodeID.ToNode().m_flags;
            if (flags.IsFlagSet(NetNode.Flags.LevelCrossing | NetNode.Flags.AsymBackward | NetNode.Flags.AsymForward))
                return false;
            int n = nodeID.ToNode().CountSegments();
            if (n > 2) return true;
            if (n == 2) return nodeID.ToNode().Info.m_netAI is RoadBaseAI;
            return false;
        }

        public bool CanChangeTo(NodeTypeT newNodeType) {
            if (SegmentCount > 2)
                return newNodeType == NodeTypeT.CustomNode;

            switch (newNodeType) {
                case NodeTypeT.Crossing:
                    return HasPedestrianLanes && HWDiff < 0.001f;
                case NodeTypeT.UTurn:
                    return NodeID.ToNode().Info.m_forwardVehicleLaneCount > 0 && NodeID.ToNode().Info.m_backwardVehicleLaneCount > 0;
                case NodeTypeT.ContinuesSegment:
                    return !DefaultFlags.IsFlagSet(NetNode.Flags.Middle); // not middle by default.
                case NodeTypeT.NoNode:
                    return true;// HWDiff < 2f; // TODO options
                case NodeTypeT.CustomNode:
                    return true;
                default:
                    throw new Exception("Unreachable code");
            }
        }

        #region External Mods
        public TernaryBool ShouldHideCrossingTexture() {
            switch (NodeType) {
                case NodeTypeT.Crossing:
                    return TernaryBool.False; // always show
                case NodeTypeT.UTurn:
                    return TernaryBool.True; // allways hide
                case NodeTypeT.ContinuesSegment:
                    return TernaryBool.False; // always don't modify
                case NodeTypeT.NoNode:
                    return TernaryBool.Undefined; // don't care
                case NodeTypeT.CustomNode:
                    return TernaryBool.Undefined; // default
                default:
                    throw new Exception("Unreachable code");
            }
        }

        // undefined -> don't touch prev value
        // true -> force true
        // false -> force false.
        public TernaryBool IsUturnAllowedConfigurable() {
            switch (NodeType) {
                case NodeTypeT.Crossing:
                    return TernaryBool.False; // always off
                case NodeTypeT.UTurn:
                    return TernaryBool.Undefined; // default on
                case NodeTypeT.ContinuesSegment:
                    return TernaryBool.False; // always off
                case NodeTypeT.NoNode:
                    return TernaryBool.Undefined; // don't care
                case NodeTypeT.CustomNode:
                    return TernaryBool.Undefined; // default
                default:
                    throw new Exception("Unreachable code");
            }
        }

        public TernaryBool GetDefaultUturnAllowed() {
            switch (NodeType) {

                case NodeTypeT.Crossing:
                    return TernaryBool.False; // always off
                case NodeTypeT.UTurn:
                    return TernaryBool.True; // default on
                case NodeTypeT.ContinuesSegment:
                    return TernaryBool.False; // always off
                case NodeTypeT.NoNode:
                    return TernaryBool.Undefined; // don't care
                case NodeTypeT.CustomNode:
                    return TernaryBool.Undefined; // default
                default:
                    throw new Exception("Unreachable code");
            }
        }

        public TernaryBool IsPedestrianCrossingAllowedConfigurable() {
            switch (NodeType) {
                case NodeTypeT.Crossing:
                    return TernaryBool.False; // always on
                case NodeTypeT.UTurn:
                    return TernaryBool.False; // always off
                case NodeTypeT.ContinuesSegment:
                    return TernaryBool.False; // always off
                case NodeTypeT.NoNode:
                    return TernaryBool.Undefined;
                case NodeTypeT.CustomNode:
                    if (!HasPedestrianLanes) {
                        return TernaryBool.False; // TODO move to TMPE.
                    }
                    return TernaryBool.Undefined; // default off
                default:
                    throw new Exception("Unreachable code");
            }
        }

        public TernaryBool GetDefaultPedestrianCrossingAllowed() {
            switch (NodeType) {
                case NodeTypeT.Crossing:
                    return TernaryBool.True; // always on
                case NodeTypeT.UTurn:
                    return TernaryBool.False; // default off
                case NodeTypeT.ContinuesSegment:
                    return TernaryBool.False; // always off
                case NodeTypeT.NoNode:
                    return TernaryBool.Undefined; // don't care
                case NodeTypeT.CustomNode:
                    return TernaryBool.False; // default off
                default:
                    throw new Exception("Unreachable code");
            }
        }

        public TernaryBool IsEnteringBlockedJunctionAllowedConfigurable() {
            switch (NodeType) {
                case NodeTypeT.Crossing:
                    return TernaryBool.Undefined; // default off
                case NodeTypeT.UTurn:
                    return TernaryBool.Undefined; // default
                case NodeTypeT.ContinuesSegment:
                    return TernaryBool.False; // always on
                case NodeTypeT.NoNode:
                    return TernaryBool.Undefined; // don't care
                case NodeTypeT.CustomNode:
                    if (SegmentCount > 2)
                        return TernaryBool.Undefined;
                    bool oneway = DefaultFlags.IsFlagSet(NetNode.Flags.OneWayIn) & DefaultFlags.IsFlagSet(NetNode.Flags.OneWayOut);
                    if(oneway & !HasPedestrianLanes) {
                        return TernaryBool.False; // always on.
                    }
                    return TernaryBool.Undefined;
                default:
                    throw new Exception("Unreachable code");
            }
        }

        public TernaryBool GetDefaultEnteringBlockedJunctionAllowed() {
            switch (NodeType) {
                case NodeTypeT.Crossing:
                    return TernaryBool.False; // default off
                case NodeTypeT.UTurn:
                    return TernaryBool.Undefined; // default
                case NodeTypeT.ContinuesSegment:
                    return TernaryBool.True; // always on
                case NodeTypeT.NoNode:
                    return TernaryBool.Undefined; // don't care
                case NodeTypeT.CustomNode:
                    if (SegmentCount > 2)
                        return TernaryBool.Undefined;
                    bool oneway = DefaultFlags.IsFlagSet(NetNode.Flags.OneWayIn) & DefaultFlags.IsFlagSet(NetNode.Flags.OneWayOut);
                    if (oneway & !HasPedestrianLanes) {
                        return TernaryBool.True; // always on.
                    }
                    return TernaryBool.Undefined;
                default:
                    throw new Exception("Unreachable code");
            }
        }
        #endregion

        #region old code
        //public void ChangeNodeType() {
        //    if (!CanModifyNodeType()) {
        //        throw new Exception("cannot change junction type");
        //    } else if (DefaultNodeType == NodeTypeT.Node) {
        //        switch (NodeType) {
        //            case NodeTypeT.Node :
        //                NodeType = NodeTypeT.Middle ;
        //                break;
        //            case NodeTypeT.Middle:
        //                NodeType = NodeTypeT.Crossing ;
        //                break;
        //            case NodeTypeT.Crossing:
        //                NodeType = NodeTypeT.Segment ;
        //                break;
        //            case NodeTypeT.Segment:
        //                NodeType = NodeTypeT.Node ;
        //                break;
        //            default:
        //                throw new Exception("Unreachable code");
        //        }
        //    }
        //    if (DefaultNodeType == NodeTypeT.Middle) {
        //        NodeType++;
        //        if (NodeType > HelpersExtensions.GetMaxEnumValue<NodeTypeT>())
        //            NodeType = 0;
        //    }
        //}

        //public const float OFFSET_STEP = 5f;
        ///// <summary>
        ///// in case of overflow resets type and return true.
        ///// </summary>
        //public void IncrementOffset() {
        //    CornerOffset += OFFSET_STEP;
        //    if (CornerOffset > OFFSET_STEP * 10)
        //        CornerOffset = 0;
        //}
        #endregion old code
    }
}
