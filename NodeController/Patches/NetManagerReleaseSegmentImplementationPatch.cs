namespace NodeController.Patches
{
    using System;
    using System.Reflection;
    using HarmonyLib;

    [HarmonyPatch]
    public static class NetManagerReleaseSegmentImplementationPatch
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.DeclaredMethod(
                typeof(NetManager),
                "ReleaseSegmentImplementation",
                new[] {typeof(ushort), typeof(NetSegment).MakeByRefType(), typeof(bool) },
                null);
        }

        public static void Prefix(ushort segment)
        {
            SegmentEndManager.Instance.SetAt(segmentID: segment, true, value: null);
            SegmentEndManager.Instance.SetAt(segmentID: segment, false, value: null);
        }
    }
}