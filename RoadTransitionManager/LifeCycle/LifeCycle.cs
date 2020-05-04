namespace RoadTransitionManager.LifeCycle
{
    using RoadTransitionManager.Tool;
    using RoadTransitionManager.Util;

    public static class LifeCycle
    {
        public static void Load()
        {
            Log.Info("LifeCycle.Load() called");
            PluginUtil.Init();
            HarmonyExtension.InstallHarmony();
            NodeControllerTool.Create();
            NodeManager.Instance.OnLoad();
            SerializableDataExtension.LoadGlobalConfig();
        }

        public static void Release()
        {
            Log.Info("LifeCycle.Release() called");
            HarmonyExtension.UninstallHarmony();
            NodeControllerTool.Remove();
            SerializableDataExtension.SaveGlobalConfig();
        }
    }
}
