namespace NodeController {
    using ColossalFramework;
    using ColossalFramework.Math;
    using ColossalFramework.UI;
    using CSUtil.Commons;
    using Epic.OnlineServices.Presence;
    using KianCommons;
    using KianCommons.Math;
    using KianCommons.Plugins;
    using KianCommons.Serialization;
    using NodeController.GUI;
    using NodeController.Tool;
    using System;
    using System.Runtime.Serialization;
    using UnityEngine;
    using static KianCommons.Assertion;
    using static KianCommons.ReflectionHelpers;
    using CSURUtil = Util.CSURUtil;
    using Log = KianCommons.Log;
    using Vector3Serializable = KianCommons.Math.Vector3Serializable;

    [Serializable]
    public class SegmentEndData : INetworkData, INetworkData<SegmentEndData>, ISerializable {
        #region serialization
        //serialization
        public void GetObjectData(SerializationInfo info, StreamingContext context) =>
            SerializationUtil.GetObjectFields(info, this);

        /// <summary>clone</summary>
        private SegmentEndData(SegmentEndData template) =>
            CopyProperties(this, template);

        public SegmentEndData Clone() => new SegmentEndData(this);
        #endregion

        // deserialization
        public SegmentEndData() { } // so that the code compiles

        public SegmentEndData(SerializationInfo info, StreamingContext context) {
            SerializationUtil.SetObjectFields(info, this);

            // corner offset and slope angle deg
            SerializationUtil.SetObjectProperties(info, this);

            if (SerializationUtil.DeserializationVersion < new Version(2, 1, 1)) {
                LeftCorner.Left = true;
                RightCorner.Left = false;

#pragma warning disable CS0618 // Type or member is obsolete
                LeftCorner.DeltaPos = info.GetValue<Vector3Serializable>("DeltaLeftCornerPos");
                LeftCorner.DeltaDir = info.GetValue<Vector3Serializable>("DeltaLeftCornerDir");
                RightCorner.DeltaPos = info.GetValue<Vector3Serializable>("DeltaRightCornerPos");
                RightCorner.DeltaDir = info.GetValue<Vector3Serializable>("DeltaRightCornerDir");
#pragma warning restore CS0618 // Type or member is obsolete
            }

            Update();
        }

        // intrinsic
        public ushort NodeID;
        public ushort SegmentID;
        public bool IsStartNode => NetUtil.IsStartNode(segmentId: SegmentID, nodeId: NodeID);
        public bool IsHeadNode => NetUtil.GetHeadNode(SegmentID) == NodeID;
        public bool IsTailNode => !IsHeadNode;

        public override string ToString() {
            return GetType().Name + $"(segment:{SegmentID} node:{NodeID})";
        }

        // defaults
        public float DefaultCornerOffset => CSURUtil.GetMinCornerOffset(SegmentID, NodeID);
        public bool DefaultFlatJunctions => Info.m_flatJunctions
            || Node.m_flags.IsFlagSet(NetNode.Flags.Untouchable);
        public bool DefaultTwist => DefaultFlatJunctions
            && !Node.m_flags.IsFlagSet(NetNode.Flags.Untouchable);

        public NetSegment.Flags DefaultFlags;

        public bool DefaultSharpCorners {
            get {
                NetNode.Flags flags = NodeData?.DefaultFlags ?? default;
                if (flags == default) {
                    flags = NodeID.ToNode().m_flags;
                }
                if(flags.IsFlagSet(NetNode.Flags.Middle | NetNode.Flags.End)) {
                    return false;
                } else {
                    return AdaptiveRoadsUtil.GetARSharpCorners(Info);
                }
            }
        }

        // cache
        public bool HasPedestrianLanes;
        public float CurveRaduis0;
        public int PedestrianLaneCount;
        public float CachedSuperElevationDeg; // rightward rotation of the road when going away from the junction.

