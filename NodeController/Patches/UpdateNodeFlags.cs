using ColossalFramework;
using HarmonyLib;
using TrafficManager.Manager.Impl;

namespace NodeController {
    using Util;
    [HarmonyPatch(typeof(RoadBaseAI))]
    [HarmonyPatch(nameof(RoadBaseAI.UpdateNodeFlags))]
    class UpdateNodeFlags {
        // Update traffic lights
        // for a node with 2 segments if NodeBlendManager says input node ID needs traffic lights then
        // check if NetAI of the node or any of its segments allows traffic lights.
        // if that is the case then set traffic-lights flag.
        // trains cannot have traffic-lights flag.
        // Credits to Crossings mod.

        static void Postfix(ref NetNode data) {
            if (data.CountSegments() != 2)return;
            
            ushort nodeID = NetUtil.GetID(data);
            NodeData nodeData = NodeManager.Instance.buffer[nodeID];

            if (nodeData == null || !nodeData.WantsTrafficLight()) return;

            if(TrafficLightManager.Instance.CanEnableTrafficLight(nodeID, ref data, out var res)) {
                TrafficLightManager.Instance.SetTrafficLight(nodeID, true, ref data);
            }
        }
    }
}
