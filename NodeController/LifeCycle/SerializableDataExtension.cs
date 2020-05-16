
namespace NodeController.LifeCycle
{
    using JetBrains.Annotations;
    using ICities;
    using NodeController.Tool;
    using NodeController.GUI;
    using System.IO;
    using UnityEngine;
    using NodeController.Util;

    [UsedImplicitly]
    public class SerializableDataExtension
        : SerializableDataExtensionBase
    {
        private const string DATA_ID = "NodeController_V1.0";
        const string FILE_NAME = "NodeController.xml";
        static string global_config_path_ = Path.Combine(Application.dataPath, FILE_NAME);

        public override void OnLoadData()
        {
            byte[] data = serializableDataManager.LoadData(DATA_ID) ??
                serializableDataManager.LoadData("RoadTransitionManager_V1.0");
            NodeManager.Deserialize(data);
        }

        public override void OnSaveData()
        {
            byte[] data = NodeManager.Serialize();
            serializableDataManager.SaveData(DATA_ID, data);
        }
    }
}
