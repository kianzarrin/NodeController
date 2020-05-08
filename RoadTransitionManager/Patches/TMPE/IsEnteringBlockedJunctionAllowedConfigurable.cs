namespace NodeController.Patches.TMPE {
    using System.Reflection;
    using TrafficManager.Manager.Impl;
    using CSUtil.Commons;
    using NodeController;
    using NodeController.Util;
    using HarmonyLib;
    using ColossalFramework;

    [HarmonyPatch]
    public static class IsEnteringBlockedJunctionAllowedConfigurable {
        public static MethodBase TargetMethod() {
            return typeof(JunctionRestrictionsManager).
                GetMethod(nameof(JunctionRestrictionsManager.IsEnteringBlockedJunctionAllowedConfigurable));
        }

        public static bool Prefix(ushort segmentId, bool startNode, ref bool __result) {
            ushort nodeID = startNode ? segmentId.ToSegment().m_startNode : segmentId.ToSegment().m_endNode;
            var data = NodeManager.Instance.buffer[nodeID];
            if (data == null) {
                var flags = nodeID.ToNode().m_flags;
                bool oneway = flags.IsFlagSet(NetNode.Flags.OneWayIn) & flags.IsFlagSet(NetNode.Flags.OneWayOut);
                if (oneway & !segmentId.ToSegment().Info.m_hasPedestrianLanes) {
                    __result = false;
                    return false;
                }
            }


            return PrefixUtils.HandleTernaryBool(
                data?.IsEnteringBlockedJunctionAllowedConfigurable(),
                ref __result);

        }
    }
}