namespace NodeController.Patches._NetManager
{
    using System;
    using System.Reflection;
    using HarmonyLib;
    using ColossalFramework;
    using NodeController.LifeCycle;

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

        public static MoveItSegmentData UpgradingSegmentData;
        static FieldInfo f_upgrading = AccessTools.DeclaredField( typeof(NetTool), "m_upgrading");
        public static bool m_upgrading => (bool)f_upgrading.GetValue(Singleton<NetTool>.instance);

        public static void Prefix(ushort segment)
        {
            if (UpgradingSegmentData != null) {
                KianCommons.Log.Error("Unexpected UpgradingSegmentData != null");
                UpgradingSegmentData = null;
            }
            if (m_upgrading) {
                UpgradingSegmentData = MoveItIntegration.CopySegment(segment);
            }
            SegmentEndManager.Instance.SetAt(segmentID: segment, true, value: null);
            SegmentEndManager.Instance.SetAt(segmentID: segment, false, value: null);
        }
    }
}