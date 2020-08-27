namespace NodeController {
    using ColossalFramework;
    using ColossalFramework.Math;
    using CSUtil.Commons;
    using KianCommons;
    using KianCommons.Math;
    using NodeController.GUI;
    using System;
    using UnityEngine;
    using CSURUtil = Util.CSURUtil;
    using Log = KianCommons.Log;

    [Serializable]
    public class SegmentEndData {
        // intrinsic
        public ushort NodeID;
        public ushort SegmentID;
        public bool IsStartNode => NetUtil.IsStartNode(segmentId: SegmentID, nodeId: NodeID);

        public override string ToString() {
            return GetType().Name + $"(segment:{SegmentID} node:{NodeID})";
        }

        /// <summary>clone</summary>
        public SegmentEndData(SegmentEndData template) =>
            HelpersExtensions.CopyProperties(this, template);

        public SegmentEndData Clone() => new SegmentEndData(this);

        // defaults
        public float DefaultCornerOffset => CSURUtil.GetMinCornerOffset(NodeID);
        public bool DefaultFlatJunctions => Info.m_flatJunctions;
        public bool DefaultTwist => Info.m_twistSegmentEnds;
        public float DefaultSlopeAngleDeg;
        public NetSegment.Flags DefaultFlags;

        // cache
        public bool HasPedestrianLanes;
        public float CurveRaduis0;
        public int PedestrianLaneCount;
        public Vector3Serializable CachedLeftCornerPos, CachedLeftCornerDir, CachedRightCornerPos, CachedRightCornerDir;// left and right is when you go away form junction
        // corner pos/dir after CornerOffset, FlatJunctions, Slope, Stretch, and EmbankmentAngle, but before DeltaLeft/RighCornerDir/Pos are applied.
        public Vector3Serializable LeftCornerDir0, RightCornerDir0, LeftCornerPos0, RightCornerPos0;
        public float SuperElevationDeg; // rightward rotation of the road when going away from the junction.

        // Configurable
        public bool NoCrossings;
        public bool NoMarkings;
        public bool NoJunctionTexture;
        public bool NoJunctionProps; // excluding TL
        public bool NoTLProps;
        public Vector3Serializable DeltaLeftCornerPos, DeltaLeftCornerDir, DeltaRightCornerPos, DeltaRightCornerDir; // left and right is when you go away form junction
        public float CornerOffset;
        public bool FlatJunctions;
        public bool Twist;

        public float Stretch; //increase width
        public float EmbankmentAngleDeg;
        public float SlopeAngleDeg;
        public bool UpdateSlopeAngleDeg = true;

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
            SlopeAngleDeg = DefaultSlopeAngleDeg = CurrentSlopeAngleDeg;
            UpdateSlopeAngleDeg = false;
            Log.Debug($"SegmentEndData() Direction={Direction} Slope={SlopeAngleDeg}");
            Twist = DefaultTwist;
            HelpersExtensions.Assert(IsDefault());
        }

        public void Calculate() {
            DefaultFlags = Segment.m_flags;
            PedestrianLaneCount = Info.CountPedestrianLanes();

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
            SuperElevationDeg = se * Mathf.Rad2Deg;

            Refresh();
        }

        public bool IsDefault() {
            bool ret = Mathf.Abs(CornerOffset - DefaultCornerOffset) < 0.1f;
            ret &= Mathf.Abs(SlopeAngleDeg - DefaultSlopeAngleDeg) < 1; // TODO change to minimum possible.
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
            SlopeAngleDeg = DefaultSlopeAngleDeg;
            FlatJunctions = DefaultFlatJunctions;
            Twist = DefaultTwist;
            NoCrossings = false;
            NoMarkings = false;
            NoJunctionTexture = false;
            NoJunctionProps = false;
            NoTLProps = false;
            DeltaRightCornerPos = DeltaRightCornerDir = DeltaLeftCornerPos = DeltaLeftCornerDir = default;
            Stretch = EmbankmentAngleDeg = 0;
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
            if (!CanModifyTwist()) {
                Twist = DefaultTwist;
            }
            if(!CanModifyCorners()) {
                SlopeAngleDeg = DefaultSlopeAngleDeg;
                Stretch = EmbankmentAngleDeg = 0;
            }

            Log.Debug($"SegmentEndData.Refresh() Updating segment:{SegmentID} node:{NodeID} CornerOffset={CornerOffset}");
            if (HelpersExtensions.VERBOSE)
                Log.Debug(Environment.StackTrace);

            NetManager.instance.UpdateNode(NodeID);
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

            return segEnd1.FlatJunctions || segEnd2.FlatJunctions;
        }
        public bool ShowClearMarkingsToggle() {
            if (IsCSUR) return false;
            if (NodeData == null) return true;
            return NodeData.NodeType == NodeTypeT.Custom;
        }
        #endregion

        public float CurrentSlopeAngleDeg => Mathf.Atan(Direction.y) * Mathf.Rad2Deg;

        /// <param name="leftSide">left side going away from the junction</param>
        public void ApplyCornerAdjustments(ref Vector3 cornerPos, ref Vector3 cornerDir, bool leftSide) {
            Vector3 rightwardDir = Vector3.Cross(Vector3.up, cornerDir).normalized; // going away from the junction
            Vector3 leftwardDir = -rightwardDir;
            Vector3 forwardDir = new Vector3(cornerDir.x, 0, cornerDir.z).normalized; // going away from the junction

            // TODO calculate slope in CalculateCorner.Posfix()

            // slope:
            if (UpdateSlopeAngleDeg) {
                SlopeAngleDeg = CurrentSlopeAngleDeg;
                UpdateSlopeAngleDeg = false;
                SimulationManager.instance.m_ThreadingWrapper.QueueMainThread(delegate () { 
                    if (UINodeControllerPanel.Instance.isVisible)
                        UINodeControllerPanel.Instance.Refresh();
                    if (UISegmentEndControllerPanel.Instance.isVisible)
                        UISegmentEndControllerPanel.Instance.Refresh();
                });
            }

            float dirY0 = cornerDir.y;
            float angleRad = SlopeAngleDeg * Mathf.Deg2Rad;
            //Log.Debug($"SlopeAngleDeg={SlopeAngleDeg} CurrentSlopeAngleDeg={CurrentSlopeAngleDeg} " +
            //    $"cornerDir={cornerDir} segmentEndDir={Direction}");

            if ( 89 <= SlopeAngleDeg && SlopeAngleDeg <= 91) {
                cornerDir.x = cornerDir.z = 0;
                cornerDir.y = 1;
            } else if (-89 >= SlopeAngleDeg && SlopeAngleDeg >= -91) {
                cornerDir.x = cornerDir.z = 0;
                cornerDir.y = -1;
            } else if (SlopeAngleDeg > 90 || SlopeAngleDeg < -90) {
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

            if (leftSide) {
                cornerPos += TransformCoordinates(deltaPos, leftwardDir, Vector3.up, forwardDir);
                LeftCornerDir0 = cornerDir;
                LeftCornerPos0 = cornerPos;

                cornerPos += TransformCoordinates(DeltaLeftCornerPos, leftwardDir, Vector3.up, forwardDir);
                cornerDir += TransformCoordinates(DeltaLeftCornerDir, leftwardDir, Vector3.up, forwardDir);
            } else {
                cornerPos += TransformCoordinates(deltaPos, rightwardDir, Vector3.up, forwardDir);
                RightCornerDir0 = cornerDir;
                RightCornerPos0 = cornerPos;

                cornerPos += TransformCoordinates(DeltaRightCornerPos, rightwardDir, Vector3.up, forwardDir);
                cornerDir += TransformCoordinates(DeltaRightCornerDir, rightwardDir, Vector3.up, forwardDir);
            }


        }

        /// <summary>
        /// tranforms input vector from relative (to x y x inputs) coordinate to absulute coodinate.
        /// </summary>
        public static Vector3 TransformCoordinates(Vector3 v, Vector3 x, Vector3 y, Vector3 z)
            => v.x * x + v.y * y + v.z * z;

        /// <returns>if position was changed</returns>
        public Vector3 MoveLeftCornerToAbsolutePos(Vector3 pos) {
            Vector3 rightwardDir = Vector3.Cross(Vector3.up, LeftCornerDir0).normalized; // going away from the junction
            Vector3 leftwardDir = -rightwardDir;
            Vector3 forwardDir = new Vector3(LeftCornerDir0.x, 0, LeftCornerDir0.z).normalized; // going away from the junction

            Vector3 delta = pos - CachedLeftCornerPos;
            bool changed = delta.sqrMagnitude > 1e-4f;
            if (changed) {
                CachedLeftCornerPos = pos;
                DeltaLeftCornerPos = ReverseTransformCoordinats(pos - LeftCornerPos0, leftwardDir, Vector3.up, forwardDir);
                Refresh();
                return delta;
            }
            return Vector3.zero;
        }

        public void MoveLeftCornerToReltativePos(Vector3 deltaPos) =>
            MoveLeftCornerToAbsolutePos(CachedLeftCornerPos + deltaPos);

        public Vector3 MoveRightCornerToAbsolutePos(Vector3 pos) {
            Vector3 rightwardDir = Vector3.Cross(Vector3.up, RightCornerDir0).normalized; // going away from the junction
            Vector3 leftwardDir = -rightwardDir;
            Vector3 forwardDir = new Vector3(RightCornerDir0.x, 0, RightCornerDir0.z).normalized; // going away from the junction

            Vector3 delta = pos - CachedRightCornerPos;
            bool changed = delta.sqrMagnitude > 1e-4f;
            if (changed) {
                CachedRightCornerPos = pos;
                DeltaRightCornerPos = ReverseTransformCoordinats(pos - RightCornerPos0, rightwardDir, Vector3.up, forwardDir);
                Refresh();
                return delta;
            }
            return Vector3.zero;
        }

        public void MoveRightCornerToReltativePos(Vector3 deltaPos) =>
            MoveRightCornerToAbsolutePos(CachedRightCornerPos + deltaPos);


        public static Vector3 ReverseTransformCoordinats(Vector3 v, Vector3 x, Vector3 y, Vector3 z) {
            Vector3 ret = default;
            ret.x = Vector3.Dot(v, x);
            ret.y = Vector3.Dot(v, y);
            ret.z = Vector3.Dot(v, z);
            return ret;
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
