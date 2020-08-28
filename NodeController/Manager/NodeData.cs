namespace NodeController {
    using System;
    using System.Runtime.Serialization;
    using System.Collections.Generic;
    using UnityEngine;
    using ColossalFramework;
    using static ColossalFramework.Math.VectorUtils;
    using TrafficManager.API.Traffic.Enums;
    using TernaryBool = CSUtil.Commons.TernaryBool;
    using KianCommons;
    using Log = KianCommons.Log;
    using static KianCommons.HelpersExtensions;
    using KianCommons.Math;

    public enum NodeTypeT {
        Middle,
        Bend,
        Stretch,
        Crossing, // change dataMatrix.w to render crossings in the middle.
        UTurn, // set offset to 5.
        Custom,
        End,
    }

    [Serializable]
    public class NodeData : ISerializable {
        public override string ToString() => $"NodeData(id:{NodeID} type:{NodeType})";
        #region serialization
        public NodeData() { } // so that the code compiles

        //serialization
        public void GetObjectData(SerializationInfo info, StreamingContext context) =>
            SerializationUtil.GetObjectFields(info, this);

        // deserialization
        public NodeData(SerializationInfo info, StreamingContext context) {
            SerializationUtil.SetObjectFields(info, this);

            if (NodeManager.TargetNodeID != 0)// backward compatibility.
                NodeID = NodeManager.TargetNodeID;

            // corner offset and clear markings
            SerializationUtil.SetObjectProperties(info, this);
        }

        public NodeData(NodeData template) {
            HelpersExtensions.CopyProperties(this, template);
        }

        public NodeData Clone() => new NodeData(this);

        #endregion


        // intrinsic
        public ushort NodeID;

        // defaults
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
        public bool Is180;
        ushort segmentID1, segmentID2;
        SegmentEndData SegmentEnd1 => SegmentEndManager.Instance.GetAt(segmentID1, NodeID);
        SegmentEndData SegmentEnd2 => SegmentEndManager.Instance.GetAt(segmentID2, NodeID);

        // Configurable
        public NodeTypeT NodeType;

        #region bulk edit
        #region corner offset
        public float CornerOffset {
            get {
                float ret = 0;
                int count = 0;
                for (int i = 0; i < 8; ++i) {
                    ushort segmentID = Node.GetSegment(i);
                    if (segmentID == 0) continue;
                    var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: NodeID);
                    ret += segEnd.CornerOffset;
                    count++;
                }
                ret /= count;
                return ret;
            }
            set {
                for (int i = 0; i < 8; ++i) {
                    ushort segmentID = Node.GetSegment(i);
                    if (segmentID == 0) continue;
                    var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: NodeID);
                    segEnd.CornerOffset = value;
                }
            }
        }

        public bool HasUniformCornerOffset() {
            float cornerOffset0 = -1;
            for (int i = 0; i < 8; ++i) {
                ushort segmentID = Node.GetSegment(i);
                if (segmentID == 0) continue;
                var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: NodeID);
                if (cornerOffset0 == -1)
                    cornerOffset0 = segEnd.CornerOffset;
                else if (cornerOffset0 != segEnd.CornerOffset)
                    return false;
            }
            return true;
        }
        #endregion
        #region clear markings
        // same as no markings. can't rename for the sake of backward compatibility.
        public bool ClearMarkings {
            get {
                for (int i = 0; i < 8; ++i) {
                    ushort segmentID = Node.GetSegment(i);
                    if (segmentID == 0) continue;
                    var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: NodeID);
                    if (segEnd.NoMarkings)
                        return true;
                }
                return false;
            }
            set {
                //Log.Debug($"ClearMarkings.set() called for node:{NodeID}" + Environment.StackTrace);
                for (int i = 0; i < 8; ++i) {
                    ushort segmentID = Node.GetSegment(i);
                    if (segmentID == 0) continue;
                    var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: NodeID);
                    segEnd.NoMarkings = value;
                }
            }
        }

        public bool HasUniformNoMarkings() {
            bool? noMarkings0 = null;
            for (int i = 0; i < 8; ++i) {
                ushort segmentID = Node.GetSegment(i);
                if (segmentID == 0) continue;
                var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: NodeID);
                if (noMarkings0 == null)
                    noMarkings0 = segEnd.NoMarkings;
                else if (noMarkings0 != segEnd.NoMarkings)
                    return false;
            }
            return true;
        }
        #endregion
        #region embankment angle
        public float EmbankmentAngle {
            get {
                Assert(CanMassEditNodeCorners());
                Assert(SegmentCount == 2);
                float ret = SegmentEnd1.EmbankmentAngleDeg - SegmentEnd2.EmbankmentAngleDeg;
                ret = ret * 0.5f; //average
                return ret;
            }
            set {
                Assert(CanMassEditNodeCorners());
                Assert(SegmentCount == 2);
                SegmentEnd1.EmbankmentAngleDeg = value;
                SegmentEnd2.EmbankmentAngleDeg = -value;
            }
        }

        public bool HasUniformEmbankmentAngle() {
            Assert(CanMassEditNodeCorners());
            Assert(SegmentCount == 2);
            return SegmentEnd1.EmbankmentAngleDeg == -SegmentEnd2.EmbankmentAngleDeg;
        }
        #endregion
        #region slope angle
        public float SlopeAngle {
            get {
                Assert(CanMassEditNodeCorners());
                Assert(SegmentCount == 2);
                float ret = SegmentEnd1.SlopeAngleDeg - SegmentEnd2.SlopeAngleDeg;
                ret = ret * 0.5f; //average
                return ret;
            }
            set {
                Assert(CanMassEditNodeCorners());
                Assert(SegmentCount == 2);
                SegmentEnd1.SlopeAngleDeg = value;
                SegmentEnd2.SlopeAngleDeg = -value;
            }
        }

        public bool HasUniformSlopeAngle() {
            Assert(CanMassEditNodeCorners());
            Assert(SegmentCount == 2);
            return MathUtil.EqualAprox(SegmentEnd1.SlopeAngleDeg, -SegmentEnd2.SlopeAngleDeg, error: 1f);
        }
        #endregion
        #region Stretch
        public float Stretch {
            get {
                Assert(CanMassEditNodeCorners());
                Assert(SegmentCount == 2);
                float ret = SegmentEnd1.Stretch + SegmentEnd2.Stretch;
                ret = ret * 0.5f; //average
                return ret;
            }
            set {
                Assert(CanMassEditNodeCorners());
                Assert(SegmentCount == 2);
                SegmentEnd1.Stretch = value;
                SegmentEnd2.Stretch = value;
            }
        }

        public bool HasUniformStretch() {
            Assert(CanMassEditNodeCorners());
            Assert(SegmentCount == 2);
            return SegmentEnd1.Stretch == SegmentEnd2.Stretch;
        }
        #endregion Strech
        #endregion bulk edit

        public bool FirstTimeTrafficLight; // turn on traffic light when inserting pedestrian node for the first time.

        public ref NetNode Node => ref NodeID.ToNode();

        public NodeData(ushort nodeID) {
            Assert(IsSupported(nodeID));
            NodeID = nodeID;
            Calculate();
            NodeType = DefaultNodeType;
            FirstTimeTrafficLight = false;
            Assert(IsDefault(), "IsDefault()");
            Assert(CanChangeTo(NodeType), $"CanChangeTo(NodeType={NodeType})");
        }

        public NodeData(ushort nodeID, NodeTypeT nodeType) : this(nodeID) {
            NodeType = nodeType;
            FirstTimeTrafficLight = nodeType == NodeTypeT.Crossing;
            // TODO update slope angle.
            Assert(CanChangeTo(NodeType), $"CanChangeTo(NodeType={NodeType})");
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
            else if (DefaultFlags.IsFlagSet(NetNode.Flags.End))
                NodeType = DefaultNodeType = NodeTypeT.End;
            else
                throw new NotImplementedException("unsupported node flags: " + DefaultFlags);

            if (SegmentCount == 2) {
                float hw0 = 0;
                Vector3 dir0 = default;
                foreach (ushort segmentID in NetUtil.IterateNodeSegments(NodeID)) {
                    int nPedLanes = segmentID.ToSegment().Info.CountPedestrianLanes();
                    if (hw0 == 0) {
                        segmentID1 = segmentID;
                        hw0 = segmentID.ToSegment().Info.m_halfWidth;
                        dir0 = NormalizeXZ(segmentID.ToSegment().GetDirection(NodeID));
                        PedestrianLaneCount = nPedLanes;
                    } else {
                        segmentID2 = segmentID;
                        HWDiff = Mathf.Abs(segmentID.ToSegment().Info.m_halfWidth - hw0);
                        var dir1 = NormalizeXZ(segmentID.ToSegment().GetDirection(NodeID));
                        float dot = DotXZ(dir0, dir1);
                        IsStraight = dot < -0.999f; // 180 degrees
                        Is180 = dot > 0.999f; // 0 degrees
                        PedestrianLaneCount = Math.Max(PedestrianLaneCount, nPedLanes);
                    }
                }
            }

            foreach (ushort segmetnID in NetUtil.IterateNodeSegments(NodeID))
                HasPedestrianLanes |= segmetnID.ToSegment().Info.m_hasPedestrianLanes;

            Refresh();
        }

        public IEnumerable<SegmentEndData> IterateSegmentEndDatas() {
            for (int i = 0; i < 8; ++i) {
                ushort segmentID = Node.GetSegment(i);
                if (segmentID == 0) continue;
                yield return SegmentEndManager.Instance.GetAt(segmentID: segmentID, nodeID: NodeID);
            }
        }

        public bool IsDefault() {
            bool isDefault = NodeType == DefaultNodeType;
            if (!isDefault)
                return false;
            foreach(var segEnd in IterateSegmentEndDatas()) { 
                isDefault = segEnd == null || segEnd.IsDefault();
                if (!isDefault)
                    return false;
            }
            return true;
        }

        public void ResetToDefault() {
            NodeType = DefaultNodeType;
            foreach (var segEnd in IterateSegmentEndDatas())
                segEnd?.ResetToDefault();
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
            }

            //Log.Debug($"NodeData.Refresh() Updating node:{NodeID}");
            if (HelpersExtensions.VERBOSE)
                Log.Debug(Environment.StackTrace);

            NetManager.instance.UpdateNode(NodeID);
        }

        bool CrossingIsRemoved(ushort segmentId) =>
            HideCrosswalks.Patches.CalculateMaterialCommons.
            ShouldHideCrossing(NodeID, segmentId);

        public bool IsCSUR => NetUtil.IsCSUR(Info);
        public NetInfo Info => NodeID.ToNode().Info;
        public bool IsRoad => Info.m_netAI is RoadBaseAI;
        public bool EndNode() => NodeType == NodeTypeT.End;
        public bool NeedMiddleFlag() => NodeType == NodeTypeT.Middle;
        public bool NeedBendFlag() => NodeType == NodeTypeT.Bend;
        public bool NeedJunctionFlag() => !NeedMiddleFlag() && !NeedBendFlag() && !EndNode();
        public bool WantsTrafficLight() => NodeType == NodeTypeT.Crossing;
        public bool CanModifyOffset() => NodeType == NodeTypeT.Bend || NodeType == NodeTypeT.Stretch || NodeType == NodeTypeT.Custom;
        public bool CanMassEditNodeCorners() => SegmentCount == 2;
        public bool CanModifyFlatJunctions() => !NeedMiddleFlag();
        public bool IsAsymRevert() => DefaultFlags.IsFlagSet(NetNode.Flags.AsymBackward | NetNode.Flags.AsymForward);
        public bool CanModifyTextures() => IsRoad && !IsCSUR;
        public bool ShowClearMarkingsToggle() => CanModifyTextures() && NodeType == NodeTypeT.Custom ;


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
                required: NetNode.Flags.Created,
                forbidden: NetNode.Flags.LevelCrossing /*| NetNode.Flags.End*/ |
                           NetNode.Flags.Outside | NetNode.Flags.Deleted)) {
                return false;
            }

            int n = nodeID.ToNode().CountSegments();
            if (n != 2) return true;
            var info = nodeID.ToNode().Info;
            //return info.m_netAI is RoadBaseAI && !NetUtil.IsCSUR(info); // TODO support paths/tracks.
            return !NetUtil.IsCSUR(info);
        }

        public bool CanChangeTo(NodeTypeT newNodeType) {
            //Log.Debug($"CanChangeTo({newNodeType}) was called.");
            if (SegmentCount == 1)
                return newNodeType == NodeTypeT.End;

            if (SegmentCount > 2 || IsCSUR)
                return newNodeType == NodeTypeT.Custom;

            bool middle = DefaultFlags.IsFlagSet(NetNode.Flags.Middle);
            // segmentCount == 2 at this point.
            switch (newNodeType) {
                case NodeTypeT.Crossing:
                    return PedestrianLaneCount >= 2 && HWDiff < 0.001f && IsStraight;
                case NodeTypeT.UTurn:
                    return IsRoad && Info.m_forwardVehicleLaneCount > 0 && Info.m_backwardVehicleLaneCount > 0;
                case NodeTypeT.Stretch:
                    return CanModifyTextures() && !middle && IsStraight;
                case NodeTypeT.Bend:
                    return !middle;
                case NodeTypeT.Middle:
                    return IsStraight || Is180; 
                case NodeTypeT.Custom:
                    return true;
                case NodeTypeT.End:
                    return false;
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
                case NodeTypeT.End:
                    return "when there is only one segment at the node.";
            }
            return null;
        }

        #region External Mods
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
                case NodeTypeT.End:
                    return TernaryBool.Undefined;
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
                case NodeTypeT.End:
                    return TernaryBool.Undefined;
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
                case NodeTypeT.End:
                    return TernaryBool.Undefined;
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
                    var netAI1 = segmentID1.ToSegment().Info.m_netAI;
                    var netAI2 = segmentID2.ToSegment().Info.m_netAI;
                    bool sameAIType = netAI1.GetType() == netAI2.GetType(); 
                    if (SegmentCount == 2 && !sameAIType) // eg: at bridge/tunnel entrances.
                        return TernaryBool.False; // default off
                    return TernaryBool.Undefined; // don't care
                case NodeTypeT.End:
                    return TernaryBool.Undefined;
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
                case NodeTypeT.End:
                    return TernaryBool.Undefined;
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
                case NodeTypeT.End:
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
                case NodeTypeT.End:
                    return TernaryBool.Undefined;
                default:
                    throw new Exception("Unreachable code");
            }
        }
        #endregion
    }
}
