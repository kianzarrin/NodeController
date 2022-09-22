namespace NodeController.Patches.NetLanePatches {
    using HarmonyLib;
    using KianCommons;
    using KianCommons.Patches;
    using System;
    using System.Reflection;
    using UnityEngine;

    [HarmonyPatch]
    static class ANCalculatePropPosPatch {
        static MethodBase TargetMethod() =>
            Type.GetType("AdaptiveRoads.Data.NetworkExtensions.PropRenderData, AdaptiveRoads", throwOnError:false)?.
            GetMethod("CalculatePropPos");

        static bool Prepare() => TargetMethod() != null;

        static void Prefix( ref Vector3 __result,
            Vector3 pos, Vector3 tan, float t,
            ushort nodeId, ushort startSegmentId, ushort endSegmentId,
            bool isCatenary) {
            var start = SegmentEndManager.Instance.GetAt(startSegmentId, nodeID: nodeId);
            var end = SegmentEndManager.Instance.GetAt(endSegmentId, nodeID: nodeId);

            float stretchStart = start?.Stretch ?? 0;
            float stretchEnd = end?.Stretch ?? 0;
            float stretch = Mathf.Lerp(stretchStart, stretchEnd, t);
            stretch = 1 + stretch * 0.01f; // convert delta-percent to ratio
            pos.x *= stretch;

            float embankStart = start?.EmbankmentPercent ?? 0;
            float embankEnd = start?.EmbankmentPercent ?? 0;
            float embankment = Mathf.Lerp(embankStart, embankEnd, t);
            embankment *= 0.01f; //convert percent to ratio.
            float deltaY = pos.x * embankment;

            const bool reverse = false; // AN tracks are always forward.
            if (reverse)
                pos.y += deltaY;
            else
                pos.y -= deltaY;
            __result = pos;
        }
    }
}
