namespace NodeController.Util {
    using ColossalFramework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using UnityEngine;

    public static class BuildingUtil {
        private static ref Building ToBuilding(this ushort buildingId) =>
            ref BuildingManager.instance.m_buildings.m_buffer[buildingId];


        public static void RelocatePillar(ushort buildingId, Vector3 position, float angle) {
            if (!KianCommons.Helpers.InSimulationThread()) {
                SimulationManager.instance.AddAction(() => RelocatePillar(buildingId, position, angle));
            }

            ref Building building = ref buildingId.ToBuilding();
            if (buildingId != 0) {
                RemoveFromGrid(buildingId, ref building);
            }

            //BuildingInfo info = data.Info;
            //if (info.m_hasParkingSpaces != VehicleInfo.VehicleType.None)
            //{
            //    Log.Debug($"PARKING (RB)\n#{building}:{info.name}");
            //    BuildingManager.instance.UpdateParkingSpaces(building, ref data);
            //}

            building.m_position = position;
            building.m_angle = (angle + Mathf.PI * 2) % (Mathf.PI * 2);

            AddToGrid(buildingId, ref building);
            building.CalculateBuilding(buildingId);
            BuildingManager.instance.UpdateBuildingRenderer(buildingId, true);
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
