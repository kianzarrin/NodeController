
namespace BlendRoadManager.LifeCycle
{
    using JetBrains.Annotations;
    using ICities;

    [UsedImplicitly]
    public class SerializableDataExtension
        : SerializableDataExtensionBase
    {
        private const string DATA_ID = "BlendRoadManager_V1.0";

        public override void OnLoadData()
        {
            byte[] data = serializableDataManager.LoadData(DATA_ID);
            NodeManager.Deserialize(data);
        }

        public override void OnSaveData()
        {
            byte[] data = NodeManager.Serialize();
            serializableDataManager.SaveData(DATA_ID, data);
        }
    }
}
