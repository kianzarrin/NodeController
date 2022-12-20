namespace NodeController.LifeCycle
{
    using JetBrains.Annotations;
    using ICities;
    using KianCommons;
    using System;
    using NodeController.GUI;
    using KianCommons.Serialization;
    using NodeController.Manager;
    using UnityEngine.SceneManagement;
    using System.Runtime.InteropServices.ComTypes;
    using static RenderManager;

    [Serializable]
    public class NCState {
        public static NCState Instance;

        public string Version = typeof(NCState).VersionOf().ToString(3);
        public byte[] NodeManagerData;
        public byte[] SegmentEndManagerData;
        public GameConfigT GameConfig;

        public static byte[] Serialize() {
            NodeManager.ValidateAndHeal(false);
            Instance = new NCState {
                NodeManagerData = NodeManager.Serialize(),
                SegmentEndManagerData = SegmentEndManager.Serialize(),
                GameConfig = NCSettings.GameConfig,
            };

            Log.Debug("NCState.Serialize(): saving UnviversalSlopeFixes as " +
                Instance.GameConfig.UnviversalSlopeFixes);

            return SerializationUtil.Serialize(Instance);
        }

        public static NCState DeserializeState(byte[] data) {
            if (data == null) {
                Log.Debug($"NCState.Deserialize(data=null)");
                return new NCState();
            } else {
                Log.Debug($"NCState.Deserialize(data): data.Length={data?.Length}");
                var ret = SerializationUtil.Deserialize(data, default) as NCState;
                if (ret?.Version != null) { //2.1.1 or above
                    Log.Debug("Deserializing V" + ret.Version);
                    SerializationUtil.DeserializationVersion = new Version(ret.Version);
                } else {
                    // 2.0
                    Log.Debug("Deserializing version 2.0");
                    ret.Version = "2.0";
                    ret.GameConfig = GameConfigT.LoadGameDefault; // for the sake of future proofing.
                    ret.GameConfig.UnviversalSlopeFixes = true; // in this version I do apply slope fixes.
                }
                return ret;
            }
        }

        public void DeserilizeConfig() {
            if (GameConfig == null) {
                NCSettings.LoadDefaltConfig(NCLifeCycle.Mode);
            } else {
                NCSettings.GameConfig = GameConfig;
            }
            Log.Info($"UnviversalSlopeFixes={NCSettings.GameConfig.UnviversalSlopeFixes}");
        }

        public void DeserializeManagers() {
            try {
                var version = new Version(Version);
                SegmentEndManager.Deserialize(SegmentEndManagerData, version);
                NodeManager.Deserialize(NodeManagerData, version);
            } catch (Exception ex) { ex.Log(); }
        }
    }

    [UsedImplicitly]
    public class SerializableDataExtension
        : SerializableDataExtensionBase
    {
        private const string DATA_ID0 = "RoadTransitionManager_V1.0";
        private const string DATA_ID1 = "NodeC+ontroller_V1.0";
        private const string DATA_ID = "NodeController_V2.0";
        private static ISerializableData SerializableData => SimulationManager.instance.m_SerializableDataWrapper;

        public static int LoadingVersion;

        public static void BeforeNetMangerAfterDeserialize() {
            try {
                Log.Called();
                byte[] data = SerializableData.LoadData(DATA_ID);
                NCState.Instance = NCState.DeserializeState(data);
                NCState.Instance.DeserilizeConfig();
            } catch (Exception ex) { ex.Log(); }
        }

        public override void OnLoadData() => Load();
        public static void Load() {
            try {
                Log.Called();
                Log.Debug(Helpers.WhatIsCurrentThread());
                Log.Debug("SimulationPaused=" + SimulationManager.instance.SimulationPaused);
                Log.Debug($"[before] NetManger to update {NodeManager.CountUpdatingNodes()} nodes and {SegmentEndManager.CountUpdatingSegments()} segments.");
                byte[] data = SerializableData.LoadData(DATA_ID);
                if (data != null) {
                    LoadingVersion = 2;
                    NCState.Instance?.DeserializeManagers();
                } else {
                    // convert to new version
                    LoadingVersion = 1;
                    data = SerializableData.LoadData(DATA_ID1)
                        ?? SerializableData.LoadData(DATA_ID0);
                    NodeManager.Deserialize(data, new Version(1, 0));
                }

                NodeManager.ValidateAndHeal(true);
                NodeManager.Instance.OnLoad();
                SegmentEndManager.Instance.OnLoad();
                Log.Info($"[after] NetManger to update {NodeManager.CountUpdatingNodes()} nodes and {SegmentEndManager.CountUpdatingSegments()} segments.");
                NCLifeCycle.LoadingStage = NCLifeCycle.Stage.DataLoaded;
                Log.Succeeded();
            } catch(Exception ex) { ex.Log(); }
        }

        public override void OnSaveData() => Save();
        public static void Save() {
            try {
                Log.Called();
                byte[] data = NCState.Serialize();
                SerializableData.SaveData(DATA_ID, data);
            }catch(Exception ex) { ex.Log(); }
        }
    }
}
