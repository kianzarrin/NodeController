namespace NodeController.Patches {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using HarmonyLib;

    [HarmonyPatch(typeof(BuildingManager),"SimulationStepImpl")]
    internal static class BuilidingManger_SimulationStep_Patch {
        // must be read/write from simulation thread.
        internal static HashSet<ushort> FixPillarNodeIDs = new();
        static void Prefix() {
            foreach(var nodeID in FixPillarNodeIDs) {
                NodeData.FixPillar(nodeID);
            }
            FixPillarNodeIDs.Clear();
        }
    }
}
