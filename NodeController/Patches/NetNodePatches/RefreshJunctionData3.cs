namespace NodeController.Patches {
    using HarmonyLib;
    using System.Reflection;
    using UnityEngine;
    using static KianCommons.Patches.TranspilerUtils;

    [HarmonyPatch]
    static class RefreshJunctionData3 {
        delegate void RefreshJunctionData(ushort nodeID, int segmentIndex, ushort nodeSegment, Vector3 centerPos, ref uint instanceIndex, ref RenderManager.Instance data); static MethodBase TargetMethod() => DeclaredMethod<RefreshJunctionData>(typeof(NetNode));
        static void Prefix(ushort nodeID, ref Vector3 centerPos) {
            if (NodeManager.Instance.buffer[nodeID] is NodeData data)
                centerPos = data.GetPosition();
        }
    }
}