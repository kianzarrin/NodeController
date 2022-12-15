#if DEBUG
namespace NodeController.Patches; 
using ColossalFramework;
using HarmonyLib;
using KianCommons;
using UnityEngine.Networking.Types;

[HarmonyPatch(typeof(NetNode), nameof(NetNode.UpdateBuilding))]
class UpdateBuilding {
    /// <summary>in case another mod updated node building without performing a full update</summary>
    static void Postfix(ushort nodeID) {
        BuilidingManger_SimulationStep_Patch.FixPillarNodeIDs.Add(nodeID);
    }
}
#endif