namespace NodeController.LifeCycle {
    using System;
    using CitiesHarmony.API;
    using ICities;
    using KianCommons;
    using NodeController.GUI;
    using NodeController.Tool;
    using NodeController.Util;
    using System.Diagnostics;
    using UnityEngine.SceneManagement;
    using NodeController.Manager;
    using NodeController.Patches;
    using KianCommons.IImplict;
    using System.Threading;
    using KianCommons.Plugins;

    public abstract class AdvancedLifeCycleBase : ILoadingExtension, IMod, IUserMod, IModWithSettings {
        public enum Stage {
            Enabled,
            MainMenu,
            Preloaded,
            DataLoaded,
            MetaDataReady,
            SimulationDataReady,
            Loaded,
            Unloading,
            PreExit,
        }

        public static Stage LoadingStage = Stage.MainMenu;


        public static SimulationManager.UpdateMode UpdateMode => SimulationManager.instance.m_metaData.m_updateMode;
        public static LoadMode Mode => (LoadMode)UpdateMode;
        public static string Scene => SceneManager.GetActiveScene().name;

        public static AdvancedLifeCycleBase Instance { get; private set; }

        public static Version ModVersion => typeof(AdvancedLifeCycleBase).Assembly.GetName().Version;
        public static string VersionString => ModVersion.ToString(2);

        internal AdvancedLifeCycleBase() => Instance = this;

        #region MOD
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract void OnSettingsUI(UIHelper helper);
        public abstract void Start();
        public virtual void IntroLoaded() { }
        public abstract void End();

        public virtual void OnEnabled() {
            try {
                LoadingManager.instance.m_introLoaded += OnIntroLoaded;
                LoadingManager.instance.m_levelPreLoaded += OnLevelPreLoaded;
                LoadingManager.instance.m_levelPreUnloaded += OnLevelPreUnloaded;
                LoadingManager.instance.m_metaDataReady += OnMetaDataReady;
                LoadingManager.instance.m_simulationDataReady += OnSimulationDataReady;
                LoadingManager.instance.m_levelUnloaded += OnLevelUnloaded;
                KianCommons.UI.TextureUtil.EmbededResources = false;
                Start();
                LoadingStage = Stage.Enabled;
                if (LoadingManager.instance.m_loadingComplete)
                    HotReload();
            } catch (Exception ex) { Log.Exception(ex); }
        }
        public virtual void OnDisabled() {
            try {
                Log.Called();
                LoadingManager.instance.m_introLoaded -= OnIntroLoaded;
                LoadingManager.instance.m_levelPreLoaded -= OnLevelPreLoaded;
                LoadingManager.instance.m_levelPreUnloaded -= OnLevelPreUnloaded;
                LoadingManager.instance.m_metaDataReady -= OnMetaDataReady;
                LoadingManager.instance.m_simulationDataReady -= OnSimulationDataReady;
                LoadingManager.instance.m_levelUnloaded -= OnLevelUnloaded;
                if (LoadingManager.instance.m_loadingComplete) {
                    LoadingStage = Stage.Unloading;
                    OnLevelUnloading();
                    OnLevelPreUnloaded();
                    OnLevelUnloaded();
                    LoadingStage = Stage.MainMenu;
                }
                Log.Succeeded();
                Log.Flush();
            } catch (Exception ex) { Log.Exception(ex); }
        }
        #endregion

        #region events
        private void OnIntroLoaded() {
            try {
                Log.Called();
                IntroLoaded();
                LoadingStage = Stage.MainMenu;
                Log.Succeeded();
            } catch(Exception ex) { ex.Log(); }
        }

        private void OnLevelPreLoaded() {
            try {
                Log.Called();
                PluginUtil.LogPlugins(detailed: false);
                Preload();
                LoadingStage = Stage.Preloaded;
                Log.Succeeded();
            } catch (Exception ex) { ex.Log(); }
        }
        private void OnMetaDataReady() {
            try {
                Log.Called();
                MetaDataReady();
                LoadingStage = Stage.MetaDataReady;
                Log.Succeeded();
            } catch (Exception ex) { ex.Log(); }
        }

        private void OnSimulationDataReady() {
            try {
                Log.Called();
                SimulationDataReady();
                LoadingStage = Stage.SimulationDataReady;
                Log.Succeeded();
            } catch (Exception ex) { ex.Log(); }
        }


