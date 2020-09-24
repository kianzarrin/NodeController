namespace NodeController.LifeCycle
{
    using NodeController.Tool;
    using NodeController.Util;
    using KianCommons;
    using ICities;
    using NodeController.GUI;
    using UnityEngine.SceneManagement;

    public static class LifeCycle
    {
        public static void Load(LoadMode mode = LoadMode.NewGame)
        {
            HelpersExtensions.VERBOSE = false;
            Log.Info("LifeCycle.Load() called");

            SimulationManager.UpdateMode updateMode = SimulationManager.instance.m_metaData.m_updateMode;
            string scene = SceneManager.GetActiveScene().name;
            Log.Info($"OnLevelLoaded({mode}) called. updateMode={updateMode}, scene={scene}");
            if (scene == "ThemeEditor")
                return; CSURUtil.Init();
            HarmonyExtension.InstallHarmony();
            NodeControllerTool.Create();
            if (Settings.GameConfig == null) {
                switch (mode) {
                    case LoadMode.NewGameFromScenario:
                    case LoadMode.LoadScenario:
                    case LoadMode.LoadMap:
                        // no NC or old NC
                        Settings.GameConfig = GameConfigT.LoadGameDefault;
                        break;
                    default:
                        Settings.GameConfig = GameConfigT.NewGameDefault;
                        break;
                }
            }

            NodeManager.Instance.OnLoad();
            SegmentEndManager.Instance.OnLoad();
            Log.Info("LifeCycle.Load() sucessful");
        }

        public static void UnLoad()
        {
            Log.Info("LifeCycle.Release() called");
            Settings.GameConfig = null;
            HarmonyExtension.UninstallHarmony();
            NodeControllerTool.Remove();
        }
    }
}