        // Configurable
        public bool NoCrossings;
        public bool NoMarkings;
        public bool NoJunctionTexture;
        public bool NoJunctionProps; // excluding TL
        public bool NoTLProps;
        public bool FlatJunctions;
        public bool Twist;
        /// <summary>delta width stretch in percent</summary>
        public float Stretch;
        public float EmbankmentAngleDeg;
        public float Shift;
        public float DeltaSlopeAngleDeg;
        public bool Nodeless;
        public bool SharpCorners;

        // shortcuts
        public ref NetSegment Segment => ref SegmentID.ToSegment();
        public ref NetNode Node => ref NodeID.ToNode();
        public NodeData NodeData => NodeManager.Instance.buffer[NodeID];
        public ref NodeTypeT NodeType => ref NodeData.NodeType;

        /// <summary>other segment end of the same segment.</summary>
        public SegmentEndData OtherEnd => SegmentEndManager.Instance.GetAt(SegmentID, !IsStartNode);
        public SegmentEndData OppositeMiddleEnd {
            get {
                if (NodeData != null && NodeType == NodeTypeT.Nodeless) {
                    ushort segmentId2 = Node.GetAnotherSegment(SegmentID);
                    if (segmentId2 != 0) {
                        return SegmentEndManager.Instance.GetAt(segmentId2, NodeID);
                    }
                }
                return null;
            }
        }


        /// <summary>segment end direction</summary>
        public ref Vector3 Direction {
            get {
                if (IsStartNode)
                    return ref Segment.m_startDirection;
                else
                    return ref Segment.m_endDirection;
            }
        }

        public SegmentEndData(ushort segmentID, ushort nodeID) {
            NodeID = nodeID;
            SegmentID = segmentID;

            Calculate();
            CornerOffset = DefaultCornerOffset;
            FlatJunctions = DefaultFlatJunctions;
            SharpCorners = DefaultSharpCorners;
            Twist = DefaultTwist;
            if (Log.VERBOSE)
                Log.Debug($"SegmentEndData() Direction={Direction} Slope={SlopeAngleDeg}");
            Assert(IsDefault(), "\n" + DefaultMessage());

            Update();
        }

        public string DefaultMessage() => $"{CornerOffset} == {DefaultCornerOffset} error = 0.1\n" +
            $"DeltaSlopeAngleDeg:{DeltaSlopeAngleDeg} == 0;" +
            $"Stretch:{Stretch} == 0; " +
            $"Shift:{Shift} == 0; " +
            $"SharpCorners:{SharpCorners} == {DefaultSharpCorners} " +
            $"EmbankmentAngleDeg:{EmbankmentAngleDeg} == 0; \n" +
            $"LeftCorner.IsDefault():{LeftCorner.IsDefault()} " +
            $"RightCorner.IsDefault():{RightCorner.IsDefault()} \n" +
            $"FlatJunctions:{FlatJunctions} == {DefaultFlatJunctions} " +
            $"Twist:{Twist} == {DefaultTwist} \n" +
            $"NoCrossings:{NoCrossings} == false; " +
            $"NoMarkings:{NoMarkings} == false; " +
            $"NoJunctionTexture:{NoJunctionTexture} == false; " +
            $"NoJunctionProps:{NoJunctionProps} == false; " +
            $"NoTLProps:{NoTLProps} == false; " +
            $"NoTLProps:{Nodeless} == false ";

        public void Calculate() {
            //Capture the default values.
            DefaultFlags = Segment.m_flags;
            PedestrianLaneCount = Info.CountPedestrianLanes();

            Refresh();
        }

        /// <summary>
        /// this is called to make necessary changes to the node to handle external changes
        /// </summary>
        private void Refresh() {
            if (Log.VERBOSE)
                Log.Debug("SegmentEndData.Refresh() for this\n" + Environment.StackTrace);

            if (!CanModifyOffset()) {
                //Log.Debug("SegmentEndData.Refresh(): setting CornerOffset = DefaultCornerOffset");
                CornerOffset = DefaultCornerOffset;
            }
            if (!CanModifyFlatJunctions()) {
                FlatJunctions = DefaultFlatJunctions;
            }
            if (!CanModifyTwist()) {
                Twist = DefaultTwist;
            }
            if (!CanModifyCorners()) {
                DeltaSlopeAngleDeg = 0;
                Shift = Stretch = EmbankmentAngleDeg = 0;
            }
            if (NodeData != null && !NodeData.CanModifySharpCorners()) {
                SharpCorners = false;
            }
            if (!FlatJunctions)
                Twist = false;
        }

