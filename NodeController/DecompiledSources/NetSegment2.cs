using ColossalFramework;
using ColossalFramework.Math;
using KianCommons;
using UnityEngine;

namespace NodeController.DecompiledSources {
    struct NetSegment2 {
        // NetSegment
        NetInfo Info;
        ushort m_startNode, m_endNode;
        Vector3 m_startDirection, m_endDirection;

        public void CalculateCorner(ushort segmentID, bool heightOffset, bool start, bool leftSide, out Vector3 cornerPos, out Vector3 cornerDirection, out bool smooth) {
            NetInfo info = this.Info;
            NetManager instance = Singleton<NetManager>.instance;
            ushort nodeID = (!start) ? this.m_endNode : this.m_startNode;
            ushort nodeIDtail = (!start) ? this.m_startNode : this.m_endNode;
            Vector3 position = instance.m_nodes.m_buffer[nodeID].m_position;
            Vector3 positionTail = instance.m_nodes.m_buffer[nodeIDtail].m_position;
            Vector3 dir = (!start) ? this.m_endDirection : this.m_startDirection;
            Vector3 dirTail = (!start) ? this.m_startDirection : this.m_endDirection;
            NetSegment.CalculateCorner(
                info, position, positionTail, dir, dirTail,
                null, Vector3.zero, Vector3.zero, Vector3.zero,
                null, Vector3.zero, Vector3.zero, Vector3.zero,
                segmentID, nodeID, heightOffset, leftSide,
                out cornerPos, out cornerDirection, out smooth);
        }

        // simplified assuming its called by NetSegment.CalculateCornerOffset.
        public static void CalculateCorner(
            NetInfo info, Vector3 startPos, Vector3 endPos, Vector3 startDir, Vector3 endDir,
            ushort ignoreSegmentID, ushort startNodeID, bool heightOffset, bool leftSide,
            out Vector3 cornerPos, out Vector3 cornerDirection, out bool smooth) {
            NetManager instance = Singleton<NetManager>.instance;
            Bezier3 bezier = default(Bezier3);
            Bezier3 bezier2 = default(Bezier3);
            NetNode.Flags flags = NetNode.Flags.End;
            flags = instance.m_nodes.m_buffer[(int)startNodeID].m_flags;
            ushort startNodeBuildingID = startNodeID.ToNode().m_building;
            cornerDirection = startDir;
            smooth = flags.IsFlagSet(NetNode.Flags.Middle);

            float hw = info.m_halfWidth;
            if (!leftSide) hw = -hw;

            if (flags.IsFlagSet(NetNode.Flags.Middle)) {
                for (int i = 0; i < 8; ++i) {
                    ushort segmentID = startNodeID.ToNode().GetSegment(i);
                    if (segmentID == 0 || segmentID == ignoreSegmentID)
                        continue;
                    Vector3 dir = segmentID.ToSegment().GetDirection(startNodeID);
                    cornerDirection = VectorUtils.NormalizeXZ(cornerDirection - dir);
                    break;
                }
            }

            Vector3 dirAcross = Vector3.Cross(cornerDirection, Vector3.up).normalized;
            Vector3 VV1 = dirAcross;
            if (info.m_twistSegmentEnds) {
                if (startNodeBuildingID != 0) {
                    float angle = Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)startNodeBuildingID].m_angle;
                    Vector3 v = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    VV1 = (Vector3.Dot(VV1, v) < 0f) ? (-v) : v;
                } else if (flags.IsFlagSet(NetNode.Flags.Junction)) {
                    Vector3 v = default;
                    int untouchableCount = 0;
                    for (int i = 0; i < 8; i++) {
                        ushort segmentID = startNodeID.ToNode().GetSegment(i);
                        if (segmentID == 0 || segmentID == ignoreSegmentID) continue;

                        if (segmentID.ToSegment().m_flags.IsFlagSet(NetSegment.Flags.Untouchable)) {
                            Vector3 dir = segmentID.ToSegment().GetDirection(startNodeID);
                            v = new Vector3(dir.z, 0f, -dir.x); // rotate vector by 90 degree and remove vertical element.
                            untouchableCount++;
                        }
                    }
                    if (untouchableCount == 1) {
                        VV1 = ((Vector3.Dot(VV1, v) < 0f) ? (-v) : v);
                    }
                }
            }

