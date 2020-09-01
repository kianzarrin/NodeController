namespace NodeController {
    using ColossalFramework;
    using ColossalFramework.Math;
    using ColossalFramework.UI;
    using CSUtil.Commons;
    using KianCommons;
    using KianCommons.Math;
    using NodeController.GUI;
    using NodeController.Tool;
    using System;
    using System.Diagnostics;
    using System.Security.Policy;
    using TrafficManager.Geometry.Impl;
    using UnityEngine;
    using CSURUtil = Util.CSURUtil;
    using Log = KianCommons.Log;

    [Serializable]
    public class SegmentEndData: INetworkData, INetworkData<SegmentEndData> {
        // intrinsic
        public ushort NodeID;
        public ushort SegmentID;
        public bool IsStartNode => NetUtil.IsStartNode(segmentId: SegmentID, nodeId: NodeID);

        public override string ToString() {
            return GetType().Name + $"(segment:{SegmentID} node:{NodeID})";
        }

        /// <summary>clone</summary>
        private SegmentEndData(SegmentEndData template) =>
            HelpersExtensions.CopyProperties(this, template);

        public SegmentEndData Clone() => new SegmentEndData(this);

        // defaults
        public float DefaultCornerOffset => CSURUtil.GetMinCornerOffset(NodeID);
        public bool DefaultFlatJunctions => Info.m_flatJunctions;
        public bool DefaultTwist => Info.m_twistSegmentEnds;
        public NetSegment.Flags DefaultFlags;

        // cache
        public bool HasPedestrianLanes;
        public float CurveRaduis0;
        public int PedestrianLaneCount;
        public Vector3Serializable CachedLeftCornerPos, CachedLeftCornerDir, CachedRightCornerPos, CachedRightCornerDir;// left and right is when you go away form junction
        // corner pos/dir after CornerOffset, FlatJunctions, Slope, Stretch, and EmbankmentAngle, but before DeltaLeft/RighCornerDir/Pos are applied.
        public Vector3Serializable LeftCornerDir0, RightCornerDir0, LeftCornerPos0, RightCornerPos0;
        public float CachedSuperElevationDeg; // rightward rotation of the road when going away from the junction.

        // Configurable
        public bool NoCrossings;
        public bool NoMarkings;
        public bool NoJunctionTexture;
        public bool NoJunctionProps; // excluding TL
        public bool NoTLProps;
        public Vector3Serializable DeltaLeftCornerPos, DeltaLeftCornerDir, DeltaRightCornerPos, DeltaRightCornerDir; // left and right is when you go away form junction
        public bool FlatJunctions;
        public bool Twist;
        public float CornerOffsetLeft, CorneroffsetRight; // going away form junction. in GUI its the opposite.

        public float Stretch; //increase width
        public float EmbankmentAngleDeg;
        public float DeltaSlopeAngleDeg;

        // shortcuts
        public ref NetSegment Segment => ref SegmentID.ToSegment();
        public ref NetNode Node => ref NodeID.ToNode();
        public NodeData NodeData => NodeManager.Instance.buffer[NodeID];
        public ref NodeTypeT NodeType => ref NodeData.NodeType;
        public Vector3 Direction => Segment.GetDirection(NodeID);


        public SegmentEndData(ushort segmentID, ushort nodeID) {
            NodeID = nodeID;
            SegmentID = segmentID;

            Calculate();
            CornerOffset = DefaultCornerOffset;
            FlatJunctions = DefaultFlatJunctions;
            DeltaSlopeAngleDeg = 0;
            Log.Debug($"SegmentEndData() Direction={Direction} Slope={SlopeAngleDeg}");
            Twist = DefaultTwist;
            HelpersExtensions.Assert(IsDefault());
            Update();
        }

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
            if (HelpersExtensions.VERBOSE)
                Log.Debug("SegmentEndData.Refresh() for this\n" + Environment.StackTrace);

            if (!CanModifyOffset()) {
                Log.Debug("SegmentEndData.Refresh(): setting CornerOffset = DefaultCornerOffset");
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
                Stretch = EmbankmentAngleDeg = 0;
            }
        }

        public void Update() => NetManager.instance.UpdateNode(NodeID);

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
            // both in SegmentEndData Cahced* and NetSegment.CalculateCorner()

            Segment.CalculateCorner(SegmentID, true, IsStartNode, leftSide: true,
                cornerPos: out var lpos, cornerDirection: out var ldir, out _);
            Segment.CalculateCorner(SegmentID, true, IsStartNode, leftSide: false,
                cornerPos: out var rpos, cornerDirection: out var rdir, out _);

            CachedLeftCornerPos = lpos;
            CachedRightCornerPos = rpos;
            CachedLeftCornerDir = ldir;
            CachedRightCornerDir = rdir;

            Vector3 diff = rpos - lpos;
            float se = Mathf.Atan2(diff.y, VectorUtils.LengthXZ(diff));
            CachedSuperElevationDeg = se * Mathf.Rad2Deg;

            SimulationManager.instance.m_ThreadingWrapper.QueueMainThread(delegate () {
                var activePanel = UIPanelBase.ActivePanel;
                if (activePanel.NetworkType == NetworkTypeT.Node && NodeID == SelectedNodeID) {
                    activePanel.RefreshValues();
                }
                if (activePanel.NetworkType == NetworkTypeT.SegmentEnd && this.IsSelected()) {
                    activePanel.RefreshValues();
                }
            });
            insideAfterCalcualte_ = false;
        }

        static ushort SelectedSegmentID => NodeControllerTool.Instance.SelectedSegmentID;
        static ushort SelectedNodeID => NodeControllerTool.Instance.SelectedNodeID;
        public bool IsSelected() => NodeID == SelectedNodeID && SegmentID == SelectedSegmentID;

        public bool IsDefault() {
            bool ret = Mathf.Abs(CornerOffset - DefaultCornerOffset) < 0.1f;
            ret &= DeltaSlopeAngleDeg == 0;
            ret &= FlatJunctions == DefaultFlatJunctions;
            ret &= Twist == DefaultTwist;
            ret &= NoCrossings == false;
            ret &= NoMarkings == false;
            ret &= NoJunctionTexture == false;
            ret &= NoJunctionProps == false;
            ret &= NoTLProps == false;
            ret &= DeltaRightCornerPos == Vector3.zero;
            ret &= DeltaRightCornerDir == Vector3.zero;
            ret &= DeltaLeftCornerPos == Vector3.zero;
            ret &= DeltaLeftCornerDir == Vector3.zero;
            ret &= Stretch == 0;
            ret &= EmbankmentAngleDeg == 0;

            return ret;
        }

        public void ResetToDefault() {
            CornerOffset = DefaultCornerOffset;
            DeltaSlopeAngleDeg = 0;
            FlatJunctions = DefaultFlatJunctions;
            Twist = DefaultTwist;
            NoCrossings = false;
            NoMarkings = false;
            NoJunctionTexture = false;
            NoJunctionProps = false;
            NoTLProps = false;
            DeltaRightCornerPos = DeltaRightCornerDir = DeltaLeftCornerPos = DeltaLeftCornerDir = Vector3.zero;
            Stretch = EmbankmentAngleDeg = 0;
            RefreshAndUpdate();
        }


        #region GUI Data Conversions
        public float CornerOffset {
            get => (CornerOffsetLeft + CorneroffsetRight) * 0.5f;
            set => CornerOffsetLeft = CorneroffsetRight = value;
        }

        public bool HasUniformCornerOffset() => CornerOffsetLeft == CorneroffsetRight;

        public float EmbankmentPercent {
            get => Mathf.Tan(EmbankmentAngleDeg*Mathf.Deg2Rad) * 100;
            set => EmbankmentAngleDeg = Mathf.Atan(value * 0.01f) *Mathf.Rad2Deg;
        }

        public float SlopeAngleDeg {
            get => DeltaSlopeAngleDeg + EndDirSlopeAngleDeg;
            set => DeltaSlopeAngleDeg = value - EndDirSlopeAngleDeg;
        }

        /// <summary>
        /// reverse transformed coordinates.
        /// </summary>
        public Vector3 LeftCornerDir {
            get {
                CalculateTransformVectors(LeftCornerDir0, true, out var outward, out var forward);
                return ReverseTransformCoordinats(CachedLeftCornerDir, outward, Vector3.up, forward);
            }
            set {
                CalculateTransformVectors(LeftCornerDir0, true, out var outward, out var forward);
                DeltaLeftCornerDir = value - ReverseTransformCoordinats( LeftCornerDir0, outward, Vector3.up, forward);
                CachedLeftCornerDir = LeftCornerDir0 + TransformCoordinates(DeltaLeftCornerDir, outward, Vector3.up, forward);
                Update();
            }
        }

        public Vector3 RightCornerDir {
            get {
                CalculateTransformVectors(RightCornerDir0, false, out var outward, out var forward);
                return ReverseTransformCoordinats(CachedRightCornerDir, outward, Vector3.up, forward);
            }
            set {
                CalculateTransformVectors(RightCornerDir0, false, out var outward, out var forward);
                DeltaRightCornerDir = value - ReverseTransformCoordinats(RightCornerDir0, outward, Vector3.up, forward);
                CachedRightCornerDir = RightCornerDir0 + TransformCoordinates(DeltaRightCornerDir, outward, Vector3.up, forward);
                Update();
            }
        }

        // shortcuts
        public void SetLeftCornerDirI(float val, int index) => LeftCornerDir = LeftCornerDir.SetI(val, index);
        public void SetRightCornerDirI(float val, int index) => RightCornerDir = RightCornerDir.SetI(val, index);

        public Vector3 RightCornerPos {
            get => CachedRightCornerPos;
            set {
                CalculateTransformVectors(RightCornerDir0, left: false, outward: out var outwardDir, forward: out var forwardDir);
                CachedRightCornerPos = value;
                DeltaRightCornerPos = ReverseTransformCoordinats(value - RightCornerPos0, outwardDir, Vector3.up, forwardDir);
                Update();
            }
        }

        public Vector3 LeftCornerPos {
            get => CachedLeftCornerPos;
            set {
                CalculateTransformVectors(LeftCornerDir0, left: true, outward: out var outwardDir, forward: out var forwardDir);
                CachedLeftCornerPos = value;
                DeltaLeftCornerPos = ReverseTransformCoordinats(value - LeftCornerPos0, outwardDir, Vector3.up, forwardDir);
                Update();
            }
        }

        /// <summary>
        /// all directions going away fromt he junction
        /// </summary>
        public void CalculateTransformVectors(Vector3 dir, bool left, out Vector3 outward, out Vector3 forward) {
            Vector3 rightward = Vector3.Cross(Vector3.up, dir).normalized; // going away from the junction
            Vector3 leftward = -rightward;
            forward = new Vector3(dir.x, 0, dir.z).normalized; // going away from the junction
            outward = left ? leftward : rightward;
        }

        /// <summary>
        /// tranforms input vector from relative (to x y z inputs) coordinate to absulute coodinate.
        /// </summary>
        public static Vector3 TransformCoordinates(Vector3 v, Vector3 x, Vector3 y, Vector3 z)
            => v.x * x + v.y * y + v.z * z;

        public static Vector3 ReverseTransformCoordinats(Vector3 v, Vector3 x, Vector3 y, Vector3 z) {
            Vector3 ret = default;
            ret.x = Vector3.Dot(v, x);
            ret.y = Vector3.Dot(v, y);
            ret.z = Vector3.Dot(v, z);
            return ret;
        }
        #endregion

        bool CrossingIsRemoved() =>
            HideCrosswalks.Patches.CalculateMaterialCommons.
            ShouldHideCrossing(NodeID, SegmentID);

        #region show/hide in UI
        public bool IsCSUR => NetUtil.IsCSUR(Info);
        public NetInfo Info => Segment.Info;
        public bool CanModifyOffset() => NodeData?.CanModifyOffset() ?? false;
        public bool CanModifyCorners() => NodeData != null &&
            (CanModifyOffset() || NodeType == NodeTypeT.End || NodeType == NodeTypeT.Middle);
        public bool CanModifyFlatJunctions() => NodeData?.CanModifyFlatJunctions() ?? false;
        public bool CanModifyTwist() {
            if (NodeData == null || NodeData.SegmentCount < 3)
                return false;

            // get neighbouring segment data
            ushort segmentID1 = Segment.GetLeftSegment(NodeID);
            ushort segmentID2 = Segment.GetRightSegment(NodeID);
            var segEnd1 = SegmentEndManager.Instance.GetOrCreate(segmentID1, NodeID);
            var segEnd2 = SegmentEndManager.Instance.GetOrCreate(segmentID2, NodeID);

            Log.Debug($"{this}.CanModifyTwist() flatjunctions: {segEnd1.FlatJunctions} {segEnd2.FlatJunctions}" );
            bool slope1 = !segEnd1.FlatJunctions;
            bool slope2 = !segEnd2.FlatJunctions;
            return slope1 || slope2;
        }
        public bool ShowClearMarkingsToggle() {
            if (IsCSUR) return false;
            if (NodeData == null) return true;
            return NodeData.NodeType == NodeTypeT.Custom;
        }


        #endregion

        public float EndDirSlopeAngleDeg => Mathf.Atan(Direction.y) * Mathf.Rad2Deg;

        /// <param name="leftSide">left side going away from the junction</param>
        public void ApplyCornerAdjustments(ref Vector3 cornerPos, ref Vector3 cornerDir, bool leftSide) {
            CalculateTransformVectors(dir: cornerDir, left: leftSide, outward: out var outwardDir, forward: out var forwardDir);

            float dirY0 = cornerDir.y;
            float angleDeg = SlopeAngleDeg; // save calculation time.
            float angleRad = angleDeg * Mathf.Deg2Rad;

            if ( 89 <= angleDeg && angleDeg <= 91) {
                cornerDir.x = cornerDir.z = 0;
                cornerDir.y = 1;
            } else if (-89 >= angleDeg && angleDeg >= -91) {
                cornerDir.x = cornerDir.z = 0;
                cornerDir.y = -1;
            } else if (angleDeg > 90 || angleDeg < -90) {
                cornerDir.y = -Mathf.Tan(angleRad);
                cornerDir.x = -cornerDir.x;
                cornerDir.z = -cornerDir.z;
            } else {
                cornerDir.y = Mathf.Tan(angleRad);
            }
            if (!Node.m_flags.IsFlagSet(NetNode.Flags.Middle)) {
                float d = VectorUtils.DotXZ(cornerPos - Node.m_position, cornerDir);
                cornerPos.y += d * (cornerDir.y - dirY0); 
            }

            // this must be done after readjusting cornerPos.y
            // make sure direction vector is not too big.
            float maxY = Mathf.Max(dirY0, 2);
            if (cornerDir.y > maxY)
                cornerDir *= maxY / cornerDir.y;


            Vector3 deltaPos = Vector3.zero;

            // embankment:
            float embankmentAngleRad = EmbankmentAngleDeg * Mathf.Deg2Rad;
            if (leftSide) embankmentAngleRad = -embankmentAngleRad;
            float sinEmbankmentAngle = Mathf.Sin(embankmentAngleRad);
            float cosEmbankmentAngle = Mathf.Cos(embankmentAngleRad);
            float hw0 = Info.m_halfWidth;
            float stretch = Stretch * 0.01f;
            float hw_total = hw0 * (1+ stretch);
            deltaPos.x += -hw_total * (1 - cosEmbankmentAngle); // outward
            deltaPos.y = hw_total * sinEmbankmentAngle;

            // Stretch:
            deltaPos.x += hw0 * stretch * cosEmbankmentAngle; // outward
            deltaPos.y += hw0 * stretch * sinEmbankmentAngle; // vertical

            cornerPos += TransformCoordinates(deltaPos, outwardDir, Vector3.up, forwardDir);

            // take a snapshot of pos0/dir0 then apply delta pos/dir
            if (leftSide) {
                if (insideAfterCalcualte_) {
                    LeftCornerDir0 = cornerDir;
                    LeftCornerPos0 = cornerPos;
                }

                cornerPos += TransformCoordinates(DeltaLeftCornerPos, outwardDir, Vector3.up, forwardDir);
                cornerDir += TransformCoordinates(DeltaLeftCornerDir, outwardDir, Vector3.up, forwardDir);
            } else {
                if (insideAfterCalcualte_) {
                    RightCornerDir0 = cornerDir;
                    RightCornerPos0 = cornerPos;
                }

                cornerPos += TransformCoordinates(DeltaRightCornerPos, outwardDir, Vector3.up, forwardDir);
                cornerDir += TransformCoordinates(DeltaRightCornerDir, outwardDir, Vector3.up, forwardDir);
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