        public void Update() {
            // update nearby nodes too to calculate velocity
            NetManager.instance.UpdateNode(NodeID, 0, -1);
        }

        public void RefreshAndUpdate() {
            Refresh();
            Update();
        }

        bool insideAfterCalcualte_ = false;

        /// <summary>
        /// called after all calculations are done. this is called in order to cache values.
        /// </summary>
        public void OnAfterCalculate() {
            //Log.Debug("SegmentEndData.OnAfterCalculate() called for " + this);
            insideAfterCalcualte_ = true;
            // left and right is when you go away form junction
            // both in SegmentEndData Cached* and NetSegment.CalculateCorner()

            Segment.CalculateCorner(SegmentID, true, IsStartNode, leftSide: true,
                cornerPos: out var lpos, cornerDirection: out var ldir, out _);
            Segment.CalculateCorner(SegmentID, true, IsStartNode, leftSide: false,
                cornerPos: out var rpos, cornerDirection: out var rdir, out _);

            // useful for putting segment names on the segment when slope has changed by user.
            // Direction.y = (ldir.y + rdir.y) * 0.5f; // messes up with middle nodes

            LeftCorner.CachedPos = lpos;
            RightCorner.CachedPos = rpos;
            LeftCorner.CachedDir = ldir;
            RightCorner.CachedDir = rdir;

            Vector3 diff = rpos - lpos;
            float se = Mathf.Atan2(diff.y, VectorUtils.LengthXZ(diff));
            CachedSuperElevationDeg = se * Mathf.Rad2Deg;

            SimulationManager.instance.m_ThreadingWrapper.QueueMainThread(delegate () {
                var activePanel = UIPanelBase.ActivePanel;
                if (activePanel != null) {
                    if (activePanel.NetworkType == NetworkTypeT.Node && NodeID == SelectedNodeID) {
                        activePanel.RefreshValues();
                    } else if (activePanel.NetworkType == NetworkTypeT.SegmentEnd && IsSelected()) {
                        activePanel.RefreshValues();
                    }
                }
            });

            NodeData?.CalculateGap();

            insideAfterCalcualte_ = false;
        }

        static ushort SelectedSegmentID => NodeControllerTool.Instance.SelectedSegmentID;
        static ushort SelectedNodeID => NodeControllerTool.Instance.SelectedNodeID;
        public bool IsSelected() => NodeID == SelectedNodeID && SegmentID == SelectedSegmentID;

        public bool IsDefault() {
            bool ret = Mathf.Abs(CornerOffset - DefaultCornerOffset) < 0.1f;
            ret &= DeltaSlopeAngleDeg == 0;
            ret &= Stretch == 0;
            ret &= Shift == 0;
            ret &= SharpCorners == DefaultSharpCorners;

            ret &= EmbankmentAngleDeg == 0;
            ret &= FlatJunctions == DefaultFlatJunctions;
            ret &= Twist == DefaultTwist;
            ret &= LeftCorner.IsDefault();
            ret &= RightCorner.IsDefault();

            ret &= NoCrossings == false;
            ret &= NoMarkings == false;
            ret &= NoJunctionTexture == false;
            ret &= NoJunctionProps == false;
            ret &= NoTLProps == false;
            ret &= Nodeless == false;
            return ret;
        }

        public void ResetToDefault() {
            CornerOffset = DefaultCornerOffset;
            DeltaSlopeAngleDeg = 0;
            FlatJunctions = DefaultFlatJunctions;
            Twist = DefaultTwist;
            Nodeless = false;
            NoCrossings = false;
            NoMarkings = false;
            NoJunctionTexture = false;
            NoJunctionProps = false;
            NoTLProps = false;
            SharpCorners = DefaultSharpCorners;
            Shift = Stretch = EmbankmentAngleDeg = 0;
            LeftCorner.ResetToDefault();
            RightCorner.ResetToDefault();
            RefreshAndUpdate();
        }


