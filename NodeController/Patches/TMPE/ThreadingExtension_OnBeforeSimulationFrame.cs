namespace NodeController.Patches.TMPE {
    using HarmonyLib;
    using KianCommons;
    using System.Reflection;
    using TrafficManager;
    using KianCommons.Plugins;
    using System;
    using System.Linq;

    // TODO: remove this when TMPE is updated.
    static class ThreadingExtension_OnBeforeSimulationFrame {
        static MethodBase TargetMethod() {
            return typeof(ThreadingExtension).GetMethod(
                nameof(ThreadingExtension.OnBeforeSimulationFrame),
                throwOnError: true);
        }

        static bool Prepare() {
            var tmpe = PluginUtil.GetTrafficManager();
            return
                tmpe != null
                && tmpe.isEnabled &&
                tmpe.GetMainAssembly().VersionOf() < new Version("11.5.3");
        }

        static void Prefix(ref bool ___firstFrame) => ___firstFrame = false;

        public static Exception Cleanup(Exception ex) {
            if(ex !=null)
                Log.Info($"(this message is harmless) Suppressing for new TMPE: {ex.Message}");
            return null;
        }
    }
}