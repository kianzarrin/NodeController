namespace BlendRoadManager.LifeCycle
{
    using System;
    using JetBrains.Annotations;
    using ICities;
    using BlendRoadManager.Util;
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
