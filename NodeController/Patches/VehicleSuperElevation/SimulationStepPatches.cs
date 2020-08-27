namespace NodeController.Patches.VehicleSuperElevation {
    using HarmonyLib;
    using System.Reflection;
    using ColossalFramework;
    using TrafficManager.Custom.AI;
    using KianCommons;

    [HarmonyPatch]
    public static class CarAI_SimulationStepPatch {
        static MethodBase TargetMethod() => SuperElevationCommons.TargetMethod<CarAI>();
        static PathUnit[] pathUnitBuffer => Singleton<PathManager>.instance.m_pathUnits.m_buffer;
        static void Postfix(ref Vehicle vehicleData, ref Vehicle.Frame frameData) =>
            SuperElevationCommons.Postfix(ref vehicleData, ref frameData);
    }
    [HarmonyPatch]
    public static class CarTrailerAI_SimulationStepPatch {
        static MethodBase TargetMethod() => SuperElevationCommons.TargetMethod<CarTrailerAI>();
        static PathUnit[] pathUnitBuffer => Singleton<PathManager>.instance.m_pathUnits.m_buffer;
        static void Postfix(ref Vehicle vehicleData, ref Vehicle.Frame frameData) {
            Log.DebugWait("CarTrailerAI_SimulationStepPatch.Postfix() called",1);
            SuperElevationCommons.Postfix(ref vehicleData, ref frameData);

        }
    }
    [HarmonyPatch]
    public static class TrainAI_SimulationStepPatch {
        static MethodBase TargetMethod() => SuperElevationCommons.TargetTMPEMethod<CustomTrainAI>();
        static PathUnit[] pathUnitBuffer => Singleton<PathManager>.instance.m_pathUnits.m_buffer;
        static void Postfix(ref Vehicle vehicleData, ref Vehicle.Frame frameData) =>
            SuperElevationCommons.Postfix(ref vehicleData, ref frameData);
    }
    [HarmonyPatch]
    public static class TramBaseAI_SimulationStepPatch {
        static MethodBase TargetMethod() => SuperElevationCommons.TargetTMPEMethod<CustomTramBaseAI>();
        static PathUnit[] pathUnitBuffer => Singleton<PathManager>.instance.m_pathUnits.m_buffer;
        static void Postfix(ref Vehicle vehicleData, ref Vehicle.Frame frameData) =>
            SuperElevationCommons.Postfix(ref vehicleData, ref frameData);
    }


}

