namespace NodeController {
    using HarmonyLib;
    using CSUtil.Commons;
    using KianCommons;
    using TrafficManager.API.Manager;
    using TrafficManager.API.Traffic.Enums;
    using NodeController.Util;

    [HarmonyPatch(typeof(RoadBaseAI))]
    [HarmonyPatch(nameof(RoadBaseAI.UpdateNodeFlags))]
    static class UpdateNodeFlags {
        static ITrafficLightManager TL => TrafficManager.API.Implementations.ManagerFactory.TrafficLightManager;
        static void Postfix(ref NetNode data) {
            if (data.CountSegments() != 2)return;
            
            ushort nodeID = data.GetID();
            NodeData nodeData = NodeManager.Instance.buffer[nodeID];

            if (nodeData == null) return;

            if (nodeData.FirstTimeTrafficLight) {
                TMPEUtils.TryEnableTL(nodeID);
                nodeData.FirstTimeTrafficLight = false;
            } else if (nodeData.CanHaveTrafficLights(out _) == TernaryBool.False) {
                data.m_flags &= ~NetNode.Flags.TrafficLights;
            }
        }
    }
}
