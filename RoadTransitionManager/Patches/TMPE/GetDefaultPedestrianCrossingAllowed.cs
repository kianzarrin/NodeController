namespace RoadTransitionManager.Patches.TMPE {
    using System.Reflection;
    using TrafficManager.Manager.Impl;
    using CSUtil.Commons;
    using RoadTransitionManager;
    using RoadTransitionManager.Util;
    using HarmonyLib;
    using ColossalFramework;

    [HarmonyPatch]
    public static class GetDefaultPedestrianCrossingAllowed {
        public static MethodBase TargetMethod() {
            return typeof(JunctionRestrictionsManager).
                GetMethod(nameof(JunctionRestrictionsManager.GetDefaultPedestrianCrossingAllowed));
        }

        public static bool Prefix(ushort segmentId, bool startNode, ref bool __result) {
            ushort nodeID = startNode ? segmentId.ToSegment().m_startNode : segmentId.ToSegment().m_endNode;
            NodeData data = NodeManager.Instance.buffer[nodeID];


            // TODO move to TMPE
            //if(data == null && nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Transition)) {
            //    __result = false;
            //    return false;
            //}

            return PrefixUtils.HandleTernaryBool(
                data?.GetDefaultPedestrianCrossingAllowed(),
                ref __result);

        }
    }
}