using KianCommons;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using JetBrains.Annotations;
using KianCommons.Patches;

namespace NodeController.Patches {

    [UsedImplicitly]
    [HarmonyPatch2(typeof(NetNode), typeof(RefreshJunctionData))]
    static class RefreshJunctionData1Patch
    {
        // non-DC
        delegate void RefreshJunctionData(ushort nodeID, int segmentIndex, ushort nodeSegment, Vector3 centerPos, ref uint instanceIndex, ref RenderManager.Instance data);

        [UsedImplicitly]
        static void Postfix(ushort nodeID, ref RenderManager.Instance data, ref Vector3 centerPos)
        {
            if(NodeManager.Instance.buffer[nodeID] is not NodeData blendData) return;

            centerPos = blendData.GetPosition(); // fix center pos.

            if(blendData.ShouldRenderCenteralCrossingTexture()) 
                data.m_dataVector1.w = 0.01f; // puts crossings in the center.
            
            if(blendData.NodeType == NodeTypeT.Stretch) {
                ushort segmentID = nodeID.ToNode().GetSegment(data.m_dataInt0 & 7);
                var invert = segmentID.ToSegment().m_flags.IsFlagSet(NetSegment.Flags.Invert);
                var startNode = NetUtil.IsStartNode(segmentId: segmentID, nodeId: nodeID);
                bool turnAround = (startNode == !invert);
                if (turnAround) {
                    // for segments it works like this:
                    // 1- data.m_dataVector0.x *= -1 (can't do this for nodes)
                    // 2- data.m_dataVector0.y *= -1 (can do this for nodes)

                    // 1- data.m_dataVector0.x *= -1 does not work for node shader. so we do the equivalent of swapping left/right matrices:
                    Helpers.Swap(ref data.m_dataMatrix0, ref data.m_dataMatrix1);
                    Helpers.Swap(ref data.m_extraData.m_dataMatrix2, ref data.m_extraData.m_dataMatrix3);

                    // 2- 
                    data.m_dataVector0.y *= -1;
                }
            }
        }
    }
}