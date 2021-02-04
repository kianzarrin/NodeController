namespace NodeController.Patches.TMPE {
    using HarmonyLib;
    using KianCommons;
    using System.Reflection;
    using TrafficManager;

    // TODO: remove this when TMPE is updated.
    [HarmonyPatch(typeof(ThreadingExtension), nameof(ThreadingExtension.OnBeforeSimulationFrame))]
    static class ThreadingExtension_OnBeforeSimulationFrame {
        static bool Prepare() => PluginUtil.GetTrafficManager().IsActive();

        static FieldInfo field_firstFrame =>
            AccessTools.Field(typeof(ThreadingExtension), "firstFrame");
        public static void Prefix(ThreadingExtension __instance) {
            //Util.Log.Debug("ThreadingExtension_OnBeforeSimulationFrame.Prefix called");
            field_firstFrame?.SetValue(__instance, false);
        }
    }
}