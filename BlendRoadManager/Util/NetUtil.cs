using ColossalFramework;
using ColossalFramework.Math;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlendRoadManager.Util {
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

        internal static ushort GetID(this NetNode node)
        {
            ref NetSegment seg = ref node.GetFirstSegment().ToSegment();
            bool startNode = Equals(ref node, seg.m_startNode);
            return startNode ? seg.m_startNode : seg.m_endNode;
        }
        
        internal static ushort GetID(this NetSegment segment)
        {
            foreach(var segmentID in GetSegmentsCoroutine(segment.m_startNode))
            {
                if(Equals(ref segment, segmentID))
                {
                    return segmentID;
                }
            }
            throw new Exception("unreachable code");
        }

        public static ushort GetFirstSegment(ushort nodeID) => nodeID.ToNode().GetFirstSegment();
        public static ushort GetFirstSegment(this ref NetNode node)
        {
            ushort segmentID = 0;
            int i;
            for (i = 0; i < 8; ++i)
            {
                segmentID = node.GetSegment(i);
                if (segmentID != 0)
                    break;
            }
            return segmentID;
        }

        public static Vector3 GetSegmentDir(ushort segmentID, ushort nodeID)
        {
            bool startNode = IsStartNode(segmentID, nodeID);
            ref NetSegment segment = ref segmentID.ToSegment();
            return startNode ? segment.m_startDirection : segment.m_endDirection;
        }


        internal static float MaxNodeHW(ushort nodeId) {
            float ret = 0;
            foreach(var segmentId in GetSegmentsCoroutine(nodeId)) {
                float hw = segmentId.ToSegment().Info.m_halfWidth;
                if (hw > ret)
                    ret = hw;
            }
            return ret;
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
                return (segmentId.ToSegment().m_flags &
                (NetSegment.Flags.Created | NetSegment.Flags.Deleted)) ==
                NetSegment.Flags.Created;
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
                ref  forward,
                ref backward);
            return (forward == 0) ^ (backward == 0);
        }

        #endregion

        public struct NodeSegments{
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
            HelpersExtensions.Assert(segmentID0!=0, "GetFirstSegment!=0");
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
    }
}
