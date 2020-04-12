using BlendRoadManager;
using BlendRoadManager.Util;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using JetBrains.Annotations;

namespace BlendRoadManager.Patches
{
    [UsedImplicitly]
    [HarmonyPatch]
    class RefreshJunctionData
    {
        [UsedImplicitly]
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(
            typeof(NetNode),
            "RefreshJunctionData",
            new Type[] {
                typeof(ushort),
                typeof(int),
                typeof(ushort),
                typeof(Vector3),
                typeof(uint).MakeByRefType(),
                typeof(RenderManager.Instance).MakeByRefType()
            });
        }

        [UsedImplicitly]
        static void Postfix(ref NetNode __instance, ref RenderManager.Instance data)
        {
            ushort nodeID = NetUtil.GetID(__instance);
            NodeBlendData blendData = NodeBlendManager.Instance.buffer[nodeID];
            if (blendData == null)
                return;

            switch (blendData.type)
            {
                case BlendType.NoBlending:
                    throw new Exception("UnreachableCode");
                    break;
                case BlendType.Crossing:
                    data.m_dataVector1.w = 0.01f;
                    break;
                case BlendType.Sharp:
                    data.m_dataVector1.w = 0.04f;
                    break;
                case BlendType.LaneBasedWidth:
                    // TODO implement
                    break;
                case BlendType.UTurn:
                    data.m_dataVector1.w = 8f;
                    break;
                case BlendType.CustomWidth:
                    data.m_dataVector1.w = blendData.customWidth;
                    break;
            }
        }
    }
}