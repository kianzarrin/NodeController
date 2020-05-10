namespace NodeController.Patches.HideCrosswalksMod {
    using System.Reflection;
    using NodeController;
    using CSUtil.Commons;
    using HarmonyLib;

    [HarmonyPatch]
    public static class ShouldHideCrossing {
        public static MethodBase TargetMethod() {
            return typeof(HideCrosswalks.Patches.CalculateMaterialCommons).
                GetMethod(nameof(HideCrosswalks.Patches.CalculateMaterialCommons.ShouldHideCrossing));
        }

        public static bool Prefix(ushort nodeID, ref bool __result) {
            var data = NodeManager.Instance.buffer[nodeID];
            return PrefixUtils.HandleTernaryBool(
                data?.ShouldHideCrossingTexture(),
                ref __result);
        }
    }
}