namespace NodeController.Patches._NetManager; 
using NodeController.LifeCycle;
using HarmonyLib;

[HarmonyPatch(typeof(NetManager.Data), nameof(NetManager.Data.AfterDeserialize))]
static class AfterDeserialize {
    static void Prefix() => SerializableDataExtension.BeforeNetMangerAfterDeserialize();
}
