namespace BlendRoadManager.LifeCycle
{
    using BlendRoadManager.Tool;
    using BlendRoadManager.Util;

    public static class LifeCycle
    {
        public static void Load()
        {
            HarmonyExtension.InstallHarmony();
            BlendRoadTool.Create();
            Log.Debug("LoadTool:Created kian tool.");
        }
        public static void Release()
        {
            HarmonyExtension.UninstallHarmony();
            BlendRoadTool.Remove();
            Log.Debug("LoadTool:Removed kian tool.");
        }
    }
}
