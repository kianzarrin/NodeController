namespace NodeController.Patches.TMPE {
    using System.Reflection;
    using HarmonyLib;
    using TrafficManager;
    using System;

    [HarmonyPatch(typeof(ThreadingExtension), nameof(ThreadingExtension.OnBeforeSimulationFrame))]
    public static class ThreadingExtension_OnBeforeSimulationFrame {
        static FieldInfo field_firstFrame =
            AccessTools.Field(typeof(ThreadingExtension), "firstFrame");
            // ?? throw new Exception("could not find ThreadingExtension.firstFrame");
        public static void Prefix(ThreadingExtension __instance) {
            //Util.Log.Debug("ThreadingExtension_OnBeforeSimulationFrame.Prefix called");
            field_firstFrame?.SetValue(__instance, false);
        }
    }
}