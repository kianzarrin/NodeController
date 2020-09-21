using ICities;
using KianCommons;
using System;
using System.Collections.Generic;
using static NodeController.LifeCycle.MoveItIntegration;

namespace NodeController.LifeCycle {
    using HarmonyLib;
    using ColossalFramework.UI;
    using System.Runtime.CompilerServices;

    // Credits to boformer
    [HarmonyPatch(typeof(LoadAssetPanel), "OnLoad")]
    public static class OnLoadPatch {
        /// <summary>
        /// when loading asset from file, IAssetData.OnAssetLoaded() is called for all assets but the one that is loaded from file.
        /// this postfix calls IAssetData.OnAssetLoaded() for asset loaded from file.
        /// </summary>
        public static void Postfix(LoadAssetPanel __instance, UIListBox ___m_SaveList) {
            // Taken from LoadAssetPanel.OnLoad
            var selectedIndex = ___m_SaveList.selectedIndex;
            var getListingMetaDataMethod = typeof(LoadSavePanelBase<CustomAssetMetaData>).GetMethod(
                "GetListingMetaData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var listingMetaData = (CustomAssetMetaData)getListingMetaDataMethod.Invoke(__instance, new object[] { selectedIndex });

            // Taken from LoadingManager.LoadCustomContent
            if (listingMetaData.userDataRef != null) {
                AssetDataWrapper.UserAssetData userAssetData = listingMetaData.userDataRef.Instantiate() as AssetDataWrapper.UserAssetData;
                if (userAssetData == null) {
                    userAssetData = new AssetDataWrapper.UserAssetData();
                }
                AssetDataExtension.Instance.OnAssetLoaded(listingMetaData.name, ToolsModifierControl.toolController.m_editPrefabInfo, userAssetData.Data);
            }
        }
    }

    public class AssetDataExtension: AssetDataExtensionBase {
        public const string NC_ID = "NodeController_V1.0";
        //static Building[] buildingBuffer = BuildingManager.instance.m_buildings.m_buffer;

        public static AssetDataExtension Instance;
        public Dictionary<BuildingInfo, object[]> Asset2Records = new Dictionary<BuildingInfo, object[]>();

        public override void OnCreated(IAssetData assetData) {
            base.OnCreated(assetData);
            Instance = this;
        }

        public override void OnReleased() {
            Instance = null;
        }

        public override void OnAssetLoaded(string name, object asset, Dictionary<string, byte[]> userData) {
            Log.Debug($"AssetDataExtension.OnAssetLoaded({name}, {asset}, userData) called");
            if (asset is BuildingInfo prefab) {
                Log.Debug("AssetDataExtension.OnAssetLoaded():  prefab is " + prefab);

                if (userData.TryGetValue(NC_ID, out byte[] data2)) {
                    Log.Debug("AssetDataExtension.OnAssetLoaded():  extracted data for " + NC_ID);
                    var records = SerializationUtil.Deserialize(data2) as object[];
                    HelpersExtensions.AssertNotNull(records, "records");
                    Asset2Records[prefab] = records;
                    Log.Debug("AssetDataExtension.OnAssetLoaded(): nodeDatas=" + records.ToSTR());

                }
            }
        }

        public override void OnAssetSaved(string name, object asset, out Dictionary<string, byte[]> userData) {
            Log.Debug($"AssetDataExtension.OnAssetSaved({name}, {asset}, userData) called");
            userData = null;
            //var info = ToolsModifierControl.toolController.m_editPrefabInfo;
            if(asset is BuildingInfo prefab) {
                Log.Debug("AssetDataExtension.OnAssetSaved():  prefab is " + prefab);


                List<object> records = new List<object>();
                for (ushort nodeID = 0; nodeID < NetManager.MAX_NODE_COUNT; ++nodeID) {
                    object record = CopyNode(nodeID);
                    if (record != null)
                        records.Add(record);
                }
                for (ushort segmentID = 0; segmentID < NetManager.MAX_SEGMENT_COUNT; ++segmentID) {
                    object record = CopySegment(segmentID);
                    if (record != null)
                        records.Add(record);
                }

                Log.Debug("AssetDataExtension.OnAssetSaved(): nodeDatas=" + records.ToSTR());
                userData.Add(NC_ID, SerializationUtil.Serialize(records.ToArray()));
            }
        }


        public static void PlaceAsset(BuildingInfo info, Dictionary<InstanceID, InstanceID> map) {
            if (Instance.Asset2Records.TryGetValue(info, out var records)) {
                Log.Debug("LoadPathsPatch.Postfix(): nodeDatas =" + records.ToSTR());
                foreach (object record in records) {
                    Paste(record,map);
                }
            } else {
                Log.Debug("LoadPathsPatch.Postfix(): nodeDatas not found");
            }
        }

        static AssetDataExtension() {
            try {
                RegisterEvent();
            }
            catch {
                Log.Error("Could not register OnNetworksMapped. TMPE 11.6+ is required");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void RegisterEvent() {
            TrafficManager.Util.PlaceIntersectionUtil.OnPlaceIntersection += PlaceAsset;
        }



    }
}
