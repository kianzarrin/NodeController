
namespace NodeController.LifeCycle
{
    using JetBrains.Annotations;
    using ICities;
    using KianCommons;
    using System;

    [Serializable]
    public class NCState {
        public static NCState Instance;
        public byte[] NodeManagerData;
        public byte[] SegmentEndManagerData;

        public static byte[] Serialize() {
            Instance = new NCState {
                NodeManagerData = NodeManager.Serialize(),
                SegmentEndManagerData = SegmentEndManager.Serialize(),
            };
            return SerializationUtil.Serialize(Instance);
        }

        public static void Deserialize(byte[] data) {
            if (data == null) {
                Log.Debug($"NCState.Deserialize(data=null)");
                Instance = new NCState();
            } else {
                Log.Debug($"NCState.Deserialize(data): data.Length={data?.Length}");
                Instance = SerializationUtil.Deserialize(data) as NCState;
            }
            SegmentEndManager.Deserialize(Instance.SegmentEndManagerData);
            NodeManager.Deserialize(Instance.NodeManagerData);
        }

    }


    [UsedImplicitly]
    public class SerializableDataExtension
        : SerializableDataExtensionBase
    {
        private const string DATA_ID0 = "RoadTransitionManager_V1.0";
        private const string DATA_ID1 = "NodeController_V1.0";
        private const string DATA_ID = "NodeController_V2.0";

        public static int LoadingVersion;
        public override void OnLoadData()
        {


            byte[] data = serializableDataManager.LoadData(DATA_ID);
            if (data != null) {
                LoadingVersion = 2;
                NCState.Deserialize(data);
            } else {
                // convert to new version
                LoadingVersion = 1;
                data = serializableDataManager.LoadData(DATA_ID1)
                    ?? serializableDataManager.LoadData(DATA_ID0);
                NodeManager.Deserialize(data);
            }


        }

        public override void OnSaveData()
        {
            byte[] data = NCState.Serialize();
            serializableDataManager.SaveData(DATA_ID, data);
        }
    }
}
