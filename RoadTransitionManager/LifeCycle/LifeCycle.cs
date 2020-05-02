namespace RoadTransitionManager.LifeCycle
{
    using RoadTransitionManager.Tool;
    using RoadTransitionManager.Util;

    public static class LifeCycle
    {
        public static bool bFirstFrame;
        public static void Load()
        {
            Log.Info("LifeCycle.Load() called");
            bFirstFrame = true;
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
