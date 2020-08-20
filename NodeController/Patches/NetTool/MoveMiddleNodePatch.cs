namespace NodeController.Patches._NetTool {
    using HarmonyLib;
    using KianCommons;
    using NodeController;
    using NodeController.LifeCycle;
    using static KianCommons.HelpersExtensions;

    [HarmonyPatch(typeof(global::NetTool), "MoveMiddleNode")]
    public static class MoveMiddleNodePatch {
        internal static MoveItSegmentData SegmentData { get; private set; }
        internal static bool CopyData => SegmentData != null;

        public static void Prefix(ref ushort node) // TODO remove ref when in lates harmony.
        {
            if (!InSimulationThread()) return;
            ushort nodeID = node;
            ushort segmentID = NetUtil.GetFirstSegment(nodeID);
            SegmentData = MoveItIntegration.CopySegment(segmentID);
        }

        public static void Postfix() {
            if (!InSimulationThread()) return;
            SegmentData = null;
        }
    }
}
