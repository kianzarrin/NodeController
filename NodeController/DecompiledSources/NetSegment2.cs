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
        NetSegment.Flags m_flags;

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
        public static void CalculateMiddlePoints(Vector3 startPos, Vector3 startDir, Vector3 endPos, Vector3 endDir, bool smoothStart, bool smoothEnd, out Vector3 middlePos1, out Vector3 middlePos2, out float distance) {
            if (NetSegment.IsStraight(startPos, startDir, endPos, endDir, out distance)) {
                middlePos1 = startPos + startDir * (distance * ((!smoothStart) ? 0.15f : 0.3f));
                middlePos2 = endPos + endDir * (distance * ((!smoothEnd) ? 0.15f : 0.3f));
            } else {
                float dotxz = VectorUtils.DotXZ(startDir, endDir);
                if (dotxz >= -0.999f &&
                    Line2.Intersect(
                        VectorUtils.XZ(startPos),
                        VectorUtils.XZ(startPos + startDir),
                        VectorUtils.XZ(endPos),
                        VectorUtils.XZ(endPos + endDir),
                        out float u,
                        out float v)) {
                    u = Mathf.Clamp(u, distance * 0.1f, distance);
                    v = Mathf.Clamp(v, distance * 0.1f, distance);
                    float uv = u + v;
                    middlePos1 = startPos + startDir * Mathf.Min(u, uv * 0.3f);
                    middlePos2 = endPos + endDir * Mathf.Min(v, uv * 0.3f);
                } else {
                    middlePos1 = startPos + startDir * (distance * 0.3f);
                    middlePos2 = endPos + endDir * (distance * 0.3f);
                }
            }
        }

        private void RenderInstance(RenderManager.CameraInfo cameraInfo, ushort segmentID, int layerMask, NetInfo info, ref RenderManager.Instance data)
        {
            NetManager instance = Singleton<NetManager>.instance;
            if (data.m_dirty)
            {
                data.m_dirty = false;
                Vector3 startPos = instance.m_nodes.m_buffer[(int)this.m_startNode].m_position;
                Vector3 endPos = instance.m_nodes.m_buffer[(int)this.m_endNode].m_position;
                data.m_position = (startPos + endPos) * 0.5f;
                data.m_rotation = Quaternion.identity;
                data.m_dataColor0 = info.m_color;
                data.m_dataColor0.a = 0f;
                data.m_dataFloat0 = Singleton<WeatherManager>.instance.GetWindSpeed(data.m_position);
                data.m_dataVector0 = new Vector4(0.5f / info.m_halfWidth, 1f / info.m_segmentLength, 1f, 1f);
                Vector4 colorLocation = RenderManager.GetColorLocation((uint)(49152 + segmentID));
                Vector4 vector = colorLocation;
                if (NetNode.BlendJunction(this.m_startNode))
                {
                    colorLocation = RenderManager.GetColorLocation(86016u + (uint)this.m_startNode);
                }
                if (NetNode.BlendJunction(this.m_endNode))
                {
                    vector = RenderManager.GetColorLocation(86016u + (uint)this.m_endNode);
                }
                data.m_dataVector3 = new Vector4(colorLocation.x, colorLocation.y, vector.x, vector.y);
                if (info.m_segments == null || info.m_segments.Length == 0)
                {
                    if (info.m_lanes != null)
                    {
                        bool invert;
                        if ((this.m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None)
                        {
                            invert = true;
                            NetInfo info2 = instance.m_nodes.m_buffer[(int)this.m_endNode].Info;
                            NetNode.Flags flags;
                            Color color;
                            info2.m_netAI.GetNodeState(this.m_endNode, ref instance.m_nodes.m_buffer[(int)this.m_endNode], segmentID, ref this, out flags, out color);
                            NetInfo info3 = instance.m_nodes.m_buffer[(int)this.m_startNode].Info;
                            NetNode.Flags flags2;
                            Color color2;
                            info3.m_netAI.GetNodeState(this.m_startNode, ref instance.m_nodes.m_buffer[(int)this.m_startNode], segmentID, ref this, out flags2, out color2);
                        }
                        else
                        {
                            invert = false;
                            NetInfo info4 = instance.m_nodes.m_buffer[(int)this.m_startNode].Info;
                            NetNode.Flags flags;
                            Color color;
                            info4.m_netAI.GetNodeState(this.m_startNode, ref instance.m_nodes.m_buffer[(int)this.m_startNode], segmentID, ref this, out flags, out color);
                            NetInfo info5 = instance.m_nodes.m_buffer[(int)this.m_endNode].Info;
                            NetNode.Flags flags2;
                            Color color2;
                            info5.m_netAI.GetNodeState(this.m_endNode, ref instance.m_nodes.m_buffer[(int)this.m_endNode], segmentID, ref this, out flags2, out color2);
                        }
                        float startAngle = (float)this.m_cornerAngleStart * 0.0245436933f;
                        float endAngle = (float)this.m_cornerAngleEnd * 0.0245436933f;
                        int num = 0;
                        uint num2 = this.m_lanes;
                        int num3 = 0;
                        while (num3 < info.m_lanes.Length && num2 != 0u)
                        {
                            instance.m_lanes.m_buffer[(int)((UIntPtr)num2)].RefreshInstance(num2, info.m_lanes[num3], startAngle, endAngle, invert, ref data, ref num);
                            num2 = instance.m_lanes.m_buffer[(int)((UIntPtr)num2)].m_nextLane;
                            num3++;
                        }
                    }
                }
                else
                {
                    float vscale = info.m_netAI.GetVScale();

                    this.CalculateCorner(segmentID, true, true, true, out var startPosLeft, out var startDirLeft, out _);
                    this.CalculateCorner(segmentID, true, false, true, out var endPosLeft, out var endDirLeft, out _);
                    this.CalculateCorner(segmentID, true, true, false, out var startPosRight, out var startDirRight, out var smoothStart);
                    this.CalculateCorner(segmentID, true, false, false, out var endPosRight, out var endDirRight, out var smoothEnd);

                    NetSegment.CalculateMiddlePoints(
                        startPosLeft, startDirLeft, endPosRight, endDirRight, smoothStart, smoothEnd, out var b1, out var c1);
                    NetSegment.CalculateMiddlePoints(
                        startPosRight, startDirRight, endPosLeft, endDirLeft, smoothStart, smoothEnd, out var b2, out var c2);

                    data.m_dataMatrix0 = NetSegment.CalculateControlMatrix(
                        startPosLeft, b1, c1, endPosRight, startPosRight, b2, c2, endPosLeft, data.m_position, vscale);
                    data.m_dataMatrix1 = NetSegment.CalculateControlMatrix(
                        startPosRight, b2, c2, endPosLeft, startPosLeft, b1, c1, endPosRight, data.m_position, vscale);
                }
                if ((this.m_flags & NetSegment.Flags.NameVisible2) != NetSegment.Flags.None)
                {
                    string segmentName = instance.GetSegmentName(segmentID);
                    UIFont nameFont = instance.m_properties.m_nameFont;
                    data.m_nameData = Singleton<InstanceManager>.instance.GetNameData(segmentName, nameFont, true);
                    if (data.m_nameData != null)
                    {
                        float snapElevation = info.m_netAI.GetSnapElevation();
                        startPos.y += snapElevation;
                        endPos.y += snapElevation;
                        Vector3 b;
                        Vector3 c;
                        NetSegment.CalculateMiddlePoints(
                            startPos, this.m_startDirection, endPos, this.m_endDirection, true, true, out b, out c);
                        data.m_dataMatrix2 = NetSegment.CalculateControlMatrix(
                            startPos, b, c, endPos, data.m_position, 1f);
                    }
                }
                else
                {
                    data.m_nameData = null;
                }
                if (info.m_requireSurfaceMaps)
                {
                    Singleton<TerrainManager>.instance.GetSurfaceMapping(data.m_position, out data.m_dataTexture0, out data.m_dataTexture1, out data.m_dataVector1);
                }
                else if (info.m_requireHeightMap)
                {
                    Singleton<TerrainManager>.instance.GetHeightMapping(data.m_position, out data.m_dataTexture0, out data.m_dataVector1, out data.m_dataVector2);
                }
            }
            if (info.m_segments != null && (layerMask & info.m_netLayers) != 0)
            {
                for (int i = 0; i < info.m_segments.Length; i++)
                {
                    NetInfo.Segment segment = info.m_segments[i];
                    bool flag;
                    if (segment.CheckFlags(this.m_flags, out flag))
                    {
                        Vector4 dataVector = data.m_dataVector3;
                        Vector4 dataVector2 = data.m_dataVector0;
                        if (segment.m_requireWindSpeed)
                        {
                            dataVector.w = data.m_dataFloat0;
                        }
                        if (flag)
                        {
                            dataVector2.x = -dataVector2.x;
                            dataVector2.y = -dataVector2.y;
                        }
                        if (cameraInfo.CheckRenderDistance(data.m_position, segment.m_lodRenderDistance))
                        {
                            instance.m_materialBlock.Clear();
                            instance.m_materialBlock.SetMatrix(instance.ID_LeftMatrix, data.m_dataMatrix0);
                            instance.m_materialBlock.SetMatrix(instance.ID_RightMatrix, data.m_dataMatrix1);
                            instance.m_materialBlock.SetVector(instance.ID_MeshScale, dataVector2);
                            instance.m_materialBlock.SetVector(instance.ID_ObjectIndex, dataVector);
                            instance.m_materialBlock.SetColor(instance.ID_Color, data.m_dataColor0);
                            if (segment.m_requireSurfaceMaps && data.m_dataTexture0 != null)
                            {
                                instance.m_materialBlock.SetTexture(instance.ID_SurfaceTexA, data.m_dataTexture0);
                                instance.m_materialBlock.SetTexture(instance.ID_SurfaceTexB, data.m_dataTexture1);
                                instance.m_materialBlock.SetVector(instance.ID_SurfaceMapping, data.m_dataVector1);
                            }
                            else if (segment.m_requireHeightMap && data.m_dataTexture0 != null)
                            {
                                instance.m_materialBlock.SetTexture(instance.ID_HeightMap, data.m_dataTexture0);
                                instance.m_materialBlock.SetVector(instance.ID_HeightMapping, data.m_dataVector1);
                                instance.m_materialBlock.SetVector(instance.ID_SurfaceMapping, data.m_dataVector2);
                            }
                            NetManager netManager = instance;
                            netManager.m_drawCallData.m_defaultCalls = netManager.m_drawCallData.m_defaultCalls + 1;
                            Graphics.DrawMesh(segment.m_segmentMesh, data.m_position, data.m_rotation, segment.m_segmentMaterial, segment.m_layer, null, 0, instance.m_materialBlock);
                        }
                        else
                        {
                            NetInfo.LodValue combinedLod = segment.m_combinedLod;
                            if (combinedLod != null)
                            {
                                if (segment.m_requireSurfaceMaps)
                                {
                                    if (data.m_dataTexture0 != combinedLod.m_surfaceTexA)
                                    {
                                        if (combinedLod.m_lodCount != 0)
                                        {
                                            NetSegment.RenderLod(cameraInfo, combinedLod);
                                        }
                                        combinedLod.m_surfaceTexA = data.m_dataTexture0;
                                        combinedLod.m_surfaceTexB = data.m_dataTexture1;
                                        combinedLod.m_surfaceMapping = data.m_dataVector1;
                                    }
                                }
                                else if (segment.m_requireHeightMap && data.m_dataTexture0 != combinedLod.m_heightMap)
                                {
                                    if (combinedLod.m_lodCount != 0)
                                    {
                                        NetSegment.RenderLod(cameraInfo, combinedLod);
                                    }
                                    combinedLod.m_heightMap = data.m_dataTexture0;
                                    combinedLod.m_heightMapping = data.m_dataVector1;
                                    combinedLod.m_surfaceMapping = data.m_dataVector2;
                                }
                                combinedLod.m_leftMatrices[combinedLod.m_lodCount] = data.m_dataMatrix0;
                                combinedLod.m_rightMatrices[combinedLod.m_lodCount] = data.m_dataMatrix1;
                                combinedLod.m_meshScales[combinedLod.m_lodCount] = dataVector2;
                                combinedLod.m_objectIndices[combinedLod.m_lodCount] = dataVector;
                                combinedLod.m_meshLocations[combinedLod.m_lodCount] = data.m_position;
                                combinedLod.m_lodMin = Vector3.Min(combinedLod.m_lodMin, data.m_position);
                                combinedLod.m_lodMax = Vector3.Max(combinedLod.m_lodMax, data.m_position);
                                if (++combinedLod.m_lodCount == combinedLod.m_leftMatrices.Length)
                                {
                                    NetSegment.RenderLod(cameraInfo, combinedLod);
                                }
                            }
                        }
                    }
                }
            }
            if (info.m_lanes != null && ((layerMask & info.m_propLayers) != 0 || cameraInfo.CheckRenderDistance(data.m_position, info.m_maxPropDistance + 128f)))
            {
                bool invert2;
                NetNode.Flags startFlags;
                Color startColor;
                NetNode.Flags endFlags;
                Color endColor;
                if ((this.m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None)
                {
                    invert2 = true;
                    NetInfo info6 = instance.m_nodes.m_buffer[(int)this.m_endNode].Info;
                    info6.m_netAI.GetNodeState(this.m_endNode, ref instance.m_nodes.m_buffer[(int)this.m_endNode], segmentID, ref this, out startFlags, out startColor);
                    NetInfo info7 = instance.m_nodes.m_buffer[(int)this.m_startNode].Info;
                    info7.m_netAI.GetNodeState(this.m_startNode, ref instance.m_nodes.m_buffer[(int)this.m_startNode], segmentID, ref this, out endFlags, out endColor);
                }
                else
                {
                    invert2 = false;
                    NetInfo info8 = instance.m_nodes.m_buffer[(int)this.m_startNode].Info;
                    info8.m_netAI.GetNodeState(this.m_startNode, ref instance.m_nodes.m_buffer[(int)this.m_startNode], segmentID, ref this, out startFlags, out startColor);
                    NetInfo info9 = instance.m_nodes.m_buffer[(int)this.m_endNode].Info;
                    info9.m_netAI.GetNodeState(this.m_endNode, ref instance.m_nodes.m_buffer[(int)this.m_endNode], segmentID, ref this, out endFlags, out endColor);
                }
                float startAngle2 = (float)this.m_cornerAngleStart * 0.0245436933f;
                float endAngle2 = (float)this.m_cornerAngleEnd * 0.0245436933f;
                Vector4 objectIndex = new Vector4(data.m_dataVector3.x, data.m_dataVector3.y, 1f, data.m_dataFloat0);
                Vector4 objectIndex2 = new Vector4(data.m_dataVector3.z, data.m_dataVector3.w, 1f, data.m_dataFloat0);
                InfoManager.InfoMode currentMode = Singleton<InfoManager>.instance.CurrentMode;
                if (currentMode != InfoManager.InfoMode.None && !info.m_netAI.ColorizeProps(currentMode))
                {
                    objectIndex.z = 0f;
                    objectIndex2.z = 0f;
                }
                int num4 = (info.m_segments != null && info.m_segments.Length != 0) ? -1 : 0;
                uint num5 = this.m_lanes;
                if ((this.m_flags & NetSegment.Flags.Collapsed) != NetSegment.Flags.None)
                {
                    int num6 = 0;
                    while (num6 < info.m_lanes.Length && num5 != 0u)
                    {
                        instance.m_lanes.m_buffer[(int)((UIntPtr)num5)].RenderDestroyedInstance(cameraInfo, segmentID, num5, info, info.m_lanes[num6], startFlags, endFlags, startColor, endColor, startAngle2, endAngle2, invert2, layerMask, objectIndex, objectIndex2, ref data, ref num4);
                        num5 = instance.m_lanes.m_buffer[(int)((UIntPtr)num5)].m_nextLane;
                        num6++;
                    }
                }
                else
                {
                    int num7 = 0;
                    while (num7 < info.m_lanes.Length && num5 != 0u)
                    {
                        instance.m_lanes.m_buffer[(int)((UIntPtr)num5)].RenderInstance(cameraInfo, segmentID, num5, info.m_lanes[num7], startFlags, endFlags, startColor, endColor, startAngle2, endAngle2, invert2, layerMask, objectIndex, objectIndex2, ref data, ref num4);
                        num5 = instance.m_lanes.m_buffer[(int)((UIntPtr)num5)].m_nextLane;
                        num7++;
                    }
                }
            }
        }

    }
}
