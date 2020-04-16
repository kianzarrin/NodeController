namespace BlendRoadManager.Patches
{
	using System.Reflection;
	using JetBrains.Annotations;
	using UnityEngine;
	using HarmonyLib;
	using Util;

	[UsedImplicitly]
	[HarmonyPatch]
	static class CalculateCornerPatch
	{
		[UsedImplicitly]
		static MethodBase TargetMethod()
		{
			return typeof(NetSegment).GetMethod(
				nameof(NetSegment.CalculateCorner),
				BindingFlags.Public | BindingFlags.Static) ??
				throw new System.Exception("CalculateCornerPatch Could not find target method.");
		}

		struct StateData
		{
			public float Offset;
			public bool Changed;
		}

		[UsedImplicitly]
		static void Prefix(ushort startNodeID, NetInfo info, ref StateData __state)
		{
			NodeBlendData blendData = NodeBlendManager.Instance.buffer[startNodeID];
			if (__state.Changed = blendData != null && blendData.CornerOffset != blendData.DefaultCornerOffset)
			{
				__state.Offset = info.m_minCornerOffset;
				info.m_minCornerOffset = blendData.CornerOffset;
			}
		}

		[UsedImplicitly]
		static void Postfix(NetInfo info, ref StateData __state)
		{
			if (__state.Changed)
			{
				info.m_minCornerOffset = __state.Offset;
			}
		}
	}
}