        #region GUI Data Conversions
        public float CornerOffset {
            get => (LeftCorner.Offset + RightCorner.Offset) * 0.5f;
            set => LeftCorner.Offset = RightCorner.Offset = value;
        }

        public bool HasUniformCornerOffset() => LeftCorner.Offset == RightCorner.Offset;

        public float EmbankmentPercent {
            get => Mathf.Tan(EmbankmentAngleDeg * Mathf.Deg2Rad) * 100;
            set => EmbankmentAngleDeg = Mathf.Atan(value * 0.01f) * Mathf.Rad2Deg;
        }

        // we know Dir00.LenXZ == 1
        float AverageDirY00 => (LeftCorner.Dir00.y + RightCorner.Dir00.y) * 0.5f;

        public float SlopeAngleDeg {
            get => DeltaSlopeAngleDeg + AngleDeg(AverageDirY00);
            set => DeltaSlopeAngleDeg = value - AngleDeg(AverageDirY00);
        }
        #endregion

        public ref CornerData Corner(bool left) {
            if (left)
                return ref LeftCorner;
            else
                return ref RightCorner;
        }

        // left and right going away from the junction.
        public CornerData LeftCorner = new() { Left = true };
        public CornerData RightCorner = new() { Left = false };

        [Serializable]
        public struct CornerData {
            public bool Left;

            public bool IsDefault() {
                bool ret = DeltaPos == Vector3.zero && DeltaDir == Vector3.zero;
                //ret &= Offset == ?; // cannot test unless I have link to segment end.
                ret &= LockLength == false;
                return ret;
            }

            public void ResetToDefault() {
                DeltaPos = DeltaDir = Vector3.zero;
                LockLength = false;
            }

#pragma warning disable CS0618 // Type or member is obsolete
            public Vector3Serializable CachedPos, CachedDir;
            public Vector3Serializable Dir00, Pos00; // before sliders
            public Vector3Serializable Dir0, Pos0; // after sliders but before 3x4 table
            public Vector3Serializable DeltaPos, DeltaDir;
#pragma warning restore CS0618 // Type or member is obsolete

            public void ResetDeltaDirI(int index) {
                Vector3 v = DeltaDir;
                v[index] = 0;
                DeltaDir = v;
            }

            public float Offset;
            public bool LockLength;

            public void SetDirI(float val, int index) => Dir = Dir.SetI(val, index);

            public Vector3 Dir {
                get {
                    CalculateTransformVectors(Dir0, Left, out var outward, out var forward);
                    return ReverseTransformCoordinats(CachedDir, outward, Vector3.up, forward);
                }
                set {
                    if (LockLength)
                        value *= DirLength / value.magnitude;
                    CalculateTransformVectors(Dir0, Left, out var outward, out var forward);
                    DeltaDir = value - ReverseTransformCoordinats(Dir0, outward, Vector3.up, forward);
                    CachedDir = Dir0 + TransformCoordinates(DeltaDir, outward, Vector3.up, forward);
                    //Update();
                }
            }

            public Vector3 GetTransformedDir0() {
                CalculateTransformVectors(Dir0, Left, out var outward, out var forward);
                return ReverseTransformCoordinats(Dir0, outward, Vector3.up, forward);
            }

            public float DirLength {
                get => ((Vector3)CachedDir).magnitude;
                set {
                    bool prevLockLength = LockLength;
                    LockLength = false;
                    Dir *= Mathf.Clamp(value, 0.001f, 1000) / DirLength;
                    LockLength = prevLockLength;
                    //Update();
                }
            }

            public Vector3 Pos {
                get => CachedPos;
                set {
                    CalculateTransformVectors(Dir0, left: Left, outward: out var outwardDir, forward: out var forwardDir);
                    CachedPos = value;
                    DeltaPos = ReverseTransformCoordinats(value - Pos0, outwardDir, Vector3.up, forwardDir);
                    //Update();
                }
            }

