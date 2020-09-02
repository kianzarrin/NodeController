namespace NodeController.Patches.VehicleSuperElevation {
    using HarmonyLib;
    using System.Reflection;
    using ColossalFramework;
    using TrafficManager.Custom.AI;
    using KianCommons;
    using UnityEngine;
    using static SuperElevationCommons;
    using System.IO;
    using System.Reflection.Emit;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    //public override void SimulationStep(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData,
    //                                    ushort leaderID, ref Vehicle leaderData, int lodPhysics)
    [HarmonyPatch]
    public static class CarAI_SimulationStepPatch {
        internal static MethodBase TargetMethod() => TargetMethod<CarAI>();

        internal static IEnumerable<CodeInstruction> Transpiler(ILGenerator il, IEnumerable<CodeInstruction> instructions) =>
            OnRotationUpdatedTranspiler(instructions, TargetMethod() as MethodInfo);

        internal static void Postfix(ref Vehicle vehicleData, ref Vehicle.Frame frameData) {
            if (!RotationUpdated) return;
            RotationUpdated = false;
            SuperElevationCommons.Postfix(ref vehicleData, ref frameData);
        }
    }

    [HarmonyPatch]
    public static class CarTrailerAI_SimulationStepPatch {
        internal static MethodBase TargetMethod() => TargetMethod<CarTrailerAI>();

        internal static IEnumerable<CodeInstruction> Transpiler(ILGenerator il, IEnumerable<CodeInstruction> instructions) =>
            OnRotationUpdatedTranspiler(instructions, TargetMethod() as MethodInfo);

        static Vehicle[] VehicleBuffer = VehicleManager.instance.m_vehicles.m_buffer;
        internal static void Postfix( ref Vehicle vehicleData, ref Vehicle.Frame frameData) {
            if (vehicleData.Info.m_leanMultiplier < 0)
                return; // motor cycle.

            if (!RotationUpdated) {
                //Log.Debug("CarTrailerAI_SimulationStepPatch rotation was not updated! leadID=" + vehicleData.m_leadingVehicle);
                return;
            }
            RotationUpdated = false;

            ref Vehicle leadingVehicle = ref VehicleBuffer[vehicleData.m_leadingVehicle];
            //Vehicle.Frame lastFrameData = leadingVehicle.GetLastFrameData();
            VehicleInfo leadningInfo = leadingVehicle.Info;
            if (!leadingVehicle.GetCurrentPathPos(out var pathPos)) return;
            uint laneID = PathManager.GetLaneID(pathPos);

            // Calculate trailer lane offset based on how far the trailer is from the car its attached to.
            bool inverted = leadingVehicle.m_flags.IsFlagSet(Vehicle.Flags.Inverted);
            float deltaPos = inverted ? leadningInfo.m_attachOffsetBack : leadningInfo.m_attachOffsetFront;
            float deltaOffset = deltaPos / laneID.ToLane().m_length;
            float offset = leadingVehicle.m_lastPathOffset * (1f / 255f) - deltaOffset;
            offset = Mathf.Clamp(offset, 0, 1);

            float se = GetCurrentSE(pathPos, offset, ref vehicleData);
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

