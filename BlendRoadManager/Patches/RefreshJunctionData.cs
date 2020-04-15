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

            if(blendData.textureType == TextureType.Corssing)
            {
                // puts crossings in the center.
                data.m_dataVector1.w = 0.01f;
            }
        }
    }
}