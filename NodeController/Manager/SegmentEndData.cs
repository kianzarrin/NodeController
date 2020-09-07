//#define CORNER_LEGACY

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
    using System.Runtime.Serialization;
    using UnityEngine;
    using CSURUtil = Util.CSURUtil;
    using Log = KianCommons.Log;

    [Serializable]
    public class SegmentEndData : INetworkData, INetworkData<SegmentEndData>, ISerializable {
        #region serialization
        //serialization
        public void GetObjectData(SerializationInfo info, StreamingContext context) =>
            SerializationUtil.GetObjectFields(info, this);

        /// <summary>clone</summary>
        private SegmentEndData(SegmentEndData template) =>
            HelpersExtensions.CopyProperties(this, template);

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

                LeftCorner.DeltaPos = info.GetVector3("DeltaLeftCornerPos");
                LeftCorner.DeltaDir = info.GetVector3("DeltaLeftCornerDir");
                RightCorner.DeltaPos = info.GetVector3("DeltaRightCornerPos");
                RightCorner.DeltaDir = info.GetVector3("DeltaRightCornerDir");
            }
            Update();
        }

        // intrinsic
        public ushort NodeID;
        public ushort SegmentID;
        public bool IsStartNode => NetUtil.IsStartNode(segmentId: SegmentID, nodeId: NodeID);

        public override string ToString() {
            return GetType().Name + $"(segment:{SegmentID} node:{NodeID})";
        }

        // defaults
        public float DefaultCornerOffset => CSURUtil.GetMinCornerOffset(NodeID);
        public bool DefaultFlatJunctions => Info.m_flatJunctions;
        public bool DefaultTwist => Info.m_flatJunctions /*|| Info.m_twistSegmentEnds*/ ;
        public NetSegment.Flags DefaultFlags;

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

        public void Update() {
            var st = new StackTrace(fNeedFileInfo: true);
            Log.Debug(this + "\n" + st.ToStringPretty());

            NetManager.instance.UpdateNode(NodeID);
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
            // both in SegmentEndData Cahced* and NetSegment.CalculateCorner()

            Segment.CalculateCorner(SegmentID, true, IsStartNode, leftSide: true,
                cornerPos: out var lpos, cornerDirection: out var ldir, out _);
            Segment.CalculateCorner(SegmentID, true, IsStartNode, leftSide: false,
                cornerPos: out var rpos, cornerDirection: out var rdir, out _);

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
                    }else if (activePanel.NetworkType == NetworkTypeT.SegmentEnd && this.IsSelected()) {
                        activePanel.RefreshValues();
                    }
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
            ret &= LeftCorner.IsDefault();
            ret &= RightCorner.IsDefault();
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
            Stretch = EmbankmentAngleDeg = 0;
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

        public float SlopeAngleDeg {
            get => DeltaSlopeAngleDeg + EndDirSlopeAngleDeg;
            set => DeltaSlopeAngleDeg = value - EndDirSlopeAngleDeg;
        }

        #endregion
        public ref CornerData Corner(bool left) {
            if (left)
                return ref LeftCorner;
            else
                return ref RightCorner;
        }

        // left and right going away from the junction.
        public CornerData LeftCorner = new CornerData { Left = true };
        public CornerData RightCorner = new CornerData { Left = false };

        [Serializable]
        public struct CornerData {
            public bool Left;

            public bool IsDefault() {
                bool ret = DeltaPos == Vector3.zero && DeltaDir == Vector3.zero;
                ret &= Offset == 0;
                ret &= LockLength == false;
                return ret;
            }

            public void ResetToDefault() {
                DeltaPos = DeltaDir = Vector3.zero;
                Offset = 0;
                LockLength = false;
            }

            public Vector3Serializable CachedPos, CachedDir;
            public Vector3Serializable Dir0, Pos0;
            public Vector3Serializable DeltaPos, DeltaDir;
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
                        value *= DirLength/value.magnitude;
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
                    Dir *= Mathf.Clamp(value,0.001f,1000) / DirLength;
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
            /// all directions going away fromt he junction
            /// </summary>
            public static void CalculateTransformVectors(Vector3 dir, bool left, out Vector3 outward, out Vector3 forward) {
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

        /// Precondition: cornerDir.LenXZ = 1
        /// <param name="leftSide">left side going away from the junction</param>
        public void ApplyCornerAdjustments(ref Vector3 cornerPos, ref Vector3 cornerDir, bool leftSide) {
            if (VectorUtils.LengthSqrXZ(cornerDir) > 1.001f) {
                // assumption for slope adjustments.
                Log.Error("Warning: ApplyCornerAdjustments() expects cornerDir.LenXZ == 1 got:" +
                    VectorUtils.LengthXZ(cornerDir));
            }

            CornerData.CalculateTransformVectors(
                dir: cornerDir,
                left: leftSide,
                outward: out var outwardDir,
                forward: out var forwardDir);

            float dirY0 = cornerDir.y;
            float angleDeg = SlopeAngleDeg; // save calculation time.
            float angleRad = angleDeg * Mathf.Deg2Rad;

            if (89 <= angleDeg && angleDeg <= 91) {
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
            float absY = Mathf.Abs(cornerDir.y);
            if (absY > 2) {
                // fix dir length so that y is 2:
                cornerDir *= 2 / absY;
            }


            Vector3 deltaPos = Vector3.zero;

            // embankment:
            float embankmentAngleRad = EmbankmentAngleDeg * Mathf.Deg2Rad;
            if (leftSide) embankmentAngleRad = -embankmentAngleRad;
            float sinEmbankmentAngle = Mathf.Sin(embankmentAngleRad);
            float cosEmbankmentAngle = Mathf.Cos(embankmentAngleRad);
            float hw0 = Info.m_halfWidth;
            float stretch = Stretch * 0.01f;
            float hw_total = hw0 * (1 + stretch);
            deltaPos.x += -hw_total * (1 - cosEmbankmentAngle); // outward
            deltaPos.y = hw_total * sinEmbankmentAngle;

            // Stretch:
            deltaPos.x += hw0 * stretch * cosEmbankmentAngle; // outward
            deltaPos.y += hw0 * stretch * sinEmbankmentAngle; // vertical

            cornerPos += CornerData.TransformCoordinates(deltaPos, outwardDir, Vector3.up, forwardDir);

            ref CornerData corner = ref Corner(leftSide);

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
