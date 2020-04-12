using System;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;
using static BlendRoadManager.Util.NetUtil;

// Token: 0x02000467 RID: 1127
public partial struct NetNode2
{
	// Token: 0x060034D0 RID: 13520 RVA: 0x0023E078 File Offset: 0x0023C478
	public static void CalculateNode(ref NetNode This, ushort nodeID)
	{
		if (This.m_flags == NetNode.Flags.None)
		{
			return;
		}
		NetManager netMan = Singleton<NetManager>.instance;
		Vector3 DirFirst = Vector3.zero;
		int iSegment = 0;
		int ConnectCount = 0;
		bool hasSegments = false;
		bool canBeMiddle = false;
		bool bCompatibleButNodeMiddle = false;
		bool isAsymForward = false;
		bool isAsymBackward = false;
		bool needsJunctionFlag = false;
		bool hasCurvedSegment = false;
		bool hasStraightSegment = false;
		bool bCompatibleAndStart2End = false;
		bool allConnectedSegmentsAreFlat = true;
		bool CanModify = true;
		bool bHasDetailMapping = Singleton<TerrainManager>.instance.HasDetailMapping(This.m_position);
		NetInfo prevInfo = null;
		int prev_backwardVehicleLaneCount = 0;
		int prev_m_forwardVehicleLaneCount = 0;
		NetInfo infoNode = null;
		float num5 = -1E+07f;
		for (int i = 0; i < 8; i++)
		{
			ushort segmentID = This.GetSegment(i);
			if (segmentID != 0)
			{
				NetInfo infoSegment = netMan.m_segments.m_buffer[segmentID].Info;
				float nodeInfoPriority = infoSegment.m_netAI.GetNodeInfoPriority(segmentID, ref netMan.m_segments.m_buffer[segmentID]);
				if (nodeInfoPriority > num5)
				{
					infoSegment = infoSegment;
					num5 = nodeInfoPriority;
				}
			}
		}
		if (infoNode == null)
		{
			infoNode = This.Info;
		}
		if (infoNode != This.Info)
		{
			This.Info = infoNode;
			Singleton<NetManager>.instance.UpdateNodeColors(nodeID);
			if (!infoNode.m_canDisable)
			{
				This.m_flags &= ~NetNode.Flags.Disabled;
			}
		}
		bool bStartNodeFirst = false;
		for (int j = 0; j < 8; j++)
		{
			ushort segmentID = This.GetSegment(j);
			if (segmentID != 0)
			{
				iSegment++;
				ushort startNodeID = netMan.m_segments.m_buffer[segmentID].m_startNode;
				ushort endNodeID = netMan.m_segments.m_buffer[segmentID].m_endNode;
				Vector3 startDirection = netMan.m_segments.m_buffer[segmentID].m_startDirection;
				Vector3 endDirection = netMan.m_segments.m_buffer[segmentID].m_endDirection;
				bool bStartNode = nodeID == startNodeID;
				Vector3 currentDir = (!bStartNode) ? endDirection : startDirection;
				NetInfo infoSegment = netMan.m_segments.m_buffer[segmentID].Info;
				ItemClass connectionClass = infoSegment.GetConnectionClass();
				if (!infoSegment.m_netAI.CanModify())
				{
					CanModify = false;
				}
				int backwardVehicleLaneCount;
				int forwardVehicleLaneCount;
				if (bStartNode == ((netMan.m_segments.m_buffer[segmentID].m_flags & NetSegment.Flags.Invert) != NetSegment.Flags.None))
				{
					backwardVehicleLaneCount = infoSegment.m_backwardVehicleLaneCount;
					forwardVehicleLaneCount = infoSegment.m_forwardVehicleLaneCount;
				}
				else
				{
					backwardVehicleLaneCount = infoSegment.m_forwardVehicleLaneCount;
					forwardVehicleLaneCount = infoSegment.m_backwardVehicleLaneCount;
				}
				for (int k = j + 1; k < 8; k++)
				{
					ushort segmentID2 = This.GetSegment(k);
					if (segmentID2 != 0)
					{
						NetInfo infoSegment2 = netMan.m_segments.m_buffer[segmentID2].Info;
						ItemClass connectionClass2 = infoSegment2.GetConnectionClass();
						if (connectionClass2.m_service == connectionClass.m_service || (infoSegment2.m_nodeConnectGroups & infoSegment.m_connectGroup) != NetInfo.ConnectGroup.None || (infoSegment.m_nodeConnectGroups & infoSegment2.m_connectGroup) != NetInfo.ConnectGroup.None)
						{
							bool bStartNode2 = nodeID == netMan.m_segments.m_buffer[segmentID2].m_startNode;
							Vector3 dir2 = (!bStartNode2) ? netMan.m_segments.m_buffer[segmentID2].m_endDirection : netMan.m_segments.m_buffer[segmentID2].m_startDirection;
							float dot2 = currentDir.x * dir2.x + currentDir.z * dir2.z;
							float turnThreshold = 0.01f - Mathf.Min(infoSegment.m_maxTurnAngleCos, infoSegment2.m_maxTurnAngleCos);
							if (dot2 < turnThreshold)
							{
								if ((infoSegment.m_requireDirectRenderers && (infoSegment.m_nodeConnectGroups == NetInfo.ConnectGroup.None || (infoSegment.m_nodeConnectGroups & infoSegment2.m_connectGroup) != NetInfo.ConnectGroup.None)) || (infoSegment2.m_requireDirectRenderers && (infoSegment2.m_nodeConnectGroups == NetInfo.ConnectGroup.None || (infoSegment2.m_nodeConnectGroups & infoSegment.m_connectGroup) != NetInfo.ConnectGroup.None)))
								{
									ConnectCount++;
								}
							}
							else
							{
								needsJunctionFlag = true;
							}
						}
						else
						{
							needsJunctionFlag = true;
						}
					}
				}

				if (netMan.m_nodes.m_buffer[startNodeID].m_elevation != netMan.m_nodes.m_buffer[endNodeID].m_elevation)
					allConnectedSegmentsAreFlat = false;

				Vector3 startPos = netMan.m_nodes.m_buffer[startNodeID].m_position;
				Vector3 endPos = netMan.m_nodes.m_buffer[endNodeID].m_position;
				if (bStartNode)
					bHasDetailMapping = (bHasDetailMapping && Singleton<TerrainManager>.instance.HasDetailMapping(endPos));
				else
					bHasDetailMapping = (bHasDetailMapping && Singleton<TerrainManager>.instance.HasDetailMapping(startPos));

				if (NetSegment.IsStraight(startPos, startDirection, endPos, endDirection))
				{
					hasStraightSegment = true;
				}
				else
				{
					hasCurvedSegment = true;
				}

				if (iSegment == 1)
				{
					bStartNodeFirst = bStartNode;
					DirFirst = currentDir;
					hasSegments = true;
				}
				else if (iSegment == 2 && infoSegment.IsCombatible(prevInfo) && infoSegment.IsCombatible(infoNode) && (backwardVehicleLaneCount != 0) == (prev_m_forwardVehicleLaneCount != 0) && (forwardVehicleLaneCount != 0) == (prev_backwardVehicleLaneCount != 0))
				{
					float dot = DirFirst.x * currentDir.x + DirFirst.z * currentDir.z;
					if (backwardVehicleLaneCount != prev_m_forwardVehicleLaneCount || forwardVehicleLaneCount != prev_backwardVehicleLaneCount)
					{
						if (backwardVehicleLaneCount > forwardVehicleLaneCount)
						{
							isAsymForward = true;
						}
						else
						{
							isAsymBackward = true;
						}
						bCompatibleButNodeMiddle = true;
					}
					else if (dot < -0.999f) // straight.
					{
						canBeMiddle = true;
					}
					else
					{
						bCompatibleButNodeMiddle = true;
					}
					bCompatibleAndStart2End = (bStartNode != bStartNodeFirst);
				}
				else
				{
					needsJunctionFlag = true;
				}
				prevInfo = infoSegment;
				prev_backwardVehicleLaneCount = backwardVehicleLaneCount;
				prev_m_forwardVehicleLaneCount = forwardVehicleLaneCount;
			}
		}
		if (!infoNode.m_enableMiddleNodes && canBeMiddle)
		{
			bCompatibleButNodeMiddle = true;
		}
		if (!infoNode.m_enableBendingNodes && bCompatibleButNodeMiddle)
		{
			needsJunctionFlag = true;
		}
		if (infoNode.m_requireContinuous && (This.m_flags & NetNode.Flags.Untouchable) != NetNode.Flags.None)
		{
			needsJunctionFlag = true;
		}
		if (infoNode.m_requireContinuous && !bCompatibleAndStart2End && (canBeMiddle || bCompatibleButNodeMiddle))
		{
			needsJunctionFlag = true;
		}
		NetNode.Flags flags = This.m_flags & ~(NetNode.Flags.End | NetNode.Flags.Middle | NetNode.Flags.Bend | NetNode.Flags.Junction | NetNode.Flags.Moveable | NetNode.Flags.AsymForward | NetNode.Flags.AsymBackward);
		if ((flags & NetNode.Flags.Outside) != NetNode.Flags.None)
		{
			This.m_flags = flags;
		}
		else if (needsJunctionFlag)
		{
			This.m_flags = (flags | NetNode.Flags.Junction);
		}
		else if (bCompatibleButNodeMiddle)
		{
			if (isAsymForward)
			{
				flags |= NetNode.Flags.AsymForward;
			}
			if (isAsymBackward)
			{
				flags |= NetNode.Flags.AsymBackward;
			}
			This.m_flags = (flags | NetNode.Flags.Bend);
		}
		else if (canBeMiddle)
		{
			if ((!hasCurvedSegment || !hasStraightSegment) && (This.m_flags & (NetNode.Flags.Untouchable | NetNode.Flags.Double)) == NetNode.Flags.None && allConnectedSegmentsAreFlat && CanModify)
			{
				flags |= NetNode.Flags.Moveable;
			}
			This.m_flags = (flags | NetNode.Flags.Middle);
		}
		else if (hasSegments)
		{
			if ((This.m_flags & NetNode.Flags.Untouchable) == NetNode.Flags.None && allConnectedSegmentsAreFlat && CanModify && infoNode.m_enableMiddleNodes)
			{
				flags |= NetNode.Flags.Moveable;
			}
			This.m_flags = (flags | NetNode.Flags.End);
		}
		else
		{
			This.m_flags = flags;
		}
		This.m_heightOffset = (byte)((!bHasDetailMapping && infoNode.m_requireSurfaceMaps) ? 64 : 0);
		This.m_connectCount = (byte)ConnectCount;
		BuildingInfo newBuilding;
		float heightOffset;
		infoNode.m_netAI.GetNodeBuilding(nodeID, ref This, out newBuilding, out heightOffset);
		This.UpdateBuilding(nodeID, newBuilding, heightOffset);
	}

