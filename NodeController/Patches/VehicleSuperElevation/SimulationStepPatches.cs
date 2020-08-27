namespace NodeController.Patches.VehicleSuperElevation {
    using HarmonyLib;
    using System.Reflection;
    using ColossalFramework;
    using TrafficManager.Custom.AI;
    using KianCommons;
    using UnityEngine;
    using static SuperElevationCommons;

    //public override void SimulationStep(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData,
    //                                    ushort leaderID, ref Vehicle leaderData, int lodPhysics)
    [HarmonyPatch]
    public static class CarAI_SimulationStepPatch {
        internal static MethodBase TargetMethod() => TargetMethod<CarAI>();
        internal static void Postfix(ref Vehicle vehicleData, ref Vehicle.Frame frameData) =>
            SuperElevationCommons.Postfix(ref vehicleData, ref frameData);
    }

    [HarmonyPatch]
    public static class CarTrailerAI_SimulationStepPatch {
        internal static MethodBase TargetMethod() => TargetMethod<CarTrailerAI>();
        static Vehicle[] VehicleBuffer = VehicleManager.instance.m_vehicles.m_buffer;

        internal static void Postfix( ref Vehicle vehicleData, ref Vehicle.Frame frameData) {
            ref Vehicle leadingVehicle = ref VehicleBuffer[vehicleData.m_leadingVehicle];
            //Vehicle.Frame lastFrameData = leadingVehicle.GetLastFrameData();
            VehicleInfo leadningInfo = leadingVehicle.Info;
            if (!leadingVehicle.GetCurrentPathPos(out var pathPos)) return;

            bool inverted = leadingVehicle.m_flags.IsFlagSet(Vehicle.Flags.Inverted);
            float deltaPos = inverted ? leadningInfo.m_attachOffsetBack : leadningInfo.m_attachOffsetFront;
            float deltaOffset = deltaPos / pathPos.m_segment.ToSegment().m_averageLength;
            float leading_offset = leadingVehicle.m_lastPathOffset * (1f / 255f);
            float offset = Mathf.Clamp(leading_offset - deltaOffset, 0, 1);

            float se = GetCurrentSE(pathPos, offset);
            var rot = Quaternion.Euler(0, 0f, se);
            frameData.m_rotation *= rot;
        }
    }

    [HarmonyPatch]
    public static class TrainAI_SimulationStepPatch {
        internal static MethodBase TargetMethod() => TargetTMPEMethod<CustomTrainAI>();
        internal static void Postfix(ref Vehicle vehicleData, ref Vehicle.Frame frameData) =>
            SuperElevationCommons.Postfix(ref vehicleData, ref frameData);
    }

    [HarmonyPatch]
    public static class TramBaseAI_SimulationStepPatch {
        internal static MethodBase TargetMethod() => TargetTMPEMethod<CustomTramBaseAI>();
        internal static void Postfix(ref Vehicle vehicleData, ref Vehicle.Frame frameData) =>
            SuperElevationCommons.Postfix(ref vehicleData, ref frameData);
    }
}

