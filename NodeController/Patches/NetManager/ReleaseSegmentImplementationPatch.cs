namespace NodeController.Patches._NetManager
{
    using System;
    using System.Reflection;
    using HarmonyLib;

    [HarmonyPatch]
    public static class ReleaseSegmentImplementationPatch
    {
        //private void ReleaseSegmentImplementation(ushort segment, ref NetSegment data, bool keepNodes)
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