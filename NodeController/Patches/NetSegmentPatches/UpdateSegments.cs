namespace NodeController.Patches;
using HarmonyLib;
using KianCommons;
using System;
using NodeController.LifeCycle;

// not called in after deserialize
internal static class UpdateSegmentsCommons {
    internal static void Postfix(ushort segmentID, bool startNode) {
        try {
            if (!NetUtil.IsSegmentValid(segmentID)) return;
            SegmentEndData segStart = SegmentEndManager.Instance.GetAt(segmentID, startNode);
            segStart?.OnAfterCalculate();

            ref NetSegment segment = ref segmentID.ToSegment();
            ushort nodeID = segment.GetNode(startNode);
            BuilidingManger_SimulationStep_Patch.FixPillarNodeIDs.Add(nodeID);
        } catch (Exception ex) { ex.Log($"segment:{segmentID}"); }
    }
}


// also called in after deserialize
[HarmonyPatch(typeof(NetSegment), nameof(NetSegment.UpdateStartSegments))]
static class UpdateStartSegments {
    static void Postfix(ushort segmentID) => UpdateSegmentsCommons.Postfix(segmentID, true);
}

// also called in after deserialize
[HarmonyPatch(typeof(NetSegment), nameof(NetSegment.UpdateEndSegments))]
static class UpdateEndSegments {
    static void Postfix(ushort segmentID) => UpdateSegmentsCommons.Postfix(segmentID, false);
}
