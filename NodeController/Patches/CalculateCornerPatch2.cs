namespace NodeController.Patches {
    using KianCommons;
    using HarmonyLib;
    using JetBrains.Annotations;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using static KianCommons.Patches.TranspilerUtils;
    using NodeController.Util;
    using UnityEngine;

    [UsedImplicitly]
    [HarmonyPatch]
    static class CalculateCornerPatch2 {
        [UsedImplicitly]
        static MethodBase TargetMethod() {
    // public void CalculateCorner(ushort segmentID, bool heightOffset, bool start, bool leftSide,
    // out Vector3 cornerPos, out Vector3 cornerDirection, out bool smooth)
            return typeof(NetSegment).GetMethod(
                    nameof(NetSegment.CalculateCorner),
                    BindingFlags.Public | BindingFlags.Instance) ??
                    throw new System.Exception("CalculateCornerPatch Could not find target method.");
        }

        /// <param name="segmentID">segment to calculate corner</param>
        /// <param name="start">true for start node</param>
        /// <param name="leftSide">going away from the node</param>
        public static void Postfix(
            ushort segmentID, bool heightOffset, bool start, bool leftSide,
            ref Vector3 cornerPos, ref Vector3 cornerDirection) {
            var data = SegmentEndManager.Instance.GetAt(segmentID, start);
            if (data == null) return;

            leftSide = !leftSide; // left side now mean left when going toward the node.
            float deltaH = leftSide ? data.HLeft : data.HRight;
            cornerPos.y += deltaH;
        }



    }
}
