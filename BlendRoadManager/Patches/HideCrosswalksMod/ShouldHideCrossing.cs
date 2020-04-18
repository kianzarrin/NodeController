namespace BlendRoadManager.Patches.HideCrosswalksMod {
    using System.Reflection;
    using BlendRoadManager;
    using Harmony;

    [HarmonyPatch]
    public static class ShouldHideCrossing {
        public static MethodBase TargetMethod() {
            return typeof(HideCrosswalks.Patches.CalculateMaterialCommons).
                GetMethod(nameof(HideCrosswalks.Patches.CalculateMaterialCommons.ShouldHideCrossing));
        }

        public static bool Prefix(ushort nodeID, ref bool __result) {
            var data = NodeBlendManager.Instance.buffer[nodeID];
            if (data != null){
                if (!data.CanHideCrossingTexture()) {
                    __result = false;
                    return false;
                }
            }
            return true;
        }
    }
}