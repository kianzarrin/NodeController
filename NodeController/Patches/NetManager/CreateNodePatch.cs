namespace NodeController.Patches._NetManager {
    using HarmonyLib;
    using NodeController;
    using NodeController.LifeCycle;
    using KianCommons;
    using static KianCommons.HelpersExtensions;
    using NodeController.Patches._NetTool;

    //public bool CreateNode(out ushort node, ref Randomizer randomizer, NetInfo info, Vector3 position, uint buildIndex)
    [HarmonyPatch(typeof(NetManager), nameof(NetManager.CreateNode))]
    public static class CreateNodePatch {
        public static void Postfix(ref ushort node, bool __result) {
            if (!__result || !InSimulationThread()) return;
            if (MoveMiddleNodePatch.CopyData) {
                MoveItIntegration.PasteNode(node, MoveMiddleNodePatch.NodeData, null);
            }
        }
    }
}