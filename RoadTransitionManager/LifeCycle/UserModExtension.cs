namespace RoadTransitionManager.LifeCycle
{
    using System;
    using JetBrains.Annotations;
    using ICities;
    using CitiesHarmony.API;
    using RoadTransitionManager.Util;
    public class UserModExtension : IUserMod
    {
        public static Version ModVersion => typeof(UserModExtension).Assembly.GetName().Version;
        public static string VersionString => ModVersion.ToString(2);
        public string Name => "Road transition manager " + VersionString;
        public string Description => "gives you control of segment transitions into other segments or intersections.";

        [UsedImplicitly]
        public void OnEnabled()
        {
            HarmonyHelper.EnsureHarmonyInstalled();   
            if (HelpersExtensions.InGame)
                LifeCycle.Load();
        }

        [UsedImplicitly]
        public void OnDisabled()
        {
            LifeCycle.Release();
        }
    }
}
