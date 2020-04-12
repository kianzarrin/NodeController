using BlendRoadManager.Tool;
using BlendRoadManager.Util;
using ICities;

namespace BlendRoadManager
{
    using ICities;
    using JetBrains.Annotations;
    using System;
    public class BlendRoadManagerMod : IUserMod
    {
        public static Version ModVersion => typeof(BlendRoadManagerMod).Assembly.GetName().Version;
        public static string VersionString => ModVersion.ToString(2);
        public string Name => "BlendRoad " + VersionString;
        public string Description => "gives you control of segment blend nodes";

        [UsedImplicitly]
        public void OnEnabled()
        {
            HarmonyExtension.InstallHarmony();
            if (HelpersExtensions.InGame)
                LifeCycle.Load();
        }

        [UsedImplicitly]
        public void OnDisabled()
        {
            HarmonyExtension.UninstallHarmony();
            LifeCycle.Release();
        }
    }
}

public static class LifeCycle
{
    public static void Load()
    {
        BlendRoadTool.Create();
        Log.Debug("LoadTool:Created kian tool.");
    }
    public static void Release()
    {
        BlendRoadTool.Remove();
        Log.Debug("LoadTool:Removed kian tool.");
    }
}

public class LoadingExtention : LoadingExtensionBase
{
    public override void OnLevelLoaded(LoadMode mode)
    {
        Log.Debug("LoadingExtention.OnLevelLoaded");
        if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame)
            LifeCycle.Load();
    }

    public override void OnLevelUnloading()
    {
        Log.Debug("LoadingExtention.OnLevelUnloading");
        LifeCycle.Release();
    }
}