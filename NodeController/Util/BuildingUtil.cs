namespace NodeController.Util {
    using ColossalFramework;
    using KianCommons;
    using System;
    using System.Threading;
    using UnityEngine;

    public static class BuildingUtil {
        internal static ref Building ToBuilding(this ushort buildingId) =>
            ref BuildingManager.instance.m_buildings.m_buffer[buildingId];

        internal static bool IsValid(this ref Building building, ushort buildingId)=>
            buildingId != 0 && (building.m_flags & (Building.Flags.Created | Building.Flags.Deleted)) == Building.Flags.Created;

        public static void RelocatePillar(ushort buildingId, Vector3 position, float angle) {
            if (buildingId == 0) return;
            if (!Helpers.InSimulationThread()) {
                SimulationManager.instance.AddAction(() => RelocatePillar(buildingId, position, angle));
                return;
            }

            ref Building building = ref buildingId.ToBuilding();
            var delta = building.m_position - position;
            if (delta.sqrMagnitude < (0.001 * 0.001f)) return; // same place.

            RemoveFromGrid(buildingId, ref building);
            building.m_position = position;
            building.m_angle = (angle + Mathf.PI * 2) % (Mathf.PI * 2);
            AddToGrid(buildingId, ref building);

            if (building.Info != null) {
                building.CalculateBuilding(buildingId);
                BuildingManager.instance.UpdateBuildingRenderer(buildingId, false);
            } else {
                BuildingManager.instance.UpdateBuilding(buildingId);
            }
        }

        private static void AddToGrid(ushort buildingID, ref Building data) {
            int num = Mathf.Clamp((int)(data.m_position.x / 64f + 135f), 0, 269);
            int num2 = Mathf.Clamp((int)(data.m_position.z / 64f + 135f), 0, 269);
            int num3 = num2 * 270 + num;
            while (!Monitor.TryEnter(BuildingManager.instance.m_buildingGrid, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
            }
            try {
                buildingID.ToBuilding().m_nextGridBuilding = BuildingManager.instance.m_buildingGrid[num3];
                BuildingManager.instance.m_buildingGrid[num3] = buildingID;
            } finally {
                Monitor.Exit(BuildingManager.instance.m_buildingGrid);
            }
        }

        private static void RemoveFromGrid(ushort buildingID, ref Building data) {
            BuildingManager buildingManager = BuildingManager.instance;

            BuildingInfo info = data.Info;
            int num = Mathf.Clamp((int)(data.m_position.x / 64f + 135f), 0, 269);
            int num2 = Mathf.Clamp((int)(data.m_position.z / 64f + 135f), 0, 269);
            int num3 = num2 * 270 + num;
            while (!Monitor.TryEnter(buildingManager.m_buildingGrid, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
            }
            try {
                ushort num4 = 0;
                ushort num5 = buildingManager.m_buildingGrid[num3];
                int num6 = 0;
                while (num5 != 0) {
                    if (num5 == buildingID) {
                        if (num4 == 0) {
                            buildingManager.m_buildingGrid[num3] = data.m_nextGridBuilding;
                        } else {
                            num4.ToBuilding().m_nextGridBuilding = data.m_nextGridBuilding;
                        }
                        break;
                    }
                    num4 = num5;
                    num5 = num5.ToBuilding().m_nextGridBuilding;
                    if (++num6 > 49152) {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                        break;
                    }
                }
                data.m_nextGridBuilding = 0;
            } finally {
                Monitor.Exit(buildingManager.m_buildingGrid);
            }
            if (info != null) {
                Singleton<RenderManager>.instance.UpdateGroup(num * 45 / 270, num2 * 45 / 270, info.m_prefabDataLayer);
            }
        }
    }
}