            bezier.a = startPos + VV1 * hw;
            bezier2.a = startPos - VV1 * hw;
            cornerPos = bezier.a;
            if (
                (flags.IsFlagSet(NetNode.Flags.Junction) && info.m_clipSegmentEnds) ||
                flags.IsFlagSet(NetNode.Flags.Bend | NetNode.Flags.End)
                ) {
                VV1 = Vector3.Cross(endDir, Vector3.up).normalized;
                bezier.d = endPos - VV1 * hw;
                bezier2.d = endPos + VV1 * hw;
                NetSegment.CalculateMiddlePoints(bezier.a, cornerDirection, bezier.d, endDir, false, false, out bezier.b, out bezier.c);
                NetSegment.CalculateMiddlePoints(bezier2.a, cornerDirection, bezier2.d, endDir, false, false, out bezier2.b, out bezier2.c);
                Bezier2 bezier3 = Bezier2.XZ(bezier);
                Bezier2 bezier4 = Bezier2.XZ(bezier2);
                float num5 = -1f;
                float num6 = -1f;
                bool flag = false;
                float a = info.m_halfWidth * 0.5f;

                int segmentCount = 0;
                for (int i = 0; i < 8; ++i) {
                    ushort segmentID = startNodeID.ToNode().GetSegment(i);
                    if (segmentID == 0 || segmentID == ignoreSegmentID) continue;
                    Vector3 vector4 = instance.m_segments.m_buffer[(int)segmentID].GetDirection(startNodeID);
                    NetInfo netInfo = instance.m_segments.m_buffer[(int)segmentID].Info;
                    if (netInfo == null) continue;
                    if (info.m_clipSegmentEnds != netInfo.m_clipSegmentEnds) continue;
                    if (netInfo.m_netAI.GetSnapElevation() > info.m_netAI.GetSnapElevation()) {
                        float num10 = 0.01f - Mathf.Min(info.m_maxTurnAngleCos, netInfo.m_maxTurnAngleCos);
                        float num11 = vector4.x * startDir.x + vector4.z * startDir.z;
                        if ((info.m_vehicleTypes & netInfo.m_vehicleTypes) == VehicleInfo.VehicleType.None || num11 >= num10) {
                            continue;
                        }
                    }
                    a = Mathf.Max(a, netInfo.m_halfWidth * 0.5f);
                    segmentCount++;
                }
                if (segmentCount >= 1 || flags.IsFlagSet(NetNode.Flags.Outside)) {
                    for (int i = 0; i < 8; ++i) {
                        ushort segmentID = startNodeID.ToNode().GetSegment(i);
                        if (segmentID == 0 || segmentID == ignoreSegmentID) continue;
                        ushort startNode2 = instance.m_segments.m_buffer[(int)segmentID].m_startNode;
                        ushort num12 = instance.m_segments.m_buffer[(int)segmentID].m_endNode;
                        Vector3 vector6 = instance.m_segments.m_buffer[(int)segmentID].m_startDirection;
                        Vector3 vector7 = instance.m_segments.m_buffer[(int)segmentID].m_endDirection;
                        if (startNodeID != startNode2) {
                            num12 = startNode2;
                            //swap
                            Vector3 temp = vector6;
                            vector6 = vector7;
                            vector7 = temp;
                        }
                        NetInfo netInfo = instance.m_segments.m_buffer[(int)segmentID].Info;
                        if (netInfo == null) continue;
                        if (info.m_clipSegmentEnds != netInfo.m_clipSegmentEnds) continue;
                        Vector3 vector5 = instance.m_nodes.m_buffer[(int)num12].m_position;
                        if (netInfo.m_netAI.GetSnapElevation() > info.m_netAI.GetSnapElevation()) {
                            float num14 = 0.01f - Mathf.Min(info.m_maxTurnAngleCos, netInfo.m_maxTurnAngleCos);
                            float num15 = vector6.x * startDir.x + vector6.z * startDir.z;
                            if ((info.m_vehicleTypes & netInfo.m_vehicleTypes) == VehicleInfo.VehicleType.None || num15 >= num14) {
                                continue;
                            }
                        }
                        if (vector6.z * cornerDirection.x - vector6.x * cornerDirection.z > 0f == leftSide) {
                            Bezier3 bezier5 = default(Bezier3);
                            float num16 = Mathf.Max(a, netInfo.m_halfWidth);
                            if (!leftSide) {
                                num16 = -num16;
                            }
                            VV1 = Vector3.Cross(vector6, Vector3.up).normalized;
                            bezier5.a = startPos - VV1 * num16;
                            VV1 = Vector3.Cross(vector7, Vector3.up).normalized;
                            bezier5.d = vector5 + VV1 * num16;
                            NetSegment.CalculateMiddlePoints(bezier5.a, vector6, bezier5.d, vector7, false, false, out bezier5.b, out bezier5.c);
                            Bezier2 b2 = Bezier2.XZ(bezier5);
                            float b3;
                            float num17;
                            if (bezier3.Intersect(b2, out b3, out num17, 6)) {
                                num5 = Mathf.Max(num5, b3);
                            } else if (bezier3.Intersect(b2.a, b2.a - VectorUtils.XZ(vector6) * 16f, out b3, out num17, 6)) {
                                num5 = Mathf.Max(num5, b3);
                            } else if (b2.Intersect(bezier3.d + (bezier3.d - bezier4.d) * 0.01f, bezier4.d, out b3, out num17, 6)) {
                                num5 = Mathf.Max(num5, 1f);
                            }
                            float num18 = cornerDirection.x * vector6.x + cornerDirection.z * vector6.z;
                            if (num18 >= -0.75f) {
                                flag = true;
                            }
                        }

                        Bezier3 bezier6 = default(Bezier3);
                        float num19 = cornerDirection.x * vector6.x + cornerDirection.z * vector6.z;
                        if (num19 >= 0f) {
                            vector6.x -= cornerDirection.x * num19 * 2f;
                            vector6.z -= cornerDirection.z * num19 * 2f;
                        }
                        float num20 = Mathf.Max(a, netInfo.m_halfWidth);
                        if (!leftSide) {
                            num20 = -num20;
                        }
                        VV1 = Vector3.Cross(vector6, Vector3.up).normalized;
                        bezier6.a = startPos + VV1 * num20;
                        VV1 = Vector3.Cross(vector7, Vector3.up).normalized;
                        bezier6.d = vector5 - VV1 * num20;
                        NetSegment.CalculateMiddlePoints(bezier6.a, vector6, bezier6.d, vector7, false, false, out bezier6.b, out bezier6.c);
                        Bezier2 b4 = Bezier2.XZ(bezier6);
                        float b5;
                        float num21;
                        if (bezier4.Intersect(b4, out b5, out num21, 6)) {
                            num6 = Mathf.Max(num6, b5);
                            continue;
                        }
                        if (bezier4.Intersect(b4.a, b4.a - VectorUtils.XZ(vector6) * 16f, out b5, out num21, 6)) {
                            num6 = Mathf.Max(num6, b5);
                            continue;
                        }
                        if (b4.Intersect(bezier3.d, bezier4.d + (bezier4.d - bezier3.d) * 0.01f, out b5, out num21, 6)) {
                            num6 = Mathf.Max(num6, 1f);
                            continue;
                        }
                        continue;
                    }
                    if ((flags & NetNode.Flags.Junction) != NetNode.Flags.None) {
                        if (!flag) {
                            num5 = Mathf.Max(num5, num6);
                        }
                    } else if ((flags & NetNode.Flags.Bend) != NetNode.Flags.None && !flag) {
                        num5 = Mathf.Max(num5, num6);
                    }
                    if ((flags & NetNode.Flags.Outside) != NetNode.Flags.None) {
                        float num22 = 8640f;
                        Vector2 vector9 = new Vector2(-num22, -num22);
                        Vector2 vector10 = new Vector2(-num22, num22);
                        Vector2 vector11 = new Vector2(num22, num22);
                        Vector2 vector12 = new Vector2(num22, -num22);
                        float b6;
                        float num23;
                        if (bezier3.Intersect(vector9, vector10, out b6, out num23, 6)) {
                            num5 = Mathf.Max(num5, b6);
                        }
                        if (bezier3.Intersect(vector10, vector11, out b6, out num23, 6)) {
                            num5 = Mathf.Max(num5, b6);
                        }
                        if (bezier3.Intersect(vector11, vector12, out b6, out num23, 6)) {
                            num5 = Mathf.Max(num5, b6);
                        }
                        if (bezier3.Intersect(vector12, vector9, out b6, out num23, 6)) {
                            num5 = Mathf.Max(num5, b6);
                        }
                        num5 = Mathf.Clamp01(num5);
                    } else {
                        if (num5 < 0f) {
                            if (info.m_halfWidth < 4f) {
                                num5 = 0f;
                            } else {
                                num5 = bezier3.Travel(0f, 8f);
                            }
                        }
                        float num24 = info.m_minCornerOffset;
                        if ((flags & (NetNode.Flags.AsymForward | NetNode.Flags.AsymBackward)) != NetNode.Flags.None) {
                            num24 = Mathf.Max(num24, 8f);
                        }
                        num5 = Mathf.Clamp01(num5);
                        float num25 = VectorUtils.LengthXZ(bezier.Position(num5) - bezier.a);
                        num5 = bezier3.Travel(num5, Mathf.Max(num24 - num25, 2f));
                        if (info.m_straightSegmentEnds) {
                            if (num6 < 0f) {
                                if (info.m_halfWidth < 4f) {
                                    num6 = 0f;
                                } else {
                                    num6 = bezier4.Travel(0f, 8f);
                                }
                            }
                            num6 = Mathf.Clamp01(num6);
                            num25 = VectorUtils.LengthXZ(bezier2.Position(num6) - bezier2.a);
                            num6 = bezier4.Travel(num6, Mathf.Max(info.m_minCornerOffset - num25, 2f));
                            num5 = Mathf.Max(num5, num6);
                        }
                    }
                    float y = cornerDirection.y;
                    cornerDirection = bezier.Tangent(num5);
                    cornerDirection.y = 0f;
                    cornerDirection.Normalize();
                    if (!info.m_flatJunctions) {
                        cornerDirection.y = y;
                    }
                    cornerPos = bezier.Position(num5);
                    cornerPos.y = startPos.y;
                }
            } else if ((flags & NetNode.Flags.Junction) != NetNode.Flags.None && info.m_minCornerOffset >= 0.01f) {
                VV1 = Vector3.Cross(endDir, Vector3.up).normalized;
                bezier.d = endPos - VV1 * hw;
                bezier2.d = endPos + VV1 * hw;
                NetSegment.CalculateMiddlePoints(bezier.a, cornerDirection, bezier.d, endDir, false, false, out bezier.b, out bezier.c);
                NetSegment.CalculateMiddlePoints(bezier2.a, cornerDirection, bezier2.d, endDir, false, false, out bezier2.b, out bezier2.c);
                Bezier2 bezier7 = Bezier2.XZ(bezier);
                Bezier2 bezier8 = Bezier2.XZ(bezier2);
                float num26;
                if (info.m_halfWidth < 4f) {
                    num26 = 0f;
                } else {
                    num26 = bezier7.Travel(0f, 8f);
                }
                num26 = Mathf.Clamp01(num26);
                float num27 = VectorUtils.LengthXZ(bezier.Position(num26) - bezier.a);
                num26 = bezier7.Travel(num26, Mathf.Max(info.m_minCornerOffset - num27, 2f));
                if (info.m_straightSegmentEnds) {
                    float num28;
                    if (info.m_halfWidth < 4f) {
                        num28 = 0f;
                    } else {
                        num28 = bezier8.Travel(0f, 8f);
                    }
                    num28 = Mathf.Clamp01(num28);
                    num27 = VectorUtils.LengthXZ(bezier2.Position(num28) - bezier2.a);
                    num28 = bezier8.Travel(num28, Mathf.Max(info.m_minCornerOffset - num27, 2f));
                    num26 = Mathf.Max(num26, num28);
                }
                float y2 = cornerDirection.y;
                cornerDirection = bezier.Tangent(num26);
                cornerDirection.y = 0f;
                cornerDirection.Normalize();
                if (!info.m_flatJunctions) {
                    cornerDirection.y = y2;
                }
                cornerPos = bezier.Position(num26);
                cornerPos.y = startPos.y;
            }
            if (heightOffset && startNodeID != 0) {
                cornerPos.y += (float)instance.m_nodes.m_buffer[(int)startNodeID].m_heightOffset * 0.015625f;
            }
        }

    }
}
