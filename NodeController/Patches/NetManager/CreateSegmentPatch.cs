namespace NodeController.Patches._NetManager
{
    using HarmonyLib;
    using NodeController;
    using NodeController.LifeCycle;
    using KianCommons;
    using static KianCommons.HelpersExtensions;
    using NodeController.Patches._NetTool;
    using System.Collections.Generic;

    // TODO check compat with ParallelRoadTool
    [HarmonyPatch(typeof(global::NetManager), nameof(NetManager.CreateSegment))]
    public static class CreateSegmentPatch
    {
        public static void Postfix(ref ushort segment, ushort startNode, ushort endNode, bool __result)
        {
            if (!__result || !InSimulationThread()) return;

            if (MoveMiddleNodePatch.CopyData) {
                Log.Debug("Moving middle node: copying data to newly created segment. " +
                    $"newSegmentID={segment} data={MoveMiddleNodePatch.SegmentData}");
                MoveItIntegration.PasteSegment(segment, MoveMiddleNodePatch.SegmentData, null);
            } else if (SplitSegmentPatch.CopyData) {
                var segmentData = SplitSegmentPatch.SegmentData;
                Log.Debug("Spliting segment: copying data to newly created segment. " +
                    $"newSegmentID={segment} data={segmentData}");

                // start and end might change ... but node ids are the same.
                var data = segmentData.Start;
                if (data != null ) {
                    ushort nodeID = data.NodeID;
                    if (nodeID == startNode || nodeID == endNode) {
                        MoveItIntegration.PasteSegmentEnd(data, nodeID, segment);
                    }
                }

                data = segmentData.End;
                if (data != null) {
                    ushort nodeID = data.NodeID;
                    if (nodeID == startNode || nodeID == endNode) {
                        MoveItIntegration.PasteSegmentEnd(data, nodeID, segment);
                    }
                }
            } else if(ReleaseSegmentImplementationPatch.UpgradingSegmentData != null){
                if (!ReleaseSegmentImplementationPatch.m_upgrading) {
                    Log.Error("Unexpected UpgradingSegmentData != null but m_upgrading == false ");
                } else {
                    MoveItIntegration.PasteSegment(
                        segment, ReleaseSegmentImplementationPatch.UpgradingSegmentData, null);
                }
                ReleaseSegmentImplementationPatch.UpgradingSegmentData = null; // consume
            } else {
                SegmentEndManager.Instance.SetAt(segment, true, null);
                SegmentEndManager.Instance.SetAt(segment, false, null);
            }
        }
    }
}
