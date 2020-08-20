namespace NodeController.Patches._NetTool {
    using HarmonyLib;
    using NodeController.LifeCycle;
    using static KianCommons.HelpersExtensions;

    [HarmonyPatch(typeof(global::NetTool), "SplitSegment")]
    public class SplitSegmentPatch
    {
        internal static MoveItSegmentData SegmentData { get; private set; }
        internal static bool CopyData { get; private set; }

        public static void Prefix(ushort segment)
        {
            if (!InSimulationThread()) return;
            SegmentData = MoveItIntegration.CopySegment(segment);
            CopyData = true;
        }

        public static void Postfix()
        {
            if (!InSimulationThread()) return;
            if (CopyData) {
                SegmentData = null;
                CopyData = false;
            }

        }
    }
}
