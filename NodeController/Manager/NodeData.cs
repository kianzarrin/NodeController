namespace NodeController {
    using ColossalFramework;
    using ColossalFramework.Math;
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters;
    using System.Runtime.Serialization.Formatters.Binary;
    using TrafficManager.API.Traffic.Enums;
    using UnityEngine;
    using UnityEngine.Assertions;
    using Util;
    using TernaryBool = CSUtil.Commons.TernaryBool;

    public enum NodeTypeT {
        Middle,
        Bend,
        Stretch,
        Crossing, // change dataMatrix.w to render crossings in the middle.
        UTurn, // set offset to 5.
        Custom,
    }

    [Serializable]
    public class NodeData {
        // intrinsic
        public ushort NodeID;

        // defaults
        public float DefaultCornerOffset => CSURUtil.GetMinCornerOffset(NodeID);
        public bool DefaultFlatJunctions => NodeID.ToNode().Info.m_flatJunctions;
        public NetNode.Flags DefaultFlags;
        public NodeTypeT DefaultNodeType;

        // cache
        public bool HasPedestrianLanes;
        public int SegmentCount;
        public float CurveRaduis0;

        // cache only for segment count == 2
        public float HWDiff;
        public int PedestrianLaneCount;
        public bool IsStraight;
        ushort segmentID1, segmentID2;

        // Configurable
        public NodeTypeT NodeType;
        public float CornerOffset;
        public bool FlatJunctions;
        public bool ClearMarkings;
        public bool FirstTimeTrafficLight; // turn on traffic light when inserting pedestrian node for the first time.

        public NodeData(ushort nodeID) {
            NodeID = nodeID;
            Calculate();
            NodeType = DefaultNodeType;
            CornerOffset = DefaultCornerOffset;
            FlatJunctions = DefaultFlatJunctions;
            FirstTimeTrafficLight = NodeType == NodeTypeT.Crossing;
        }

        public NodeData(ushort nodeID, NodeTypeT nodeType) : this(nodeID) {
            NodeType = nodeType;
            FirstTimeTrafficLight = NodeType == NodeTypeT.Crossing;
        }

        public void Calculate() {
            SegmentCount = NodeID.ToNode().CountSegments();
            DefaultFlags = NodeID.ToNode().m_flags;

            if (DefaultFlags.IsFlagSet(NetNode.Flags.Middle))
                DefaultNodeType = NodeTypeT.Middle;
            else if (DefaultFlags.IsFlagSet(NetNode.Flags.Bend))
                DefaultNodeType = NodeTypeT.Bend;
            else if (DefaultFlags.IsFlagSet(NetNode.Flags.Junction))
                DefaultNodeType = NodeTypeT.Custom;
            else
                throw new NotImplementedException("unsupported node flags: " + DefaultFlags);

            if (SegmentCount == 2) {
                float hw0 = 0;
                Vector2 dir0 = default;
                foreach (ushort segmentID in NetUtil.GetSegmentsCoroutine(NodeID)) {
                    int nPedLanes = segmentID.ToSegment().Info.CountPedestrianLanes();
                    if (hw0 == 0) {
                        segmentID1 = segmentID;
                        hw0 = segmentID.ToSegment().Info.m_halfWidth;
                        dir0 = VectorUtils.XZ(segmentID.ToSegment().GetDirection(NodeID));
                        dir0.Normalize();
                        PedestrianLaneCount = nPedLanes;
                    } else {
                        segmentID2 = segmentID;
                        HWDiff = Mathf.Abs(segmentID.ToSegment().Info.m_halfWidth - hw0);
                        Vector2 dir1 = VectorUtils.XZ(segmentID.ToSegment().GetDirection(NodeID));
                        dir1.Normalize();
                        IsStraight = Mathf.Abs(Vector2.Dot(dir0, dir1) + 1) < 0.001f;
                        PedestrianLaneCount = Math.Max(PedestrianLaneCount, nPedLanes);
                    }
                }
            }

            foreach (ushort segmetnID in NetUtil.GetSegmentsCoroutine(NodeID))
                HasPedestrianLanes |= segmetnID.ToSegment().Info.m_hasPedestrianLanes;

            Refresh();
        }

        public bool IsDefault() {
            bool ret = NodeType == DefaultNodeType;
            ret &= ClearMarkings == false;
            ret &= FlatJunctions == DefaultFlatJunctions;
            ret = ret && Mathf.Abs(CornerOffset - DefaultCornerOffset) < 0.5f;
            return ret;
        }

        public void ResetToDefault() {
            NodeType = DefaultNodeType;
            CornerOffset = DefaultCornerOffset;
            ClearMarkings = false;
            FlatJunctions = DefaultFlatJunctions;
            NetManager.instance.UpdateNode(NodeID);
        }

        public void Refresh() {
            if (NodeType != NodeTypeT.Custom)
                ClearMarkings = false;

            if (!CanModifyOffset()) {
                if (NodeType == NodeTypeT.UTurn)
                    CornerOffset = 8f;
                else if (NodeType == NodeTypeT.Crossing)
                    CornerOffset = 0f;
                else if (NodeType != NodeTypeT.Custom)
                    CornerOffset = DefaultCornerOffset;
            }
            if(!CanModifyFlatJunctions()) {
                FlatJunctions = DefaultFlatJunctions;
            }
            Log.Debug($"NodeData.Refresh() Updating node:{NodeID}");
            if (HelpersExtensions.VERBOSE)
                Log.Debug(Environment.StackTrace);

            NetManager.instance.UpdateNode(NodeID);
        }

        bool CrossingIsRemoved(ushort segmentId) =>
            HideCrosswalks.Patches.CalculateMaterialCommons.
            ShouldHideCrossing(NodeID, segmentId);

        public bool IsCSUR => NetUtil.IsCSUR(Info);
        public NetInfo Info => NodeID.ToNode().Info;
        public bool NeedMiddleFlag() => NodeType == NodeTypeT.Middle;
        public bool NeedBendFlag() => NodeType == NodeTypeT.Bend;
        public bool NeedJunctionFlag() => !NeedMiddleFlag() && !NeedBendFlag();
        public bool WantsTrafficLight() => NodeType == NodeTypeT.Crossing;
        public bool CanModifyOffset() => NodeType == NodeTypeT.Stretch || NodeType == NodeTypeT.Custom;
        public bool CanModifyFlatJunctions() => false;//!NeedMiddleFlag();
        public bool ShowClearMarkingsToggle() => NodeType == NodeTypeT.Custom && !IsCSUR;
        public bool IsAsymRevert() => DefaultFlags.IsFlagSet(NetNode.Flags.AsymBackward | NetNode.Flags.AsymForward);

        public bool NeedsTransitionFlag() =>
            SegmentCount == 2 &&
            (NodeType == NodeTypeT.Custom ||
            NodeType == NodeTypeT.Crossing ||
            NodeType == NodeTypeT.UTurn);

        public bool ShouldRenderCenteralCrossingTexture() =>
            NodeType == NodeTypeT.Crossing &&
            CrossingIsRemoved(segmentID1) &&
            CrossingIsRemoved(segmentID2);

        public static bool IsSupported(ushort nodeID) {
            var flags = nodeID.ToNode().m_flags;
            if (!flags.CheckFlags(
                required:  NetNode.Flags.Created,
                forbidden: NetNode.Flags.LevelCrossing | NetNode.Flags.End |
                           NetNode.Flags.Outside | NetNode.Flags.Deleted)) {
                return false;
            }

            int n = nodeID.ToNode().CountSegments();
            if (n > 2)
                return true;
            var info = nodeID.ToNode().Info;
            if (n == 2)
                return info.m_netAI is RoadBaseAI && !NetUtil.IsCSUR(info)!;
            return false;
        }

        public bool CanChangeTo(NodeTypeT newNodeType) {
            if (SegmentCount > 2 || IsCSUR)
                return newNodeType == NodeTypeT.Custom;

            switch (newNodeType) {
                case NodeTypeT.Crossing:
                    return PedestrianLaneCount >= 2 && HWDiff < 0.001f && IsStraight;
                case NodeTypeT.UTurn:
                    return Info.m_forwardVehicleLaneCount > 0 && Info.m_backwardVehicleLaneCount > 0;
                case NodeTypeT.Stretch:
                    return !DefaultFlags.IsFlagSet(NetNode.Flags.Middle) && IsStraight;
                case NodeTypeT.Middle:
                    return IsStraight;
                case NodeTypeT.Bend:
                    return DefaultFlags.IsFlagSet(NetNode.Flags.Bend) || HWDiff > 0.05f;// || !IsStraight;
                case NodeTypeT.Custom:
                    return true;
                default:
                    throw new Exception("Unreachable code");
            }
        }

        public string ToolTip(NodeTypeT nodeType) {
            switch (nodeType) {
                case NodeTypeT.Crossing:
                    return "Crossing node.";
                case NodeTypeT.Middle:
                    return "Middle: No node.";
                case NodeTypeT.Bend:
                    if (IsAsymRevert())
                        return "Bend: Asymmetrical road changes direction.";
                    if (HWDiff > 0.05f)
                        return "Bend: Linearly match segment widths. ";
                    return "Bend: Simple road corner.";
                case NodeTypeT.Stretch:
                    return "Stretch: Match both pavement and road.";
                case NodeTypeT.UTurn:
                    return "U-Turn: node with enough space for U-Turn.";
                case NodeTypeT.Custom:
                    return "Custom: transition size and traffic rules are configrable.";
            }
            return null;
        }

        #region External Mods
        public TernaryBool ShouldHideCrossingTexture() {
            if (NodeType == NodeTypeT.Stretch)
                return TernaryBool.False; // always ignore.
            if (ClearMarkings)
                return TernaryBool.True; // always hide
            return TernaryBool.Undefined; // default.
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
                case NodeTypeT.Stretch:
                    return TernaryBool.False; // always off
                case NodeTypeT.Middle:
                case NodeTypeT.Bend:
                    return TernaryBool.False; // always default
                case NodeTypeT.Custom:
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
                case NodeTypeT.Stretch:
                    return TernaryBool.False; // always off
                case NodeTypeT.Middle:
                case NodeTypeT.Bend:
                    return TernaryBool.Undefined; // don't care
                case NodeTypeT.Custom:
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
                case NodeTypeT.Stretch:
                    return TernaryBool.False; // always off
                case NodeTypeT.Middle:
                case NodeTypeT.Bend:
                    return TernaryBool.False; // always off
                case NodeTypeT.Custom:
                    if (SegmentCount == 2 && !HasPedestrianLanes) {
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
                case NodeTypeT.Stretch:
                    return TernaryBool.False; // always off
                case NodeTypeT.Middle:
                case NodeTypeT.Bend:
                    return TernaryBool.False; // always off
                case NodeTypeT.Custom:
                    if (SegmentCount == 2)
                        return TernaryBool.False; // default off
                    return TernaryBool.Undefined; // don't care
                default:
                    throw new Exception("Unreachable code");
            }
        }

        public TernaryBool CanHaveTrafficLights(out ToggleTrafficLightError reason) {
            reason = ToggleTrafficLightError.None;
            switch (NodeType) {
                case NodeTypeT.Crossing:
                case NodeTypeT.UTurn:
                    return TernaryBool.Undefined;
                case NodeTypeT.Stretch:
                case NodeTypeT.Middle:
                case NodeTypeT.Bend:
                    reason = ToggleTrafficLightError.NoJunction;
                    return TernaryBool.False; 
                case NodeTypeT.Custom:
                    return TernaryBool.Undefined; // default off
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
                case NodeTypeT.Stretch:
                    return TernaryBool.False; // always on
                case NodeTypeT.Middle:
                case NodeTypeT.Bend:
                    return TernaryBool.False; // always default
                case NodeTypeT.Custom:
                    if (SegmentCount > 2)
                        return TernaryBool.Undefined;
                    bool oneway = DefaultFlags.IsFlagSet(NetNode.Flags.OneWayIn) & DefaultFlags.IsFlagSet(NetNode.Flags.OneWayOut);
                    if (oneway & !HasPedestrianLanes) {
                        return TernaryBool.False; // always on.
                    }
                    return TernaryBool.Undefined; // default on.
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
                case NodeTypeT.Stretch:
                    return TernaryBool.True; // always on
                case NodeTypeT.Middle:
                case NodeTypeT.Bend:
                    return TernaryBool.Undefined; // don't care
                case NodeTypeT.Custom:
                    if (SegmentCount > 2)
                        return TernaryBool.Undefined;
                    return TernaryBool.True;
                //bool oneway = DefaultFlags.IsFlagSet(NetNode.Flags.OneWayIn) & DefaultFlags.IsFlagSet(NetNode.Flags.OneWayOut);
                //if (oneway & !HasPedestrianLanes) {
                //    return TernaryBool.True; // always on.
                //}
                //return TernaryBool.Undefined;
                default:
                    throw new Exception("Unreachable code");
            }
        }
        #endregion
    }
}
