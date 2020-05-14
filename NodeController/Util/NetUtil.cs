using ColossalFramework;
using ColossalFramework.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeController.Util {
    public class NetServiceException : Exception {
        public NetServiceException(string m) : base(m) { }
        public NetServiceException() : base() { }
        public NetServiceException(string m, Exception e) : base(m, e) { }
    }

    public static class NetUtil {
        public const float SAFETY_NET = 0.02f;

        public static NetManager netMan => NetManager.instance;
        public static NetTool netTool => Singleton<NetTool>.instance;
        public static SimulationManager simMan => Singleton<SimulationManager>.instance;
        public static TerrainManager terrainMan => TerrainManager.instance;

        public const float MPU = 8f; // meter per unit
        internal static ref NetNode ToNode(this ushort id) => ref netMan.m_nodes.m_buffer[id];
        internal static ref NetSegment ToSegment(this ushort id) => ref netMan.m_segments.m_buffer[id];
        internal static ref NetLane ToLane(this uint id) => ref netMan.m_lanes.m_buffer[id];
        public static bool IsCSUR(NetInfo info) {
            if (info == null ||
                (info.m_netAI.GetType() != typeof(RoadAI) &&
                info.m_netAI.GetType() != typeof(RoadBridgeAI) &&
                info.m_netAI.GetType() != typeof(RoadTunnelAI))) {
                return false;
            }
            return info.name.Contains(".CSUR ");
        }

        public static ToolBase.ToolErrors InsertNode(NetTool.ControlPoint controlPoint, out ushort nodeId, bool test = false) {
            var ret = NetTool.CreateNode(
                controlPoint.m_segment.ToSegment().Info,
                controlPoint, controlPoint, controlPoint,
                NetTool.m_nodePositionsSimulation,
                maxSegments: 0,
                test: test, visualize: false, autoFix: true, needMoney: false,
                invert: false, switchDir: false,
                relocateBuildingID: 0,
                out nodeId, out var newSegment, out var cost, out var productionRate);
            if (!test) {
                nodeId.ToNode().m_flags |= NetNode.Flags.Middle | NetNode.Flags.Moveable;
            }
            //Log.Debug($"[InsertNode] test={test} errors:{ret} nodeId:{nodeId} newSegment:{newSegment} cost:{cost} productionRate{productionRate}");
            return ret;
        }

        internal static int CountPedestrianLanes(this NetInfo info) =>
            info.m_lanes.Count(lane => lane.m_laneType == NetInfo.LaneType.Pedestrian);

        static bool Equals(this ref NetNode node1, ushort nodeId2) {
            ref NetNode node2 = ref nodeId2.ToNode();
            return node1.m_buildIndex == node2.m_buildIndex &&
                   node1.m_position == node2.m_position;
        }

        static bool Equals(this ref NetSegment segment1, ushort segmentId2) {
            ref NetSegment segment2 = ref segmentId2.ToSegment();
            return (segment1.m_startNode == segment2.m_startNode) &
                   (segment1.m_endNode == segment2.m_endNode);
        }

        internal static ushort GetID(this NetNode node) {
            ref NetSegment seg = ref node.GetFirstSegment().ToSegment();
            bool startNode = Equals(ref node, seg.m_startNode);
            return startNode ? seg.m_startNode : seg.m_endNode;
        }

        internal static ushort GetID(this NetSegment segment) {
            foreach (var segmentID in GetSegmentsCoroutine(segment.m_startNode)) {
                if (Equals(ref segment, segmentID)) {
                    return segmentID;
                }
            }
            throw new Exception("unreachable code");
        }

        public static ushort GetFirstSegment(ushort nodeID) => nodeID.ToNode().GetFirstSegment();
        public static ushort GetFirstSegment(this ref NetNode node) {
            ushort segmentID = 0;
            int i;
            for (i = 0; i < 8; ++i) {
                segmentID = node.GetSegment(i);
                if (segmentID != 0)
                    break;
            }
            return segmentID;
        }

        public static Vector3 GetSegmentDir(ushort segmentID, ushort nodeID) {
            bool startNode = IsStartNode(segmentID, nodeID);
            ref NetSegment segment = ref segmentID.ToSegment();
            return startNode ? segment.m_startDirection : segment.m_endDirection;
        }


        internal static float MaxNodeHW(ushort nodeId) {
            float ret = 0;
            foreach (var segmentId in GetSegmentsCoroutine(nodeId)) {
                float hw = segmentId.ToSegment().Info.m_halfWidth;
                if (hw > ret)
                    ret = hw;
            }
            return ret;
        }

        /// Note: inverted flag or LHT does not influce the beizer.
        internal static Bezier3 CalculateSegmentBezier3(this ref NetSegment seg) {
            ref NetNode startNode = ref seg.m_startNode.ToNode();
            ref NetNode endNode = ref seg.m_endNode.ToNode();
            Bezier3 bezier = new Bezier3 {
                a = startNode.m_position,
                d = endNode.m_position,
            };
            NetSegment.CalculateMiddlePoints(
                bezier.a, seg.m_startDirection,
                bezier.d, seg.m_endDirection,
                startNode.m_flags.IsFlagSet(NetNode.Flags.Middle),
                endNode.m_flags.IsFlagSet(NetNode.Flags.Middle),
                out bezier.b,
                out bezier.c);
            return bezier;
        }


        #region copied from TMPE
        public static bool LHT => TrafficDrivesOnLeft;
        public static bool RHT => !LHT;
        public static bool TrafficDrivesOnLeft =>
            Singleton<SimulationManager>.instance.m_metaData.m_invertTraffic
                == SimulationMetaData.MetaBool.True;

        public static bool CanConnectPathToSegment(ushort segmentID) =>
            segmentID.ToSegment().CanConnectPath();

        public static bool CanConnectPath(this ref NetSegment segment) =>
            segment.Info.m_netAI is RoadAI & segment.Info.m_hasPedestrianLanes;

        public static bool CanConnectPath(this NetInfo info) =>
            info.m_netAI is RoadAI & info.m_hasPedestrianLanes;

        public static bool IsStartNode(ushort segmentId, ushort nodeId) =>
            segmentId.ToSegment().m_startNode == nodeId;

        public static bool HasNode(ushort segmentId, ushort nodeId) =>
            segmentId.ToSegment().m_startNode == nodeId || segmentId.ToSegment().m_endNode == nodeId;

        public static ushort GetSharedNode(ushort segmentID1, ushort segmentID2) =>
            segmentID1.ToSegment().GetSharedNode(segmentID2);

        public static bool IsSegmentValid(ushort segmentId) {
            if (segmentId != 0) {
                return segmentId.ToSegment().m_flags.
                    CheckFlags(required: NetSegment.Flags.Created, forbidden: NetSegment.Flags.Deleted);
            }
            return false;
        }

        public static bool IsNodeValid(ushort nodeId) {
            if (nodeId != 0) {
                return nodeId.ToNode().m_flags.
                    CheckFlags(required: NetNode.Flags.Created, forbidden: NetNode.Flags.Deleted);
            }
            return false;
        }

        public static ushort GetHeadNode(ref NetSegment segment) {
            // tail node>-------->head node
            bool invert = (segment.m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
            invert = invert ^ LHT;
            if (invert) {
                return segment.m_startNode;
            } else {
                return segment.m_endNode;
            }
        }

        public static ushort GetHeadNode(ushort segmentId) =>
            GetHeadNode(ref segmentId.ToSegment());

        public static ushort GetTailNode(ref NetSegment segment) {
            bool invert = (segment.m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None;
            invert = invert ^ LHT;
            if (!invert) {
                return segment.m_startNode;
            } else {
                return segment.m_endNode;
            }//endif
        }

        public static ushort GetTailNode(ushort segmentId) =>
            GetTailNode(ref segmentId.ToSegment());

        public static bool CalculateIsOneWay(ushort segmentId) {
            int forward = 0;
            int backward = 0;
            segmentId.ToSegment().CountLanes(
                segmentId,
                NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle,
                VehicleInfo.VehicleType.Car | VehicleInfo.VehicleType.Train |
                VehicleInfo.VehicleType.Tram | VehicleInfo.VehicleType.Metro |
                VehicleInfo.VehicleType.Monorail,
                ref forward,
                ref backward);
            return (forward == 0) ^ (backward == 0);
        }

        #endregion

        public struct NodeSegments {
            public ushort[] segments;
            public int count;
            void Add(ushort segmentID) {
                segments[count++] = segmentID;
            }

            public NodeSegments(ushort nodeID) {
                segments = new ushort[8];
                count = 0;

                ushort segmentID = GetFirstSegment(nodeID);
                Add(segmentID);
                while (true) {
                    segmentID = segmentID.ToSegment().GetLeftSegment(nodeID);
                    if (segmentID == segments[0])
                        break;
                    else
                        Add(segmentID);
                }
            }
        }

        /// <summary>
        /// returns a counter-clockwise list of segments of the given node ID.
        /// </summary>
        public static IEnumerable<ushort> GetCCSegList(ushort nodeID) {
            ushort segmentID0 = GetFirstSegment(nodeID);
            HelpersExtensions.Assert(segmentID0 != 0, "GetFirstSegment!=0");
            yield return segmentID0;
            ushort segmentID = segmentID0;

            // add the rest of the segments.
            while (true) {
                segmentID = segmentID.ToSegment().GetRightSegment(nodeID);
                if ((segmentID == 0) | (segmentID == segmentID0))
                    yield break;
                else
                    yield return segmentID;
            }
        }

        /// <summary>
        /// returns a clock-wise list of segments of the given node ID.
        /// </summary>
        public static IEnumerable<ushort> GetCWSegList(ushort nodeID) {
            ushort segmentID0 = GetFirstSegment(nodeID);
            HelpersExtensions.Assert(segmentID0 != 0, "GetFirstSegment!=0");
            yield return segmentID0;
            ushort segmentID = segmentID0;

            // add the rest of the segments.
            while (true) {
                segmentID = segmentID.ToSegment().GetLeftSegment(nodeID);
                if ((segmentID == 0) | (segmentID == segmentID0))
                    yield break;
                else
                    yield return segmentID;
            }
        }

        public static IEnumerable<ushort> GetSegmentsCoroutine(ushort nodeID) {
            for (int i = 0; i < 8; ++i) {
                ushort segmentID = nodeID.ToNode().GetSegment(i);
                if (segmentID != 0) {
                    yield return segmentID;
                }
            }
        }

        public static void LaneTest(ushort segmentId) {
            string message = $"STRANGE LANE ISSUE: lane count mismatch for " +
                $"segment:{segmentId} Info:{segmentId.ToSegment().Info} IsSegmentValid={IsSegmentValid(segmentId)}\n";
            if (segmentId.ToSegment().Info != null) {

                var laneIDs = new List<uint>();
                for (uint laneID = segmentId.ToSegment().m_lanes;
                    laneID != 0;
                    laneID = laneID.ToLane().m_nextLane) {
                    laneIDs.Add(laneID);
                }

                var laneInfos = segmentId.ToSegment().Info.m_lanes;

                if (laneIDs.Count == laneInfos.Length) return;

                string m1 = "laneIDs=\n";
                foreach (uint laneID in laneIDs)
                    m1 += $"\tlaneID:{laneID} flags:{laneID.ToLane().m_flags} segment:{laneID.ToLane().m_segment} bezier.a={laneID.ToLane().m_bezier.a}\n";

                string m2 = "laneInfoss=\n";
                for (int i = 0; i < laneInfos.Length; ++i) {
                    m2 += $"\tlaneID:{laneInfos[i]} dir:{laneInfos[i].m_direction} laneType:{laneInfos[i].m_laneType} vehicleType:{laneInfos[i].m_vehicleType} pos:{laneInfos[i].m_position}\n";
                }

                message += m1 + m2;
            }
            Util.Log.Error(message, true);
            Util.Log.LogToFileSimple(file: "NodeControler.Strage.log", message: message);
        }

        // TODO use start node instead of direction.
        public static IEnumerable<LaneData> GetLanesCoroutine(
            ushort segmentId,
            NetInfo.Direction? direction,
            NetInfo.LaneType laneType = NetInfo.LaneType.All,
            VehicleInfo.VehicleType vehicleType = VehicleInfo.VehicleType.All) {

            LaneTest(segmentId);

            int idx = 0;
            if (segmentId.ToSegment().Info == null) {
                Log.Error("null info: potentially cuased by missing assets");
                yield break;
            }
            int n = segmentId.ToSegment().Info.m_lanes.Length;
            for (uint laneID = segmentId.ToSegment().m_lanes;
                laneID != 0 && idx < n;
                laneID = laneID.ToLane().m_nextLane, idx++) {
                var ret = new LaneData {
                    LaneID = laneID,
                    LaneIndex = idx,
                    LaneInfo = segmentId.ToSegment().Info.m_lanes[idx]
                };
                if (direction != null && direction != ret.LaneInfo.m_direction)
                    continue;
                if (!ret.LaneInfo.m_laneType.IsFlagSet(laneType))
                    continue;
                if (!ret.LaneInfo.m_vehicleType.IsFlagSet(vehicleType))
                    continue;
                yield return ret;
            }
        }

        public struct LaneData {
            public uint LaneID;
            public int LaneIndex;
            public NetInfo.Lane LaneInfo;
            public ushort SegmentID => LaneID.ToLane().m_segment;
        }
    }
}
