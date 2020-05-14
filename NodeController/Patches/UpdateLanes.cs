using ColossalFramework;
using HarmonyLib;

namespace NodeController {
    using Util;
    [HarmonyPatch(typeof(RoadBaseAI))]
    [HarmonyPatch(nameof(RoadBaseAI.UpdateLanes))]
    [HarmonyBefore("de.viathinksoft.tmpe")]
    class UpdateLanes {
        public static bool AllFlagsAreForward(ushort segmentID, NetInfo.Direction dir) {
            NetLane.Flags flags = 0;
            foreach (var lane in NetUtil.GetLanesCoroutine(segmentID, direction:dir)) {
                flags |= (NetLane.Flags)lane.LaneID.ToLane().m_flags;
            }
            return (flags & NetLane.Flags.LeftForwardRight) == NetLane.Flags.Forward;
        }

        static void Postfix(ref RoadBaseAI __instance, ushort segmentID) {
            if (!NetUtil.IsSegmentValid(segmentID)) return;
            foreach (var dir in new[] { NetInfo.Direction.Forward, NetInfo.Direction.Backward }) {
                if (AllFlagsAreForward(segmentID, dir)) {
                    foreach (var lane in NetUtil.GetLanesCoroutine(segmentID, direction: dir)) {
                        NetLane.Flags flags = (NetLane.Flags)lane.LaneID.ToLane().m_flags;
                        flags = flags & ~NetLane.Flags.LeftForwardRight;
                        lane.LaneID.ToLane().m_flags = (ushort)flags;
                    }
                }
            }
        }
    }
}
