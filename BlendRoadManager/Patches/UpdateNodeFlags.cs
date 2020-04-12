using ColossalFramework;
using HarmonyLib;

namespace BlendRoadManager
{
	using Util;
	[HarmonyPatch(typeof(RoadBaseAI))]
	[HarmonyPatch(nameof(RoadBaseAI.UpdateNodeFlags))]
	class UpdateNodeFlags
	{
		// Update traffic lights
		static void Postfix(ref RoadBaseAI __instance, ref NetNode data)
		{
			if (data.CountSegments() != 2)
				return;
			ushort nodeID = NetUtil.GetID(data);
			NodeBlendData blendData = NodeBlendManager.Instance.buffer[nodeID];
			if (blendData == null)
				return;
			if (!blendData.NeedsTrafficLight())
				return;

			bool wantTrafficLights = __instance.WantTrafficLights();
			if (!wantTrafficLights)
			{
				foreach (var segmentID in NetUtil.GetSegmentsCoroutine(nodeID)) { 
					NetInfo info = segmentID.ToSegment().Info;
					if (info != null)
					{
						if (info.m_vehicleTypes.IsFlagSet(VehicleInfo.VehicleType.Train))
						{
							// No crossings allowed where there's a railway intersecting
							return;
						}

						if (info.m_netAI.WantTrafficLights())
						{
							wantTrafficLights = true;
							break;
						}
					}
				}
			}

			if (wantTrafficLights)
			{
				data.m_flags |= NetNode.Flags.TrafficLights;
			}
		}
	}
}
