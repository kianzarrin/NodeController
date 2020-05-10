
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
            LoadGlobalConfig();
        }

        public override void OnSaveData()
        {
            byte[] data = NodeManager.Serialize();
            serializableDataManager.SaveData(DATA_ID, data);
            SaveGlobalConfig();
        }

        public static void LoadGlobalConfig() {
            try { 
            var data = File.ReadAllBytes(global_config_path_);
                UINodeControllerPanel.Deserialize(data);
            }
            catch {
                Log.Info(global_config_path_ + "does not exist. loading default global settings.");
            }
        }

        public static void SaveGlobalConfig() {
            var data = UINodeControllerPanel.Serialize();
            File.WriteAllBytes(global_config_path_, data);
        }

    }
}
