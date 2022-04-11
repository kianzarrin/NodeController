namespace NodeController.Patches {
    using HarmonyLib;
    using KianCommons;
    [HarmonyPatch(typeof(NetSegment), nameof(NetSegment.CalculateSegment))]
    class CalculateSegment {
        static void Postfix(ushort segmentID) {
            if (!NetUtil.IsSegmentValid(segmentID)) return;
            SegmentEndData segStart = SegmentEndManager.Instance.GetAt(segmentID, true);
            SegmentEndData segEnd = SegmentEndManager.Instance.GetAt(segmentID, false);
            segStart?.OnAfterCalculate();
            segEnd?.OnAfterCalculate();
            segStart?.NodeData.ShiftPilar();
            segEnd?.NodeData.ShiftPilar();
        }
    }
}
