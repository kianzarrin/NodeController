namespace NodeController.Patches.VehicleSuperElevation {
    using System.Reflection;
    using UnityEngine;
    using KianCommons;
    using ColossalFramework;
    using static KianCommons.Patches.TranspilerUtils;
    using TrafficManager.Custom.AI;

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
            var rot = Quaternion.Euler(0, 0f, -se); // minus to rotate to right
            frameData.m_rotation *= rot;
        }

        static bool GetCurrentPathPos(this ref Vehicle vehicleData, out PathUnit.Position pathPos) {
            byte pathIndex = vehicleData.m_pathPositionIndex;
            if (pathIndex == 255) pathIndex = 0;
            return pathUnitBuffer[vehicleData.m_path].GetPosition(pathIndex >> 1, out pathPos);
        }

        static float GetCurrentSE(PathUnit.Position pathPos, float offset) {
            uint laneID = PathManager.GetLaneID(pathPos);
            //laneID.ToLane().GetClosestPosition(pos, out pos, out var offset);

            NetUtil.GetLaneTailAndHeadNodes(laneID, pathPos.m_lane, tail: out ushort tail, head: out ushort head);
            SegmentEndData segTail = SegmentEndManager.Instance.GetAt(pathPos.m_segment, tail);
            SegmentEndData segHead = SegmentEndManager.Instance.GetAt(pathPos.m_segment, head);
            float tailSE = segTail == null ? 0f : segTail.SuperElevationDeg;
            float headSE = segHead == null ? 0f : -segHead.SuperElevationDeg;

            float se = tailSE * offset + headSE * (1 - offset);
            bool invert = pathPos.m_segment.ToSegment().m_flags.IsFlagSet(NetSegment.Flags.Invert);
            if (!invert) se = -se;
            return se;
        }

    }
}
