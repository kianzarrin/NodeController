using System;
using System.Reflection;
using HarmonyLib;

namespace NodeController.Patches
{
    [HarmonyPatch]
    public static class NetManagerReleaseNodeImplementationPatch
    {
        public static MethodBase TargetMethod()
        {
            // ReleaseNodeImplementation(ushort node, ref NetNode data)
            return typeof(global::NetManager).GetMethod(
                "ReleaseNodeImplementation",
                BindingFlags.NonPublic | BindingFlags.Instance,
                Type.DefaultBinder,
                new[] {
                typeof(ushort), typeof(global::NetNode).MakeByRefType(),
                }, null);
        }

        public static void Prefix(ushort node)
        {
            NodeManager.Instance.buffer[node] = null;
        }
    }
}