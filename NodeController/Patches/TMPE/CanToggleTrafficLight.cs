namespace NodeController.Patches.TMPE {
    using System.Reflection;
    using TrafficManager.Manager.Impl;
    using NodeController;
    using HarmonyLib;
    using TrafficManager.API.Traffic.Enums;
    using KianCommons.Patches;
    using KianCommons;
    using KianCommons.Plugins;

    [HarmonyPatch]
    static class CanToggleTrafficLightPatch {
        public delegate bool CanToggleTrafficLight(ushort nodeId, bool flag, ref NetNode node, out ToggleTrafficLightError reason);
        public static MethodBase TargetMethod() =>
            typeof(TrafficLightManager).GetMethod<CanToggleTrafficLight>(throwOnError: true);

        static bool Prepare() => PluginUtil.GetTrafficManager().IsActive();

        public static bool Prefix(ref bool __result, ushort nodeId, ref ToggleTrafficLightError reason) {
            var nodeData = NodeManager.Instance.buffer[nodeId];
            return PrefixUtils.HandleTernaryBool(
                nodeData?.CanHaveTrafficLights(out reason),
                ref __result);
        }
    }
}