	public static bool BlendJunction(ushort nodeID)
	{
		NetManager netManager = Singleton<NetManager>.instance;
		if ((netManager.m_nodes.m_buffer[(int)nodeID].m_flags & (NetNode.Flags.Middle | NetNode.Flags.Bend)) != NetNode.Flags.None)
		{
			return true;
		}
		if ((netManager.m_nodes.m_buffer[(int)nodeID].m_flags & NetNode.Flags.Junction) != NetNode.Flags.None)
		{
			bool bHasForward_Prev = false;
			bool bHasBackward_Prev = false;
			int segmentCount = 0;
			for (int i = 0; i < 8; i++)
			{
				ushort segmentID = nodeID.ToNode().GetSegment(i);
				if (segmentID != 0)
				{
					if (++segmentCount >= 3)
					{
						return false;
					}
					NetInfo info_segment =segmentID.ToSegment().Info;
					if (!info_segment.m_enableMiddleNodes || info_segment.m_requireContinuous)
					{
						return false;
					}
					bool bHasForward;
					bool bHasBackward;
					bool bStartNode = segmentID.ToSegment().m_startNode == nodeID;
					bool bInvert = !segmentID.ToSegment().m_flags.IsFlagSet(NetSegment.Flags.Invert);
					if (bStartNode == bInvert)
					{
						bHasForward = info_segment.m_hasForwardVehicleLanes;
						bHasBackward = info_segment.m_hasBackwardVehicleLanes;
					}
					else
					{
						bHasForward = info_segment.m_hasBackwardVehicleLanes;
						bHasBackward = info_segment.m_hasForwardVehicleLanes;
					}
					if (segmentCount == 2)
					{
						if (bHasForward != bHasBackward_Prev || bHasBackward != bHasForward_Prev)
						{
							return false;
						}
					}
					else
					{
						bHasForward_Prev = bHasForward;
						bHasBackward_Prev = bHasBackward;
					}
				}
			}
			return segmentCount == 2;
		}
		return false;
	}

