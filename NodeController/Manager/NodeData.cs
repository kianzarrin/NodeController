using System;
namespace NodeController {
    using ColossalFramework;
    using KianCommons;
    using KianCommons.Math;
    using KianCommons.Plugins;
    using KianCommons.Serialization;
    using NodeController.Tool;
    using NodeController.Util;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using TrafficManager.API.Traffic.Enums;
    using UnityEngine;
    using static ColossalFramework.Math.VectorUtils;
    using static KianCommons.Assertion;
    using static KianCommons.ReflectionHelpers;
    using Log = KianCommons.Log;
    using TernaryBool = CSUtil.Commons.TernaryBool;

    public enum NodeTypeT {
        Nodeless,
        Bend,
        Stretch,
        Crossing, // change dataMatrix.w to render crossings in the middle.
        UTurn, // set offset to 5.
        Custom,
        End,
    }

    [Serializable]
    public class NodeData : ISerializable, INetworkData, INetworkData<NodeData> {
        public override string ToString() => $"NodeData(id:{NodeID} type:{NodeType} sharp:{SharpCorners})";
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

            Update();
        }

        private NodeData(NodeData template) {
            CopyProperties(this, template);
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
        public bool AllStraight; // no segment is curved.
        public float Gap;

        // cache only for segment count == 2
        public float HWDiff;
        public int PedestrianLaneCount;
        public bool IsStraight;
        public bool Is180;
        ushort segmentID1, segmentID2;
        public List<ushort> SortedSegmentIDs; //sorted by how big segment is.
        public SegmentEndData SegmentEnd1 => SegmentEndManager.Instance.GetAt(segmentID1, NodeID);
        public SegmentEndData SegmentEnd2 => SegmentEndManager.Instance.GetAt(segmentID2, NodeID);

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
                if (!segEnd.HasUniformCornerOffset())
                    return false;
                if (cornerOffset0 == -1)
                    cornerOffset0 = segEnd.CornerOffset;
                else if (cornerOffset0 != segEnd.CornerOffset)
                    return false;
            }
            return true;
        }
        #endregion
        #region no markings

        [Obsolete("this is only for backward compatibility")]
        public bool ClearMarkings { set => NoMarkings = value; }

        public bool NoMarkings {
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
        #region flatten node

        static int CompareSegments(ushort seg1Id, ushort seg2Id) {
            ref NetSegment seg1 = ref seg1Id.ToSegment();
            ref NetSegment seg2 = ref seg2Id.ToSegment();
            NetInfo info1 = seg1.Info;
            NetInfo info2 = seg2.Info;

            int slope1 = info1.m_flatJunctions ? 0 : 1;
            int slope2 = info2.m_flatJunctions ? 0 : 1;
            int diff = slope1 - slope2;
            if (diff != 0) return diff;

            diff = info1.m_forwardVehicleLaneCount - info2.m_forwardVehicleLaneCount;
            if (diff != 0) return diff;

            diff = (int)Math.Ceiling(info2.m_halfWidth - info1.m_halfWidth);
            if (diff != 0) return diff;

            bool bHighway1 = (info1.m_netAI as RoadBaseAI)?.m_highwayRules ?? false;
            bool bHighway2 = (info1.m_netAI as RoadBaseAI)?.m_highwayRules ?? false;
            int iHighway1 = bHighway1 ? 1 : 0;
            int iHighway2 = bHighway2 ? 1 : 0;
            diff = iHighway1 - iHighway2;
            return diff;
        }

        public void Flatten() {
            Log.Debug("NodeData.Flatten() called");
            foreach (ushort segmentID in SortedSegmentIDs) {
                var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: NodeID);
                segEnd.FlatJunctions = true;
                segEnd.Twist = false;
            }

            Update();
        }

        public void UnFlatten() {
            Log.Debug("NodeData.UnFlatten() called");
            for (int i = 0; i < SortedSegmentIDs.Count; ++i) {
                ushort segmentID = SortedSegmentIDs[i];
                var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: NodeID);
                bool sideSegment = i >= 2;
                segEnd.FlatJunctions = sideSegment;
                segEnd.Twist = sideSegment;
            }
            Update();
        }

        //public bool IsFlattened {
        //    get {
        //        for(int i = 0; i < SortedSegmentIDs.Count; ++i) {
        //            ushort segmentID = SortedSegmentIDs[i];
        //            var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: NodeID);
        //            //Log.Debug($"get_SlopedJunction i:{i} segEnd.flat:{segEnd.FlatJunctions}");
        //            bool sideSegment = i >= 2;
        //            if (segEnd.FlatJunctions)
        //                return true;
        //        }
        //        return false;
        //    }
        //    set {
        //        for (int i = 0; i < SortedSegmentIDs.Count; ++i) {
        //            ushort segmentID = SortedSegmentIDs[i];
        //            var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: NodeID);
        //            bool sideSegment = i >= 2;
        //            if (!value) {
        //                segEnd.FlatJunctions = sideSegment;
        //                segEnd.Twist = sideSegment;
        //            } else {
        //                segEnd.FlatJunctions = true;
        //                segEnd.Twist = false;
        //            }
        //        }
        //    }
        //}

        //public bool HasUniformSlopedJunction() {
        //    bool sloped0 = default;
        //    for (int i = 0; i < SortedSegmentIDs.Count; ++i) {
        //        ushort segmentID = SortedSegmentIDs[i];
        //        var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: NodeID);
        //        bool sideSegment = i >= 2;
        //        //main road slope, side segment flat and twisted.
        //        bool sloped = segEnd.FlatJunctions == sideSegment && segEnd.Twist == sideSegment;
        //        //Log.Debug($"HasUniformSlopedJunction i:{i} good:{sloped}");
        //        if (i == 0)
        //            sloped0 = sloped;
        //        else if (sloped != sloped0)
        //            return false;
        //    }
        //    return true;
        //}

        //public bool HasUniformFlatJunction() {
        //    bool flat0 = default;
        //    for (int i = 0; i < SortedSegmentIDs.Count; ++i) {
        //        ushort segmentID = SortedSegmentIDs[i];
        //        var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: NodeID);
        //        bool sideSegment = i >= 2;
        //        // main road slope, side segment flat and twisted.
        //        bool flat = segEnd.FlatJunctions == true;
        //        //Log.Debug($"HasUniformFlatJunction i:{i} good:{flat}");
        //        if (i == 0)
        //            flat0 = flat;
        //        else if (flat != flat0)
        //            return false;
        //    }
        //    return true;
        //}

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

        public float EmbankmentPercent {
            get {
                Assert(CanMassEditNodeCorners());
                Assert(SegmentCount == 2);
                float ret = SegmentEnd1.EmbankmentPercent - SegmentEnd2.EmbankmentPercent;
                ret = ret * 0.5f; //average
                return ret;
            }
            set {
                Assert(CanMassEditNodeCorners());
                Assert(SegmentCount == 2);
                SegmentEnd1.EmbankmentPercent = value;
                SegmentEnd2.EmbankmentPercent = -value;
            }
        }

        public bool HasUniformEmbankmentAngle() {
            Assert(CanMassEditNodeCorners());
            Assert(SegmentCount == 2);
            return SegmentEnd1.EmbankmentAngleDeg == -SegmentEnd2.EmbankmentAngleDeg;
        }
        #endregion
        #region slope angle
        public float SlopeAngleDeg {
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
        #endregion Stretch
        #region Shift
        public float Shift1 {
            get {
                if (SegmentEnd1.IsHeadNode) {
                    return +SegmentEnd1.Shift;
                } else {
                    return -SegmentEnd1.Shift;
                }
            }
            set {
                if (SegmentEnd1.IsHeadNode) {
                    SegmentEnd1.Shift = +value;
                } else {
                    SegmentEnd1.Shift = -value;
                }
            }
        }
        public float Shift2 {
            get {
                if (!SegmentEnd1.IsHeadNode) {
                    return +SegmentEnd2.Shift;
                } else {
                    return -SegmentEnd2.Shift;
                }
            }
            set {
                if (!SegmentEnd1.IsHeadNode) {
                    SegmentEnd2.Shift = +value;
                } else {
                    SegmentEnd2.Shift = -value;
                }
            }
        }
        public float Shift {
            get {
                Assert(CanMassEditNodeCorners());
                Assert(SegmentCount == 2);
                return (Shift1 + Shift2) * 0.5f;
            }
            set {
                Assert(CanMassEditNodeCorners());
                Assert(SegmentCount == 2);
                Shift1 = Shift2 = value;
            }
        }

        public bool HasUniformShift() {
            Assert(CanMassEditNodeCorners());
            Assert(SegmentCount == 2);
            return Shift1 == Shift2;
        }
        #endregion Shift
        #region sharp
        public bool SharpCorners {
            get {
                bool ret = false;
                for (int i = 0; i < 8; ++i) {
                    ushort segmentID = Node.GetSegment(i);
                    if (segmentID == 0) continue;
                    var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: NodeID);
                    ret |= segEnd.SharpCorners;
                }
                return ret;
            }
            set {
                for (int i = 0; i < 8; ++i) {
                    ushort segmentID = Node.GetSegment(i);
                    if (segmentID == 0) continue;
                    var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: NodeID);
                    segEnd.SharpCorners = value;
                }
            }
        }
        public bool HasUnifromSharp() {
            bool? val = null;
            for (int i = 0; i < 8; ++i) {
                ushort segmentID = Node.GetSegment(i);
                if (segmentID == 0) continue;
                var segEnd = SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: NodeID);
                if (val == null) {
                    val = segEnd.SharpCorners;
                } else if (val != segEnd.SharpCorners) {
                    return false;
                }
            }
            return true;
        }

        #endregion
        #endregion bulk edit

        public bool FirstTimeTrafficLight; // turn on traffic light when inserting pedestrian node for the first time.

        public ref NetNode Node => ref NodeID.ToNode();

        public NodeData(ushort nodeID) {
            try {
                Assert(IsSupported(nodeID));
                NodeID = nodeID;
                Calculate();
                NodeType = DefaultNodeType;
                if (CanModifySharpCorners()) {
                    SharpCorners = Info.GetARSharpCorners();
                } else {
                    SharpCorners = false;
                }

                FirstTimeTrafficLight = false;
                Assert(IsDefault(), $"{this}.IsDefault(): NodeType:{NodeType} == {DefaultNodeType}\n" +
                    string.Join("\n\n", IterateSegmentEndDatas()
                    .Where(segEnd => !segEnd.IsDefault())
                    .Select(segEnd => $"{segEnd} is not default : " + segEnd.DefaultMessage())
                    .ToArray()));
                Assert(CanChangeTo(NodeType), $"CanChangeTo(NodeType={NodeType})");
                Update();
            } catch (Exception ex) {
                ex.Log();
                throw;
            }
        }

        public NodeData(ushort nodeID, NodeTypeT nodeType) : this(nodeID) {
            try {
                NodeType = nodeType;
                FirstTimeTrafficLight = nodeType == NodeTypeT.Crossing || IsLevelCrossing;
                // TODO update slope angle.
                Assert(CanChangeTo(NodeType), $"CanChangeTo(NodeType={NodeType})");
            } catch (Exception ex) {
                ex.Log();
                throw;
            }
        }

        public void Calculate() {
            try {
                CalculateDefaults();
                Refresh();
            } catch (Exception ex) {
                ex.Log();
                throw;
            }
        }

        /// <summary>
        /// Capture the default values.
        /// </summary>
        private void CalculateDefaults() {
            try {
                SegmentCount = NodeID.ToNode().CountSegments();
                DefaultFlags = NodeID.ToNode().m_flags;

                if (DefaultFlags.IsFlagSet(NetNode.Flags.Middle)) {
                    DefaultNodeType = NodeTypeT.Nodeless;
                } else if (DefaultFlags.IsFlagSet(NetNode.Flags.Bend)) {
                    DefaultNodeType = NodeTypeT.Bend;
                } else if (DefaultFlags.IsFlagSet(NetNode.Flags.Junction)) {
                    DefaultNodeType = NodeTypeT.Custom;
                } else if (DefaultFlags.IsFlagSet(NetNode.Flags.End)) {
                    NodeType = DefaultNodeType = NodeTypeT.End;
                } else {
                    throw new NotImplementedException("unsupported node flags: " + DefaultFlags);
                }

                static bool IsSegmentStraight(ushort segmentId, ushort nodeId) {
                    Vector3 pos1 = nodeId.ToNode().m_position;
                    Vector3 pos2 = segmentId.ToSegment().GetOtherNode(nodeId).ToNode().m_position;
                    Vector3 dir = NormalizeXZ(pos2 - pos1);
                    Vector3 endDir = NormalizeXZ(segmentId.ToSegment().GetDirection(nodeId));
                    return DotXZ(dir, endDir) > 0.95f;
                }

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

                SortedSegmentIDs = new List<ushort>(Node.CountSegments());
                for (int i = 0; i < 8; ++i) {
                    ushort segmentID = Node.GetSegment(i);
                    if (segmentID == 0) continue;
                    SortedSegmentIDs.Add(segmentID);
                }

                SortedSegmentIDs.Sort(CompareSegments);
                SortedSegmentIDs.Reverse();

                Refresh();
            } catch (Exception ex) {
                ex.Log();
                throw;
            }
        }

        public void CalculateGap() {
            try {
                var maxGapSqr = 0f;
                foreach (var firstData in IterateSegmentEndDatas()) {
                    foreach (var secondData in IterateSegmentEndDatas()) {
                        CalculateGapSqr(ref maxGapSqr, firstData, secondData, true, true);
                        CalculateGapSqr(ref maxGapSqr, firstData, secondData, true, false);
                        CalculateGapSqr(ref maxGapSqr, firstData, secondData, false, true);
                        CalculateGapSqr(ref maxGapSqr, firstData, secondData, false, false);
                    }
                }
                Gap = Mathf.Sqrt(maxGapSqr) + 2f;
            } catch (Exception ex) {
                ex.Log();
                throw;
            }
        }
        private void CalculateGapSqr(ref float gap, SegmentEndData segEnd1, SegmentEndData segEnd2, bool left1, bool left2) {
            if (segEnd1 == null || segEnd2 == null)
                return;
            var pos1 = segEnd1.Corner(left1).Pos;
            var pos2 = segEnd2.Corner(left2).Pos;
            var delta = (pos1 - pos2).sqrMagnitude;
            gap = Mathf.Max(gap, delta);
        }

        /// <summary>
        /// this is called to make necessary changes to the node to handle external changes
        /// </summary>
        private void Refresh() {
            if (Log.VERBOSE) Log.Debug($"NodeData.Refresh() node:{NodeID}\n" + Environment.StackTrace);

            if (NodeType != NodeTypeT.Custom)
                NoMarkings = false;

            if (!CanModifyOffset()) {
                if (NodeType == NodeTypeT.UTurn)
                    CornerOffset = 8f;
                else if (NodeType == NodeTypeT.Crossing)
                    CornerOffset = 0f;
            }
            if (!CanModifySharpCorners()) {
                SharpCorners = false;
            }
        }

        public void Update() => NodeManager.UpdateNode(NodeID);

        public void RefreshAndUpdate() {
            Refresh();
            Update();
        }

        public IEnumerable<SegmentEndData> IterateSegmentEndDatas() {
            for (int i = 0; i < 8; ++i) {
                ushort segmentID = Node.GetSegment(i);
                if (segmentID == 0) continue;
                yield return SegmentEndManager.Instance.GetAt(segmentID: segmentID, nodeID: NodeID);
            }
        }

        static ushort SelectedNodeID => NodeControllerTool.Instance.SelectedNodeID;
        public bool IsSelected() => NodeID == SelectedNodeID;

        public bool IsDefault() {
            try {
                bool isDefault = NodeType == DefaultNodeType;
                if (!isDefault)
                    return false;

                foreach (var segEnd in IterateSegmentEndDatas()) {
                    isDefault = segEnd == null || segEnd.IsDefault();
                    if (!isDefault)
                        return false;
                }
                return true;
            } catch (Exception ex) {
                ex.Log();
                throw;
            }
        }

        public void ResetToDefault() {
            try {
                NodeType = DefaultNodeType;
                foreach (var segEnd in IterateSegmentEndDatas())
                    segEnd?.ResetToDefault();
                Update();
            } catch (Exception ex) {
                ex.Log();
                throw;
            }
        }
        public static bool IsSupported(ushort nodeID) {
            try {
                if (!NetUtil.IsNodeValid(nodeID)) // check info !=null (and maybe more checks in future)
                    return false;
                foreach (ushort segmentID in NetUtil.IterateNodeSegments(nodeID)) {
                    if (!NetUtil.IsSegmentValid(segmentID))
                        return false;
                }

                var flags = nodeID.ToNode().m_flags;
                if (!flags.CheckFlags(
                    required: NetNode.Flags.Created,
                    forbidden: NetNode.Flags.Outside | NetNode.Flags.Deleted)) {
                    return false;
                }

                int n = nodeID.ToNode().CountSegments();
                if (n != 2) return true;
                var info = nodeID.ToNode().Info;
                //return info.m_netAI is RoadBaseAI && !NetUtil.IsCSUR(info); // TODO support paths/tracks.
                return !NetUtil.IsCSUR(info);
            } catch (Exception ex) {
                ex.Log();
                throw;
            }
        }

        public bool CanChangeTo(NodeTypeT newNodeType) {
            try {
                //Log.Debug($"CanChangeTo({newNodeType}) was called.");
                if (SegmentCount == 1)
                    return newNodeType is NodeTypeT.End;

                if (SegmentCount > 2 || IsCSUR || IsLevelCrossing)
                    return newNodeType is NodeTypeT.Custom or NodeTypeT.Nodeless;

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
                        return true; // !middle; clus wants to use bend nodes.
                    case NodeTypeT.Nodeless:
                        return true; // all junctions can be node-less
                    case NodeTypeT.Custom:
                        return true;
                    case NodeTypeT.End:
                        return false;
                    default:
                        throw new Exception("Unreachable code");
                }
            } catch (Exception ex) {
                ex.Log();
                throw;
            }
        }

        public bool IsLevelCrossing => Node.m_flags.IsFlagSet(NetNode.Flags.LevelCrossing);
        public bool IsCSUR => NetUtil.IsCSUR(Info);
        public NetInfo Info => Node.Info;
        public bool IsRoad => Info.m_netAI is RoadBaseAI;
        public bool EndNode() => NodeType == NodeTypeT.End;
        public bool NeedMiddleFlag() => NodeType == NodeTypeT.Nodeless && (IsStraight || Is180);
        public bool IsNodelessJunction() =>
            !NeedMiddleFlag() &&
            (NodeType == NodeTypeT.Nodeless ||
            IterateSegmentEndDatas().All(item => item != null && item.IsNodeless));
        public bool NeedBendFlag() => NodeType == NodeTypeT.Bend;
        public bool NeedJunctionFlag() => !NeedMiddleFlag() && !NeedBendFlag() && !EndNode();
        public bool WantsTrafficLight() => NodeType == NodeTypeT.Crossing || IsLevelCrossing;
        public bool CanModifyOffset() =>
            (NodeType is NodeTypeT.Bend or NodeTypeT.Stretch or NodeTypeT.Custom);
        public bool CanModifySharpCorners() {
            return NodeType is NodeTypeT.Bend or NodeTypeT.Custom;
        }
        public bool CanMassEditNodeCorners() => SegmentCount == 2;
        public bool CanModifyFlatJunctions() => !NeedMiddleFlag(); // && !IsNodelessJunction() ?
        public bool IsAsymRevert() => DefaultFlags.IsFlagSet(NetNode.Flags.AsymBackward | NetNode.Flags.AsymForward);
        public bool CanModifyTextures() => IsRoad && !IsCSUR;
        public bool ShowNoMarkingsToggle() => CanModifyTextures() && NodeType == NodeTypeT.Custom;

        private bool CrossingIsRemoved(ushort segmentId) => HTCUtil.ShouldHideCrossing(nodeID: NodeID, segmentID: segmentId);
            
        public bool NeedsTransitionFlag() =>
            SegmentCount == 2 &&
            (NodeType == NodeTypeT.Custom ||
            NodeType == NodeTypeT.Crossing ||
            NodeType == NodeTypeT.UTurn);

        public bool ShouldRenderCenteralCrossingTexture() =>
            NodeType == NodeTypeT.Crossing &&
            CrossingIsRemoved(segmentID1) &&
            CrossingIsRemoved(segmentID2);

        public string ToolTip(NodeTypeT nodeType) {
            switch (nodeType) {
                case NodeTypeT.Crossing:
                    return "Crossing node.";
                case NodeTypeT.Nodeless:
                    return "Nodeless: No node.";
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
                    return "Custom: transition size and traffic rules are configurable.";
                case NodeTypeT.End:
                    return "when there is only one segment at the node.";
            }
            return null;
        }

        public Vector3 GetPosition() => Node.m_position + Vector3.up * (Node.m_heightOffset / 64f);

        // same code as AN
        private void ShiftPillar() {
            try {
                ref NetNode node = ref Node;
                NetInfo info = node.Info;
                ushort buildingId = node.m_building;
                ref var building = ref buildingId.ToBuilding();
                bool isValid = node.IsValid() && building.IsValid(buildingId);
                if (!isValid)
                    return;

                info.m_netAI.GetNodeBuilding(NodeID, ref node, out BuildingInfo buildingInfo, out float heightOffset);
                Vector3 center = default;
                int counter = 0;
                foreach (var segmentEndData in IterateSegmentEndDatas()) {
                    center += segmentEndData.LeftCorner.Pos + segmentEndData.RightCorner.Pos;
                    counter += 2;
                }
                center /= counter;
                center.y += heightOffset;

                BuildingUtil.RelocatePillar(buildingId, center, building.m_angle);
                building.m_position = center;
            } catch (Exception ex) { ex.Log(); }
        }

        public static void FixPillar(ushort nodeID) {
            //Log.Called(nodeID);
            ref NetNode node = ref nodeID.ToNode();
            if (NodeManager.Instance.buffer[nodeID] is NodeData nodeData) {
                nodeData.ShiftPillar();
                return;
            }

            ushort buildingId = node.m_building;
            ref var building = ref buildingId.ToBuilding();
            bool isValid = node.IsValid() && building.IsValid(buildingId);
            bool middle = node.IsMiddle();
            bool untouchable = node.m_flags.IsFlagSet(NetNode.Flags.Untouchable);
            if (isValid && !middle && !untouchable && HasSlope(nodeID)) {
                Vector3 center = GetCenter(nodeID);
                node.Info.m_netAI.GetNodeBuilding(nodeID, ref node, out BuildingInfo buildingInfo, out float heightOffset);
                center.y += heightOffset;
                BuildingUtil.RelocatePillar(buildingId, center, building.m_angle);
            }

            static Vector3 GetCenter(ushort nodeID) {
                Vector3 center = default;
                int count = 0;
                ref NetNode node = ref nodeID.ToNode();
                for(ushort segmentIndex = 0; segmentIndex < 8; ++segmentIndex) {
                    ushort segmentID = node.GetSegment(segmentIndex);
                    if (segmentID == 0) continue;
                    ref NetSegment segment = ref segmentID.ToSegment();
                    bool startNode = segment.IsStartNode(nodeID);
                    foreach (bool left in HelpersExtensions.ALL_BOOL) {
                        segment.CalculateCorner(
                            segmentID, heightOffset: true, start: startNode, leftSide: left,
                            out var corner, out _, out _);
                        center += corner;
                        count++;
                    }
                }
                return center / count;
            }

            static bool HasSlope(ushort nodeID) {
                ref NetNode node = ref nodeID.ToNode();
                for (ushort segmentIndex = 0; segmentIndex < 8; ++segmentIndex) {
                    ushort segmentID = node.GetSegment(segmentIndex);
                    if (segmentID == 0) continue;
                    ref NetSegment segment = ref segmentID.ToSegment();
                    if (segment.Info == null) return false;
                    if (!segment.Info.m_flatJunctions)
                        return true;
                }
                return false;
            }
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
                case NodeTypeT.Nodeless:
                    return TernaryBool.False; // always off
                case NodeTypeT.Bend:
                    return TernaryBool.False; // always default
                case NodeTypeT.Custom:
                    return TernaryBool.Undefined; // default
                case NodeTypeT.End:
                    return TernaryBool.Undefined;
                default:
                    throw new Exception($"Unreachable code. NodeType={NodeType}");
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
                case NodeTypeT.Nodeless:
                    return TernaryBool.False; // always off
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
                case NodeTypeT.Nodeless:
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
                case NodeTypeT.Nodeless:
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
                case NodeTypeT.Nodeless:
                    if (IsNodelessJunction()) {
                        return TernaryBool.Undefined;
                    } else {
                        reason = ToggleTrafficLightError.NoJunction;
                        return TernaryBool.False;
                    }
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
                case NodeTypeT.Nodeless:
                case NodeTypeT.Bend:
                    return TernaryBool.False; // always default
                case NodeTypeT.Custom:
                    if (SegmentCount > 2)
                        return TernaryBool.Undefined;
                    bool oneway = DefaultFlags.IsFlagSet(NetNode.Flags.OneWayIn) & DefaultFlags.IsFlagSet(NetNode.Flags.OneWayOut);
                    if (oneway & !HasPedestrianLanes) {
                        return TernaryBool.False; // always on. // TODO move to TMPE
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
                case NodeTypeT.Nodeless:
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

namespace NodeController.Overrides {
    using static Result;
    internal struct Rule {
        internal Result Crrossing, UTurn, EnterBLocked;
    }

    internal struct Result {
        internal enum ConfigurableType { Allways, Default, NA };
        internal enum DefaultType { On, Off, NA };
        internal bool? Configurable, Default;
        internal Result(ConfigurableType configurable, DefaultType def) {
            Configurable = configurable switch {
                ConfigurableType.Allways => false,
                ConfigurableType.Default => true,
                ConfigurableType.NA => null,
                _ => throw new Exception($"Unreachable code. configurable={configurable}"),
            };
            Default = def switch {
                DefaultType.On => false,
                DefaultType.Off => true,
                DefaultType.NA => null,
                _ => throw new Exception($"Unreachable code. def={def}"),
            };
        }

        internal static Result AllwaysOn = new Result(ConfigurableType.Allways, DefaultType.On);
        internal static Result AllwaysOff = new Result(ConfigurableType.Allways, DefaultType.Off);
        internal static Result DefaultOn = new Result(ConfigurableType.Default, DefaultType.On);
        internal static Result DefaultOff = new Result(ConfigurableType.Default, DefaultType.Off);
        internal static Result NA = new Result(ConfigurableType.NA, DefaultType.NA);
    }

    internal static class Overrides {
        static int NodeTypeTCOUNT => Enum.GetValues(typeof(NodeTypeT)).Length;

        // Rules [Rule][NodeType][RuleType]
        internal static Rule[] Rules = new Rule[NodeTypeTCOUNT];
        static Overrides() {
#pragma warning disable format
            Rules[(int)NodeTypeT.Crossing] = new Rule { Crrossing = AllwaysOn , UTurn = AllwaysOff, EnterBLocked = DefaultOff};
            Rules[(int)NodeTypeT.UTurn]    = new Rule { Crrossing = AllwaysOff, UTurn = AllwaysOn , EnterBLocked = NA        };
            Rules[(int)NodeTypeT.Stretch]  = new Rule { Crrossing = AllwaysOff, UTurn = AllwaysOff, EnterBLocked = AllwaysOn };
            Rules[(int)NodeTypeT.Nodeless] = new Rule { Crrossing = AllwaysOff, UTurn = AllwaysOff, EnterBLocked = AllwaysOn };
            Rules[(int)NodeTypeT.Bend]     = new Rule { Crrossing = NA        , UTurn = NA        , EnterBLocked = NA        };
            Rules[(int)NodeTypeT.Custom]   = new Rule { Crrossing =NA/*moved*/, UTurn = NA        , EnterBLocked = NA/*not moved*/};
            Rules[(int)NodeTypeT.End]      = new Rule { Crrossing = NA        , UTurn = NA        , EnterBLocked = NA        };
#pragma warning restore format
        }
    }
}