namespace NodeController.Patches.VehicleSuperElevation {
    using System.Reflection;
    using UnityEngine;
    using KianCommons;
    using ColossalFramework;
    using static KianCommons.Patches.TranspilerUtils;

    public static class SuperElevationCommons {
        delegate void SimulationStepDelegate(
            ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData,
            ushort leaderID, ref Vehicle leaderData, int lodPhysics);

        public static MethodBase TargetMethod<T>() =>
            DeclaredMethod<SimulationStepDelegate>(typeof(T), "SimulationStep");

        public static MethodBase TargetTMPEMethod<T>() =>
            DeclaredMethod<SimulationStepDelegate>(typeof(T), "CustomSimulationStep");


        static PathUnit[] pathUnitBuffer => Singleton<PathManager>.instance.m_pathUnits.m_buffer;

        public static void Postfix(ref Vehicle vehicleData, ref Vehicle.Frame frameData) {
            if (!vehicleData.GetCurrentPathPos(out var pathPos))
                return;

            float se = GetCurrentSE(pathPos, vehicleData.m_lastPathOffset*(1f/255f));
            var rot = Quaternion.Euler(0, 0f, se);
            frameData.m_rotation *= rot;
        }

        internal static bool GetCurrentPathPos(this ref Vehicle vehicleData, out PathUnit.Position pathPos) {
            byte pathIndex = vehicleData.m_pathPositionIndex;
            if (pathIndex == 255) pathIndex = 0;
            return pathUnitBuffer[vehicleData.m_path].GetPosition(pathIndex >> 1, out pathPos);
        }

        internal static NetInfo.Lane GetLaneInfo(this ref PathUnit.Position pathPos) =>
            pathPos.m_segment.ToSegment().Info.m_lanes[pathPos.m_lane];

        internal static float GetCurrentSE(PathUnit.Position pathPos, float offset) {
            // bezier is always from start to end node regardless of direction.
            SegmentEndData segStart = SegmentEndManager.Instance.GetAt(pathPos.m_segment, true);
            SegmentEndData segEnd = SegmentEndManager.Instance.GetAt(pathPos.m_segment, false);
            float startSE = segStart == null ? 0f : segStart.SuperElevationDeg;
            float endSE = segEnd == null ? 0f : -segEnd.SuperElevationDeg;
            float se = startSE * (1-offset) + endSE * offset;
           

            bool invert = pathPos.m_segment.ToSegment().m_flags.IsFlagSet(NetSegment.Flags.Invert);
            bool backward = pathPos.GetLaneInfo().m_finalDirection == NetInfo.Direction.Backward;
            if (invert^backward) se = -se;
            return se;
        }

    }
}