	// NetNode
	// Token: 0x060034C6 RID: 13510 RVA: 0x0023D1EC File Offset: 0x0023B5EC
	private static void RefreshJunctionData(ref NetNode This, ushort nodeID, int segmentIndex, ushort SegmentID, Vector3 centerPos, ref uint instanceIndex, ref RenderManager.Instance data)
	{
		Vector3 cornerPos_right =Vector3.zero, cornerDir_right = Vector3.zero, cornerPos_left = Vector3.zero, cornerDir_left = Vector3.zero,
			cornerPosA_right = Vector3.zero, cornerDirA_right = Vector3.zero, cornerPosA_left = Vector3.zero, cornerDirA_left = Vector3.zero,
			cornerPosB_right = Vector3.zero, cornerDirB_right = Vector3.zero, cornerPosB_left = Vector3.zero, cornerDirB_left = Vector3.zero;



				NetManager instance = Singleton<NetManager>.instance;
		data.m_position = This.m_position;
		data.m_rotation = Quaternion.identity;
		data.m_initialized = true;
		NetSegment segment = SegmentID.ToSegment();
		NetInfo info = segment.Info;
		float vscale = info.m_netAI.GetVScale();
		ItemClass connectionClass = info.GetConnectionClass();
		Vector3 dir = (nodeID != segment.m_startNode) ? segment.m_endDirection : segment.m_startDirection;
		float dot_A = -4f;
		float dot_B = -4f;
		ushort segmentID_A = 0;
		ushort segmentID_B = 0;
		for (int i = 0; i < 8; i++)
		{
			ushort segmentID2 = This.GetSegment(i);
			if (segmentID2 != 0 && segmentID2 != SegmentID)
			{
				NetInfo info2 = instance.m_segments.m_buffer[(int)segmentID2].Info;
				ItemClass connectionClass2 = info2.GetConnectionClass();
				if (connectionClass.m_service == connectionClass2.m_service)
				{
					NetSegment segment2 = segmentID2.ToSegment();
					bool bStartNode2 = nodeID != segment2.m_startNode;
					Vector3 dir2 = !bStartNode2 ? segment2.m_endDirection : segment2.m_startDirection;
					float dot = dir.x * dir2.x + dir.z * dir2.z;
					float determinent = dir2.z * dir.x - dir2.x * dir.z;
					if (determinent < 0)
					{
						if (dot > dot_A)
						{
							dot_A = dot;
							segmentID_A = segmentID2;
						}
						dot = -2f - dot;
						if (dot > dot_B)
						{
							dot_B = dot;
							segmentID_B = segmentID2;
						}
					}
					else
					{
						if (dot > dot_B)
						{
							dot_B = dot;
							segmentID_B = segmentID2;
						}
						dot = -2f - dot;
						if (dot > dot_A)
						{
							dot_A = dot;
							segmentID_A = segmentID2;
						}
					}
				}
			}
		}
		bool start = segment.m_startNode == nodeID;
		segment.CalculateCorner(SegmentID, true, start, false, out cornerPos_right, out cornerDir_right, out _);
		segment.CalculateCorner(SegmentID, true, start, true, out cornerPos_left, out  cornerDir_left, out _);
		if (segmentID_A != 0 && segmentID_B != 0)
		{
			float pavementRatio_avgA = info.m_pavementWidth / info.m_halfWidth * 0.5f;
			float averageWidthA = 1f;
			if (segmentID_A != 0)
			{
				NetSegment segment_A = instance.m_segments.m_buffer[(int)segmentID_A];
				NetInfo infoA = segment_A.Info;
				start = (segment_A.m_startNode == nodeID);
				segment_A.CalculateCorner(segmentID_A, true, start, true, out cornerPosA_right, out cornerDirA_right, out _);
				segment_A.CalculateCorner(segmentID_A, true, start, false, out cornerPosA_left, out cornerDirA_left, out _);
				float pavementRatioA = infoA.m_pavementWidth / infoA.m_halfWidth * 0.5f;
				pavementRatio_avgA = (pavementRatio_avgA + pavementRatioA) * 0.5f;
				averageWidthA = 2f * info.m_halfWidth / (info.m_halfWidth + infoA.m_halfWidth);
			}
			float pavementRatio_avgB = info.m_pavementWidth / info.m_halfWidth * 0.5f;
			float averageWithB = 1f;
			if (segmentID_B != 0)
			{
				NetSegment segment_B = instance.m_segments.m_buffer[(int)segmentID_B];
				NetInfo infoB = segment_B.Info;
				start = (segment_B.m_startNode == nodeID);
				segment_B.CalculateCorner(segmentID_B, true, start, true, out cornerPosB_right, out cornerDirB_right, out _);
				segment_B.CalculateCorner(segmentID_B, true, start, false, out cornerPosB_left, out cornerDirB_left, out _);
				float pavementRatioB = infoB.m_pavementWidth / infoB.m_halfWidth * 0.5f;
				pavementRatio_avgB = (pavementRatio_avgB + pavementRatioB) * 0.5f;
				averageWithB = 2f * info.m_halfWidth / (info.m_halfWidth + infoB.m_halfWidth);
			}

			NetSegment.CalculateMiddlePoints(cornerPos_right, -cornerDir_right, cornerPosA_right, -cornerDirA_right, true, true, out var cpoint2_Aright, out var cpoint3_Aright);
			NetSegment.CalculateMiddlePoints(cornerPos_left, -cornerDir_left, cornerPosA_left, -cornerDirA_left, true, true, out var cpoint2_Aleft, out var cpoint3_Aleft);
			NetSegment.CalculateMiddlePoints(cornerPos_right, -cornerDir_right, cornerPosB_right, -cornerDirB_right, true, true, out var cpoint2_Bright, out var cpoint3_Bright);
			NetSegment.CalculateMiddlePoints(cornerPos_left, -cornerDir_left, cornerPosB_left, -cornerDirB_left, true, true, out var cpoint2_Bleft, out var cpoint3_Bleft);
			
			data.m_dataMatrix0 = NetSegment.CalculateControlMatrix(cornerPos_right, cpoint2_Aright, cpoint3_Aright, cornerPosA_right, cornerPos_right, cpoint2_Aright, cpoint3_Aright, cornerPosA_right, This.m_position, vscale);
			data.m_extraData.m_dataMatrix2 = NetSegment.CalculateControlMatrix(cornerPos_left, cpoint2_Aleft, cpoint3_Aleft, cornerPosA_left, cornerPos_left, cpoint2_Aleft, cpoint3_Aleft, cornerPosA_left, This.m_position, vscale);
			data.m_extraData.m_dataMatrix3 = NetSegment.CalculateControlMatrix(cornerPos_right, cpoint2_Bright, cpoint3_Bright, cornerPosB_right, cornerPos_right, cpoint2_Bright, cpoint3_Bright, cornerPosB_right, This.m_position, vscale);
			data.m_dataMatrix1 = NetSegment.CalculateControlMatrix(cornerPos_left, cpoint2_Bleft, cpoint3_Bleft, cornerPosB_left, cornerPos_left, cpoint2_Bleft, cpoint3_Bleft, cornerPosB_left, This.m_position, vscale);
			data.m_dataVector0 = new Vector4(0.5f / info.m_halfWidth, 1f / info.m_segmentLength, 0.5f - info.m_pavementWidth / info.m_halfWidth * 0.5f, info.m_pavementWidth / info.m_halfWidth * 0.5f);
			data.m_dataVector1 = centerPos - data.m_position;
			data.m_dataVector1.w = (data.m_dataMatrix0.m31 + data.m_dataMatrix0.m32 + data.m_extraData.m_dataMatrix2.m31 + data.m_extraData.m_dataMatrix2.m32 + data.m_extraData.m_dataMatrix3.m31 + data.m_extraData.m_dataMatrix3.m32 + data.m_dataMatrix1.m31 + data.m_dataMatrix1.m32) * 0.125f;
			data.m_dataVector2 = new Vector4(pavementRatio_avgA, averageWidthA, pavementRatio_avgB, averageWithB);
		}
		else
		{
			centerPos.x = (cornerPos_right.x + cornerPos_left.x) * 0.5f;
			centerPos.z = (cornerPos_right.z + cornerPos_left.z) * 0.5f;
			var cornerPos_left_prev = cornerPos_left;
			var cornerPos_right_prev = cornerPos_right;
			cornerDirB_right = cornerDir_left;
			cornerDirB_left = cornerDir_right;
			float d = info.m_netAI.GetEndRadius() * 1.33333337f;
			Vector3 vector13 = cornerPos_right - cornerDir_right * d;
			Vector3 vector14 = cornerPos_left_prev - cornerDirB_right * d;
			Vector3 vector15 = cornerPos_left - cornerDir_left * d;
			Vector3 vector16 = cornerPos_right_prev - cornerDirB_left * d;
			Vector3 vector17 = cornerPos_right + cornerDir_right * d;
			Vector3 vector18 = cornerPos_left_prev + cornerDirB_right * d;
			Vector3 vector19 = cornerPos_left + cornerDir_left * d;
			Vector3 vector20 = cornerPos_right_prev + cornerDirB_left * d;
			data.m_dataMatrix0 = NetSegment.CalculateControlMatrix(cornerPos_right, vector13, vector14, cornerPos_left_prev, cornerPos_right, vector13, vector14, cornerPos_left_prev, This.m_position, vscale);
			data.m_extraData.m_dataMatrix2 = NetSegment.CalculateControlMatrix(cornerPos_left, vector19, vector20, cornerPos_right_prev, cornerPos_left, vector19, vector20, cornerPos_right_prev, This.m_position, vscale);
			data.m_extraData.m_dataMatrix3 = NetSegment.CalculateControlMatrix(cornerPos_right, vector17, vector18, cornerPos_left_prev, cornerPos_right, vector17, vector18, cornerPos_left_prev, This.m_position, vscale);
			data.m_dataMatrix1 = NetSegment.CalculateControlMatrix(cornerPos_left, vector15, vector16, cornerPos_right_prev, cornerPos_left, vector15, vector16, cornerPos_right_prev, This.m_position, vscale);
			data.m_dataMatrix0.SetRow(3, data.m_dataMatrix0.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
			data.m_extraData.m_dataMatrix2.SetRow(3, data.m_extraData.m_dataMatrix2.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
			data.m_extraData.m_dataMatrix3.SetRow(3, data.m_extraData.m_dataMatrix3.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
			data.m_dataMatrix1.SetRow(3, data.m_dataMatrix1.GetRow(3) + new Vector4(0.2f, 0.2f, 0.2f, 0.2f));
			data.m_dataVector0 = new Vector4(0.5f / info.m_halfWidth, 1f / info.m_segmentLength, 0.5f - info.m_pavementWidth / info.m_halfWidth * 0.5f, info.m_pavementWidth / info.m_halfWidth * 0.5f);
			data.m_dataVector1 = centerPos - data.m_position;
			data.m_dataVector1.w = (data.m_dataMatrix0.m31 + data.m_dataMatrix0.m32 + data.m_extraData.m_dataMatrix2.m31 + data.m_extraData.m_dataMatrix2.m32 + data.m_extraData.m_dataMatrix3.m31 + data.m_extraData.m_dataMatrix3.m32 + data.m_dataMatrix1.m31 + data.m_dataMatrix1.m32) * 0.125f;
			data.m_dataVector2 = new Vector4(info.m_pavementWidth / info.m_halfWidth * 0.5f, 1f, info.m_pavementWidth / info.m_halfWidth * 0.5f, 1f);
		}
		Vector4 colorLocation;
		Vector4 vector21;
		if (NetNode.BlendJunction(nodeID))
		{
			colorLocation = RenderManager.GetColorLocation(86016u + (uint)nodeID);
			vector21 = colorLocation;
		}
		else
		{
			colorLocation = RenderManager.GetColorLocation((uint)(49152 + SegmentID));
			vector21 = RenderManager.GetColorLocation(86016u + (uint)nodeID);
		}
		data.m_extraData.m_dataVector4 = new Vector4(colorLocation.x, colorLocation.y, vector21.x, vector21.y);
		data.m_dataInt0 = segmentIndex;
		data.m_dataColor0 = info.m_color;
		data.m_dataColor0.a = 0f;
		data.m_dataFloat0 = Singleton<WeatherManager>.instance.GetWindSpeed(data.m_position);
		if (info.m_requireSurfaceMaps)
		{
			Singleton<TerrainManager>.instance.GetSurfaceMapping(data.m_position, out data.m_dataTexture0, out data.m_dataTexture1, out data.m_dataVector3);
		}
		instanceIndex = (uint)data.m_nextInstance;
	}

}
