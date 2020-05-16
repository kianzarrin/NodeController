namespace NodeController.LifeCycle
{
    using NodeController.Tool;
    using NodeController.Util;

    public static class LifeCycle
    {
        public static void Load()
        {
            Log.Info("LifeCycle.Load() called");
            PluginUtil.Init();
            HarmonyExtension.InstallHarmony();
            NodeControllerTool.Create();
            NodeManager.Instance.OnLoad();
        }

        public static void Release()
        {
            Log.Info("LifeCycle.Release() called");
            HarmonyExtension.UninstallHarmony();
            NodeControllerTool.Remove();
        }
    }
}