            /// <summary>
            /// all directions going away from the junction
            /// </summary>
            public static void CalculateTransformVectors(Vector3 dir, bool left, out Vector3 outward, out Vector3 forward) {
                Vector3 rightward = Vector3.Cross(Vector3.up, dir).normalized; // going away from the junction
                Vector3 leftward = -rightward;
                forward = new Vector3(dir.x, 0, dir.z).normalized; // going away from the junction
                outward = left ? leftward : rightward;
            }

            /// <summary>
            /// transforms input vector from relative (to x y z inputs) coordinate to absolute coordinate.
            /// </summary>
            public static Vector3 TransformCoordinates(Vector3 v, Vector3 x, Vector3 y, Vector3 z)
                => v.x * x + v.y * y + v.z * z;

            /// <summary>
            /// reverse transformed coordinates.
            /// </summary>
            public static Vector3 ReverseTransformCoordinats(Vector3 v, Vector3 x, Vector3 y, Vector3 z) {
                Vector3 ret = default;
                ret.x = Vector3.Dot(v, x);
                ret.y = Vector3.Dot(v, y);
                ret.z = Vector3.Dot(v, z);
                return ret;
            }
        }

        bool CrossingIsRemoved() =>
            HideCrosswalks.Patches.CalculateMaterialCommons.ShouldHideCrossing(NodeID, SegmentID);

        #region show/hide in UI
        public bool IsCSUR => NetUtil.IsCSUR(Info);
        public bool IsRoad => Info.m_netAI is RoadBaseAI;
        public NetInfo Info => Segment.Info;
        public bool CanModifyOffset() => (!IsNodeless) && (NodeData?.CanModifyOffset() ?? false);
        public bool CanModifyCorners() => NodeData != null &&
            (CanModifyOffset() || NodeType == NodeTypeT.End || IsNodeless);
        public bool CanModifyNodeless() => NodeData?.NodeType == NodeTypeT.Custom;
        public bool IsNodeless => (NodeData?.NodeType == NodeTypeT.Nodeless) || (CanModifyNodeless() && Nodeless);

        public bool CanModifyFlatJunctions() => NodeData?.CanModifyFlatJunctions() ?? false;
        public bool CanModifyTwist() => CanTwist(SegmentID, NodeID);
        public static bool CanTwist(ushort segmentID, ushort nodeID) {
            // can be twisted but not by default
            //if (nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Untouchable))
            //    return false;
            //if (nodeID.ToNode().Info.m_netAI is PedestrianPathAI)
            //    return false;

            int segmentCount = nodeID.ToNode().CountSegments();

            if (segmentCount == 1) return false;

            // get neighboring segment data
            ushort segmentID1 = segmentID.ToSegment().GetLeftSegment(nodeID);
            ushort segmentID2 = segmentID.ToSegment().GetRightSegment(nodeID);
            var segEnd1 = SegmentEndManager.Instance.GetAt(segmentID1, nodeID);
            var segEnd2 = SegmentEndManager.Instance.GetAt(segmentID2, nodeID);

            bool flat1 = segEnd1?.FlatJunctions ?? segmentID1.ToSegment().Info.m_flatJunctions;
            bool flat2 = segEnd2?.FlatJunctions ?? segmentID2.ToSegment().Info.m_flatJunctions;
            bool slope1 = !flat1;
            bool slope2 = !flat2;
            bool slope = slope1 || slope2;
            if (!slope) return false;

            if (segmentCount == 2) {
                var dir1 = segmentID1.ToSegment().GetDirection(nodeID);
                var dir = segmentID.ToSegment().GetDirection(nodeID);
                float dot = VectorUtils.DotXZ(dir, dir1);
                bool straight = Mathf.Abs(dot) > 0.999f;
                if (straight) return false;
            }

            return true;
        }

