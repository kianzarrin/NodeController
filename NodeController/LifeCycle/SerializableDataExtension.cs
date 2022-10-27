namespace NodeController.LifeCycle
{
    using JetBrains.Annotations;
    using ICities;
    using KianCommons;
    using System;
    using NodeController.GUI;
    using KianCommons.Serialization;

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

        public static void Deserialize(byte[] data) {
            if (data == null) {
                Log.Debug($"NCState.Deserialize(data=null)");
                Instance = new NCState();
            } else {
                Log.Debug($"NCState.Deserialize(data): data.Length={data?.Length}");
                Instance = SerializationUtil.Deserialize(data, default) as NCState;
                if (Instance?.Version != null) { //2.1.1 or above
                    Log.Debug("Deserializing V"+ Instance.Version);
                    SerializationUtil.DeserializationVersion = new Version(Instance.Version);
                } else {
                    // 2.0
                    Log.Debug("Deserializing version 2.0");
                    Instance.Version = "2.0";
                    Instance.GameConfig = GameConfigT.LoadGameDefault; // for the sake of feature proofing.
                    Instance.GameConfig.UnviversalSlopeFixes = true; // in this version I do apply slope fixes.
                }
            }
            Log.Debug($"setting UnviversalSlopeFixes to {Instance.GameConfig.UnviversalSlopeFixes}");
            NCSettings.GameConfig = Instance.GameConfig;
            NCSettings.UpdateGameSettings();
            var version = new Version(Instance.Version);
            SegmentEndManager.Deserialize(Instance.SegmentEndManagerData, version);
            NodeManager.Deserialize(Instance.NodeManagerData, version);
        }

    }

    [UsedImplicitly]
    public class SerializableDataExtension
        : SerializableDataExtensionBase
    {
        private const string DATA_ID0 = "RoadTransitionManager_V1.0";
        private const string DATA_ID1 = "NodeController_V1.0";
        private const string DATA_ID = "NodeController_V2.0";
        private static ISerializableData SerializableData => SimulationManager.instance.m_SerializableDataWrapper;

        public static int LoadingVersion;


        public override void OnLoadData() => Load();
        public static void Load() { 
            byte[] data = SerializableData.LoadData(DATA_ID);
            if (data != null) {
                LoadingVersion = 2;
                NCState.Deserialize(data);
            } else {
                // convert to new version
                LoadingVersion = 1;
                data = SerializableData.LoadData(DATA_ID1)
                    ?? SerializableData.LoadData(DATA_ID0);
                NodeManager.Deserialize(data, new Version(1, 0));
            }
        }

        public override void OnSaveData() => Save();
        public static void Save() {
            byte[] data = NCState.Serialize();
            SerializableData.SaveData(DATA_ID, data);
        }
    }
}