        private void OnLevelPreUnloaded() {
            try {
                Log.Called();
                PreExit();
                LoadingStage = Stage.PreExit;
                Log.Succeeded();
            } catch (Exception ex) { ex.Log(); }
        }


        private void OnLevelUnloaded() {
            try {
                Log.Called();
                Exit();
                LoadingStage = Stage.MainMenu;
                Log.Succeeded();
            } catch (Exception ex) { ex.Log(); }
        }

        public void OnCreated(ILoading loading) { }

        public void OnReleased() { }

        public void OnLevelLoaded(LoadMode mode) {
            try {
                Log.Called();
                Load();
                LoadingStage = Stage.Loaded;
                Log.Succeeded();
            } catch (Exception ex) { ex.Log(); }
        }

        public void OnLevelUnloading() {
            try {
                Log.Called();
                LoadingStage = Stage.Unloading;
                UnLoading();
                Log.Succeeded();
            } catch (Exception ex) { ex.Log(); }
        }

        #endregion
        public virtual void HotReload() {
            Log.Called();
            try {
                OnLevelPreLoaded();
                OnMetaDataReady();
                OnSimulationDataReady();
                OnLevelLoaded(0);
                Log.Succeeded();
            } catch(Exception ex) { ex.Log(); }
        }

        public virtual void Preload() { }
        public virtual void MetaDataReady() { }
        public virtual void SimulationDataReady() { }
        public virtual void Load() { }

        public virtual void UnLoading() { }
        public virtual void PreExit() { }

        public virtual void Exit() { } // exit to main menu
    }


    public class NCLifeCycle : AdvancedLifeCycleBase {
        public static string HARMONY_ID = "CS.Kian.NodeController";
        public override string Name => "Node controller " + VersionString;
        public override string Description => "control Nodes/Corners";
        public override void Start() {
            HarmonyHelper.EnsureHarmonyInstalled();
            const bool fastTestHarmony = false;
            if (fastTestHarmony) {
                HarmonyUtil.InstallHarmony(HARMONY_ID);
                Process.GetCurrentProcess().Kill();
            }

        }
        public override void OnSettingsUI(UIHelper helper) {
            NCSettings.OnSettingsUI(helper);
        }

        public override void End() {
            LaneCache.Instance?.Release();
        }

        public override void HotReload() {
            HarmonyUtil.InstallHarmony<HotReloadPatchAttribute>(HARMONY_ID);
            var task = SimulationManager.instance.AddAction(SerializableDataExtension.Load);
            while (task.completedOrFailed) {
                Thread.Sleep(1);
            }
            base.HotReload();
            TMPE_Loaded();
        }

        private static bool harmonyInstalled_ = false;
        public override void Preload() {
            if (Scene == "ThemeEditor") return;
            TrafficManager.API.Implementations.Notifier.EventLevelLoaded += TMPE_Loaded;
            CSURUtil.Init();
            NCSettings.GameConfig = GameConfigT.NewGameDefault;
            LaneCache.Instance?.Release();
            LaneCache.Create();
            SegmentEndManager.Deserialize(null, default);
            NodeManager.Deserialize(null, default);
            if (!harmonyInstalled_) {
                // skip when loading another game
                HarmonyUtil.InstallHarmony(HARMONY_ID, forbidden: typeof(HotReloadPatchAttribute));
                harmonyInstalled_ = true;
            }
        }


        private static void TMPE_Loaded() {
            Log.Called();
            LaneCache.Ensure();
            LaneCache.Instance.OnTMPELoaded();
        }

        public override void SimulationDataReady() {
            if (Scene == "ThemeEditor") {
                Log.Info("Skip SimulationDataReady theme editor", true);
                return;
            }
            NCSettings.UpdateGameSettings();
        }

        public override void Load() {
            NodeControllerTool.Create();
        }

        public static void Unload() {
            Log.Info("LifeCycle.Unload() called");
            NodeControllerTool.Remove();
        }
        public override void PreExit() {
            harmonyInstalled_ = false;
            HarmonyUtil.UninstallHarmony(HARMONY_ID);
            NCSettings.GameConfig = null;
            LaneCache.Instance?.Release();
            TrafficManager.API.Implementations.Notifier.EventLevelLoaded -= TMPE_Loaded;
        }
    }
}