        public bool ShowNoMarkingsToggle() {
            if (IsCSUR) return false;
            if (NodeData == null) return true;
            return NodeData.NodeType == NodeTypeT.Custom && IsRoad;
        }
        #endregion

        public static float AngleDeg(float y) => Mathf.Atan(y) * Mathf.Rad2Deg;

        public float EmbankmentDifferenceDeg => EmbankmentAngleDeg + (OtherEnd?.EmbankmentAngleDeg ?? 0); // + because other segment end is facing opposite direction.
        public float StretchDifference => Stretch - (OtherEnd?.Stretch ?? 0);


        public float GetEmbankmentVelocityDeg() {
            if (OppositeMiddleEnd is SegmentEndData end2) {
                return MinByAbs(this.EmbankmentDifferenceDeg, -end2.EmbankmentDifferenceDeg);
            } else {
                return 0;
            }
        }

        public float GetStretchVelocity() {
            if (OppositeMiddleEnd is SegmentEndData end2) {
                return MinByAbs(this.StretchDifference, -end2.StretchDifference);
            } else {
                return 0;
            }
        }

        static float MinByAbs(float a, float b) {
            if (a == 0 || b == 0) return 0; // optimisation.
            int comp = Math.Abs(a).CompareTo(Math.Abs(b));
            if (comp == 0) {
                return Math.Max(a, b); //return the positive one.
            } else if (comp < 0) {
                return a;
            } else {
                return b;
            }
        }

