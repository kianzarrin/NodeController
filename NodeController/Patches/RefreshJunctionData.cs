using NodeController;
using NodeController.Util;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using JetBrains.Annotations;
using ColossalFramework;

namespace NodeController.Patches
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

        //public static Matrix4x4 HouseholderReflection(this Matrix4x4 matrix4X4, Vector3 planeNormal) {
        //    planeNormal.Normalize();
        //    Vector4 planeNormal4 = new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, 0);
        //    var a = MultiplyVectorsTransposed(planeNormal4, planeNormal4);

        //    //var a = Minus(Matrix4x4.identity, planeNormal4);
        //    //    Matrix4x4 householderMatrix = Matrix4x4.identity.Minus(
        //    //        MultiplyVectorsTransposed(planeNormal4, planeNormal4).MutiplyByNumber(2));
        //    //    return householderMatrix * matrix4X4;

        //    // c = -2*(planeNorma14 * planeNormal4');
        //    // householderMatrix = c * Matrix4x4.identity;
        //    //return householderMatrix * matrix4X4;
        //}

        [UsedImplicitly]
        static void Postfix(ref NetNode __instance, ref RenderManager.Instance data)
        {
            ushort nodeID = NetUtil.GetID(__instance);
            NodeData blendData = NodeManager.Instance.buffer[nodeID];
            if (blendData == null)
                return;

            if(blendData.ShouldRenderCenteralCrossingTexture())
            {
                // puts crossings in the center.
                data.m_dataVector1.w = 0.01f;
            }
#if false
            if(blendData.NodeType == NodeTypeT.Stretch) {
                // should data vectors be inverted?
                ushort segmentID = __instance.GetSegment(data.m_dataInt0 & 7);
                var invert = segmentID.ToSegment().m_flags.IsFlagSet(NetSegment.Flags.Invert);
                var startNode = NetUtil.IsStartNode(segmentId:segmentID, nodeId: nodeID);
                bool flip = startNode == !invert; // tested works.
                if (flip) {// flip across x axis
                    //data.m_dataVector0.x = -data.m_dataVector0.x;
                    data.m_dataVector0.z = -data.m_dataVector0.z;
                    data.m_dataVector0.y = -data.m_dataVector0.y;
                    //data.m_dataVector0.w = -data.m_dataVector0.w;

                    //data.m_dataVector2.x = -data.m_dataVector2.x;
                    data.m_dataVector2.z = -data.m_dataVector2.z;
                    data.m_dataVector2.y = -data.m_dataVector2.y;
                    //data.m_dataVector2.w = -data.m_dataVector2.w;
                     
                    //data.m_dataVector1.x = -data.m_dataVector1.x;
                    //data.m_dataVector1.z = -data.m_dataVector1.z;
                    //data.m_dataVector1.y = -data.m_dataVector1.y;
                    //data.m_dataVector1.w = -data.m_dataVector1.w;

                    //data.m_dataVector3.z = -data.m_dataVector3.z;
                    //data.m_dataVector3.y = -data.m_dataVector3.y;
                    //data.m_dataVector3.x = -data.m_dataVector3.x;
                    //data.m_dataVector3.w = -data.m_dataVector3.w;
                }
            }
#endif
        }
    }
}