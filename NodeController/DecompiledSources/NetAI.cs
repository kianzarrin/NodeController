using ColossalFramework;
using ColossalFramework.Math;
using KianCommons;
using System;
using UnityEngine;
using KianCommons;

namespace NodeController.DecompiledSources {
    class NetAI {
        // NetAI
        public NetInfo m_info;

        public virtual void UpdateLanes(ushort segmentID, ref NetSegment segment, bool loading) {
            NetManager instance = Singleton<NetManager>.instance;
            bool flag = Singleton<SimulationManager>.instance.m_metaData.m_invertTraffic == SimulationMetaData.MetaBool.True;
            uint prevLaneID = 0u;
            uint laneID = segment.m_lanes;
            segment.CalculateCorner(segmentID, heightOffset: true, start: true, true,
                out var cornerPosStartLeft,  out var cornerDirStartLeft, out _);
            segment.CalculateCorner(segmentID, heightOffset: true, start: false, leftSide: true,
                out var cornerPosEndLeft,    out var cornerDirEndLeft, out bool smoothStart);
            segment.CalculateCorner(segmentID, heightOffset: true, start: true, leftSide: false,
                out var cornerPosStartRight, out var cornerDirStartRight, out _);
            segment.CalculateCorner(segmentID, heightOffset: true, start: false, leftSide: false,
                out var cornerPosEndRight,   out var cornerDirEndRight, out bool smoothEnd);

            bool segmentInverted = segment.m_flags.IsFlagSet(NetSegment.Flags. Invert);

            float cc = 128 / Mathf.PI;//40.7436638f
            Vector3 deltaPosStart = cornerPosStartRight - cornerPosStartLeft;
            Vector3 deltaPosEnd = cornerPosEndRight - cornerPosEndLeft;
            if (segmentInverted) {
                segment.m_cornerAngleStart = (byte)(Mathf.RoundToInt(Mathf.Atan2(deltaPosStart.z, deltaPosStart.x) * cc) & 255);
                segment.m_cornerAngleEnd = (byte)(Mathf.RoundToInt(Mathf.Atan2(-deltaPosEnd.z, -deltaPosEnd.x) * cc) & 255);
            } else {
                segment.m_cornerAngleStart = (byte)(Mathf.RoundToInt(Mathf.Atan2(-deltaPosStart.z, -deltaPosStart.x) * cc) & 255);
                segment.m_cornerAngleEnd = (byte)(Mathf.RoundToInt(Mathf.Atan2(deltaPosEnd.z, deltaPosEnd.x) * cc) & 255);
            }
            NetLane.Flags flags = NetLane.Flags.None;
            if (segment.m_flags.IsFlagSet(NetSegment.Flags.YieldStart)) {
                flags |= segmentInverted ? NetLane.Flags.YieldStart : NetLane.Flags.YieldEnd;
            }
            if (segment.m_flags.IsFlagSet(NetSegment.Flags.YieldEnd)) {
                flags |= segmentInverted ? NetLane.Flags.YieldEnd : NetLane.Flags.YieldStart;
            }
            float lengthAcc = 0f;
            float laneCount = 0f;
            for (int i = 0; i < this.m_info.m_lanes.Length; i++) {
                if (laneID == 0u) {
                    if (!Singleton<NetManager>.instance.CreateLanes(
                        out laneID, ref Singleton<SimulationManager>.instance.m_randomizer, segmentID, 1)) {
                        break;
                    }
                    if (prevLaneID != 0u) {
                        prevLaneID.ToLane().m_nextLane = laneID;
                    } else {
                        segment.m_lanes = laneID;
                    }
                }
                NetInfo.Lane laneInfo = this.m_info.m_lanes[i];
                float lanePos01 = laneInfo.m_position / (this.m_info.m_halfWidth * 2f) + 0.5f; // lane pos rescaled between 0~1
                if (segmentInverted) {
                    lanePos01 = 1f - lanePos01;
                }
                Vector3 startPos = cornerPosStartLeft + (cornerPosStartRight - cornerPosStartLeft) * lanePos01;
                Vector3 startDir = Vector3.Lerp(cornerDirStartLeft, cornerDirStartRight, lanePos01);
                Vector3 endPos = cornerPosEndRight + (cornerPosEndLeft - cornerPosEndRight) * lanePos01;
                Vector3 endDir = Vector3.Lerp(cornerDirEndRight, cornerDirEndLeft, lanePos01);
                startPos.y += laneInfo.m_verticalOffset;
                endPos.y += laneInfo.m_verticalOffset;
                Vector3 b;
                Vector3 c;
                NetSegment.CalculateMiddlePoints(startPos, startDir, endPos, endDir, smoothStart, smoothEnd, out b, out c);
                NetLane.Flags flags2 = laneID.ToLane().Flags();
                NetLane.Flags flags3 = flags;
                flags2 &= ~(NetLane.Flags.YieldStart | NetLane.Flags.YieldEnd);
                if ((byte)(laneInfo.m_finalDirection & NetInfo.Direction.Both) == 2) {
                    flags3 &= ~NetLane.Flags.YieldEnd;
                }
                if ((byte)(laneInfo.m_finalDirection & NetInfo.Direction.Both) == 1) {
                    flags3 &= ~NetLane.Flags.YieldStart;
                }
                flags2 |= flags3;
                if (flag) {
                    flags2 |= NetLane.Flags.Inverted;
                } else {
                    flags2 &= ~NetLane.Flags.Inverted;
                }
                laneID.ToLane().m_bezier = new Bezier3(startPos, b, c, endPos);
                laneID.ToLane().m_segment = segmentID;
                laneID.ToLane().m_flags = (ushort)flags2;
                laneID.ToLane().m_firstTarget = 0;
                laneID.ToLane().m_lastTarget = byte.MaxValue;
                lengthAcc += laneID.ToLane().UpdateLength();
                laneCount += 1f;
                prevLaneID = laneID;
                laneID = laneID.ToLane().m_nextLane;
            }
            if (laneCount != 0f) {
                segment.m_averageLength = lengthAcc / laneCount;
            } else {
                segment.m_averageLength = 0f;
            }
        }

    }
}
