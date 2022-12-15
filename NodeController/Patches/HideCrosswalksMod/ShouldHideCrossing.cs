namespace NodeController.Patches.HideCrosswalksMod {
    using System.Reflection;
    using NodeController;
    using KianCommons.Patches;
    using HarmonyLib;
    using KianCommons;
    using KianCommons.Plugins;
    using NodeController.Util;
    using HideCrosswalks.Patches;

    [HarmonyPatch]
    public static class ShouldHideCrossing {
        static bool Prepare() => HTCUtil.IsActive;

        public static MethodBase TargetMethod() {
            return HTCUtil.Type_CalculateMaterialCommons.
                GetMethod(nameof(CalculateMaterialCommons.ShouldHideCrossing), throwOnError: true);
        }

        public static bool Prefix(ushort nodeID, ushort segmentID, ref bool __result) {
            var data = SegmentEndManager.Instance.GetAt(
                segmentID: segmentID, nodeID: nodeID);
            return PrefixUtils.HandleTernaryBool(
                data?.ShouldHideCrossingTexture(),
                ref __result);
        }
    }
}