namespace NodeController.LifeCycle
{
    using NodeController.Tool;
    using NodeController.Util;
    using static KianCommons.HelpersExtensions;
    using KianCommons;
    using System.Net.Mail;

    public static class LifeCycle
    {
        public static void Load()
        {
            HelpersExtensions.VERBOSE = false;
            Log.Info("LifeCycle.Load() called");
            CSURUtil.Init();
            HarmonyExtension.InstallHarmony();
            NodeControllerTool.Create();
            NodeManager.Instance.OnLoad();
            SegmentEndManager.Instance.OnLoad();
        }

        public static void Release()
        {
            Log.Info("LifeCycle.Release() called");
            HarmonyExtension.UninstallHarmony();
            NodeControllerTool.Remove();
        }
    }
}