        /// Precondition: cornerDir.LenXZ = 1
        /// <param name="leftSide">left side going away from the junction</param>
        public void ApplyCornerAdjustments(ref Vector3 cornerPos, ref Vector3 cornerDir, bool leftSide) {
            if (VectorUtils.LengthSqrXZ(cornerDir) > 1.001f) {
                // assumption for slope adjustments.
                Log.Error("Warning: ApplyCornerAdjustments() expects cornerDir.LenXZ == 1 got:" +
                    VectorUtils.LengthXZ(cornerDir));
            }

            //Log.Debug($"ApplyCornerAdjustments: cornerDir={cornerDir.ToString("e")}");

            ref CornerData corner = ref Corner(leftSide);
            if (insideAfterCalcualte_) {
                // take a snapshot of pos00/dir00 before sliders but after flatten node checkbox.
                corner.Dir00 = cornerDir;
                corner.Pos00 = cornerPos;
            }

            CornerData.CalculateTransformVectors(
                dir: cornerDir,
                left: leftSide,
                outward: out var outwardDir,
                forward: out var forwardDir);


            {
                Vector3 deltaPos = Vector3.zero;
                {
                    // embankment:
                    float embankmentAngleRad = EmbankmentAngleDeg * Mathf.Deg2Rad;
                    if (leftSide) embankmentAngleRad = -embankmentAngleRad;
                    float sinEmbankmentAngle = Mathf.Sin(embankmentAngleRad);
                    float cosEmbankmentAngle = Mathf.Cos(embankmentAngleRad);
                    float hw = Info.m_halfWidth;
                    deltaPos.x += -hw * (1 - cosEmbankmentAngle); // outward
                    deltaPos.y = hw * sinEmbankmentAngle; // vertical

                    // Stretch:
                    float stretch = Stretch * 0.01f;
                    deltaPos.x += hw * stretch * cosEmbankmentAngle; // outward
                    deltaPos.y += hw * stretch * sinEmbankmentAngle; // vertical
                }

                // Shift:
                if (leftSide) {
                    deltaPos.x -= Shift; // outward
                } else {
                    deltaPos.x += Shift; // outward
                }

                cornerPos += CornerData.TransformCoordinates(deltaPos, outwardDir, Vector3.up, forwardDir);
            }


            {
                //slope:
                float slopeAngleDeg = DeltaSlopeAngleDeg + AngleDeg(corner.Dir00.y);
                float slopeAngleRad = slopeAngleDeg * Mathf.Deg2Rad;

                if (89 <= slopeAngleDeg && slopeAngleDeg <= 91) {
                    cornerDir.x = cornerDir.z = 0;
                    cornerDir.y = 1;
                } else if (-89 >= slopeAngleDeg && slopeAngleDeg >= -91) {
                    cornerDir.x = cornerDir.z = 0;
                    cornerDir.y = -1;
                } else if (slopeAngleDeg > 90 || slopeAngleDeg < -90) {
                    cornerDir.y = -Mathf.Tan(slopeAngleRad);
                    cornerDir.x = -cornerDir.x;
                    cornerDir.z = -cornerDir.z;
                } else {
                    cornerDir.y = Mathf.Tan(slopeAngleRad);
                }
                if (!Node.m_flags.IsFlagSet(NetNode.Flags.Middle)) {
                    float d = VectorUtils.DotXZ(cornerPos - Node.m_position, cornerDir);
                    cornerPos.y += d * (cornerDir.y - corner.Dir00.y);
                }

                if (NCSettings.GameConfig.UnviversalSlopeFixes) {
                    // this must be done after readjusting cornerPos.y
                    // make sure direction vector is not too big.
                    float absY = Mathf.Abs(cornerDir.y);
                    if (absY > 2) {
                        // fix dir length so that y is 2:
                        cornerDir *= 2 / absY;
                    }
                }
            }

            if(Segment.m_averageLength > 0.1f) { // avoid divide by zero.
                // velocity
                Vector3 deltaDir = default;
                {
                    // embankment:
                    float embankmentVelocityRad = GetEmbankmentVelocityDeg().LogRet($"GetEmbankmentVelocityDeg({SegmentID}, {IsStartNode})") * Mathf.Deg2Rad;
                    if (leftSide) embankmentVelocityRad = -embankmentVelocityRad;
                    float sin = Mathf.Sin(embankmentVelocityRad);
                    float cos = Mathf.Cos(embankmentVelocityRad);
                    float r = Info.m_halfWidth / Segment.m_averageLength; // ratio
                    deltaDir.x = -(1 - cos) * r; // outward
                    deltaDir.y = -sin * r; // vertical

                    // Stretch:
                    float stretchVelocity = GetStretchVelocity().LogRet($"GetStretchVelocity({SegmentID}, {IsStartNode})%") * 0.01f;
                    deltaDir.x += -stretchVelocity * cos * r; // outward
                    deltaDir.y += -stretchVelocity * sin * r; // vertical
                }

                Assertion.NotNaNOrInf(deltaDir, "deltaDir");
                float lenxz0 = VectorUtils.LengthSqrXZ(cornerDir);
                cornerDir += CornerData.TransformCoordinates(deltaDir, outwardDir, Vector3.up, forwardDir);
                if (MathUtil.EqualAprox(lenxz0, 1, error: 0.001f)) {
                    // if we damaged normalization then fix it.
                    cornerDir = VectorUtils.NormalizeXZ(cornerDir);
                }
            }


            if (insideAfterCalcualte_) {
                // take a snapshot of pos0/dir0 then apply delta pos/dir
                corner.Dir0 = cornerDir;
                corner.Pos0 = cornerPos;
            }

            cornerPos += CornerData.TransformCoordinates(corner.DeltaPos, outwardDir, Vector3.up, forwardDir);
            cornerDir += CornerData.TransformCoordinates(corner.DeltaDir, outwardDir, Vector3.up, forwardDir);

            if (corner.LockLength) {
                float prevSqrmagnitiude = ((Vector3)corner.CachedDir).sqrMagnitude;
                float newSqrmagnitiude = cornerDir.sqrMagnitude;
                cornerDir *= Mathf.Sqrt(prevSqrmagnitiude / newSqrmagnitiude);
            }
        }

        #region External Mods
        public TernaryBool ShouldHideCrossingTexture() {
            if (NodeData != null && NodeData.NodeType == NodeTypeT.Stretch)
                return TernaryBool.False; // always ignore.
            if (NoMarkings)
                return TernaryBool.True; // always hide
            return TernaryBool.Undefined; // default.
        }

        #endregion
    }
}
