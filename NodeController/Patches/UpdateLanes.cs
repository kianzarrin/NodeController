namespace NodeController {
    using HarmonyLib;
    using KianCommons;
    using NodeController.Manager;

    [HarmonyPatch(typeof(RoadBaseAI))]
    [HarmonyPatch(nameof(RoadBaseAI.UpdateLanes))]
    [HarmonyAfter("de.viathinksoft.tmpe", "me.tmpe")]
    [HarmonyPriority(Priority.Low)]
    class UpdateLanes {
        static void Postfix(ushort segmentID) {
            LaneCache.Instance.UpdateLanes(segmentID);
        }
    }
}
