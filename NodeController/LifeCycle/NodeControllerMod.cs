namespace NodeController.LifeCycle
{
    using System;
    using JetBrains.Annotations;
    using ICities;
    using CitiesHarmony.API;
    using KianCommons;
    using System.Diagnostics;

    public class NodeControllerMod : IUserMod
    {
        public static Version ModVersion => typeof(NodeControllerMod).Assembly.GetName().Version;
        public static string VersionString => ModVersion.ToString(2);
        public string Name => "Node controller " + VersionString;
        public string Description => "control Road/junction transitions";

        [UsedImplicitly]
        public void OnEnabled()
        {
            Log.Debug("Testing StackTrace:\n" + new StackTrace(true).ToString(), copyToGameLog: false);
            KianCommons.UI.TextureUtil.EmbededResources = false;
            HarmonyHelper.EnsureHarmonyInstalled();   
            if (!HelpersExtensions.InStartup)
                LifeCycle.Load();
#if DEBUG
            //HarmonyExtension.InstallHarmony(); // Only for testing
#endif
        }

        [UsedImplicitly]
        public void OnDisabled()
        {
            LifeCycle.UnLoad();
        }

        [UsedImplicitly]
        public void OnSettingsUI(UIHelperBase helper) {
            GUI.Settings.OnSettingsUI(helper);
        }

    }
}
