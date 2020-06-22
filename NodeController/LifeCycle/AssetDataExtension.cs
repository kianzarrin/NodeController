using ICities;
using NodeController.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeController.LifeCycle {
    [Serializable]
    public class pathInfoExt {
        public ushort SegmentId;
        public ushort StartNodeId;
        public ushort EndNodeId;

        public void MapInstanceIDs(ushort newSegmentId, Dictionary<InstanceID, InstanceID> map) {
            map[new InstanceID { NetSegment = SegmentId }] =
                new InstanceID { NetSegment = newSegmentId };

            map[new InstanceID { NetNode = StartNodeId }] =
                new InstanceID { NetNode = newSegmentId.ToSegment().m_startNode };

            map[new InstanceID { NetNode = EndNodeId }] =
                new InstanceID { NetNode = newSegmentId.ToSegment().m_endNode };
        }
    }

    public class AssetDataExtension: AssetDataExtensionBase {
        public const string PATH_ID = "TMPE_oldPathInfoExts_V1.0";
        public const string NC_ID = "NodeController_V1.0";
        static Building[] buildingBuffer = BuildingManager.instance.m_buildings.m_buffer;

        public static AssetDataExtension Instance;
        public Dictionary<BuildingInfo, List<pathInfoExt>> Asset2PathInfoExts = new Dictionary<BuildingInfo, List<pathInfoExt>>();
        public Dictionary<BuildingInfo, List<NodeData>> Asset2NodeDatas = new Dictionary<BuildingInfo, List<NodeData>>();

        public override void OnCreated(IAssetData assetData) {
            base.OnCreated(assetData);
            Instance = this;
        }

        public override void OnReleased() {
            Instance = null;
        }

        public override void OnAssetLoaded(string name, object asset, Dictionary<string, byte[]> userData) {
            if (asset is BuildingInfo prefab) {
                if (userData.TryGetValue(PATH_ID, out byte[] data1)) {
                    var oldPathInfoExts = SerializationUtil.Deserialize(data1) as List<pathInfoExt>;
                    HelpersExtensions.AssertNotNull(oldPathInfoExts, "oldPathInfoExts");
                    Asset2PathInfoExts[prefab] = oldPathInfoExts;
                }
                if (userData.TryGetValue(NC_ID, out byte[] data2)) {
                    var nodeDatas = SerializationUtil.Deserialize(data2) as List<NodeData>;
                    HelpersExtensions.AssertNotNull(nodeDatas,"nodedatas");
                    Asset2NodeDatas[prefab] = nodeDatas;
                }
            }
        }

        //public void OnAssetLoadedInAssetEditor(string name, object asset, Dictionary<string, byte[]> userData) {
        //    if (asset is BuildingInfo prefab) {
        //        if (userData.TryGetValue(NC_ID, out byte[] data2)) {
        //            var nodeDatas = SerializationUtil.Deserialize(data2) as List<NodeData>;
        //            HelpersExtensions.AssertNotNull(nodeDatas, "nodedatas");
        //            foreach (var nodeData in nodeDatas) {
        //                NodeManager.Instance.buffer[nodeData.NodeID] = nodeData;
        //            }
        //        }
        //    }
        //}

        public override void OnAssetSaved(string name, object asset, out Dictionary<string, byte[]> userData) {
            userData = null;
            //var info = ToolsModifierControl.toolController.m_editPrefabInfo;
            if(asset is BuildingInfo buildingInfo) {
                var oldPathInfoExts = GetOldNetworkIDs(buildingInfo);
                userData = new Dictionary<string, byte[]>();
                userData.Add(PATH_ID, SerializationUtil.Serialize(oldPathInfoExts));

                List<NodeData> nodeDatas = NodeManager.Instance.GetNodeDataList();
                userData.Add(NC_ID, SerializationUtil.Serialize(nodeDatas));
            }
        }


        public static List<pathInfoExt> GetOldNetworkIDs(BuildingInfo info) {
            List<ushort> buildingSegmentIds = new List<ushort>();
            List<ushort> buildingIds = new List<ushort>(info.m_paths.Length);
            var oldInstanceIds = new List<pathInfoExt>();
            for (ushort buildingId = 1; buildingId < BuildingManager.MAX_BUILDING_COUNT; buildingId += 1) {
                if (buildingBuffer[buildingId].m_flags != Building.Flags.None) {
                    buildingSegmentIds.AddRange(BuildingDecoration.GetBuildingSegments(ref buildingBuffer[buildingId]));
                    buildingIds.Add(buildingId);
                }
            }
            for (ushort segmentId = 0; segmentId <NetManager.MAX_SEGMENT_COUNT; segmentId++) {
                if (NetUtil.IsSegmentValid(segmentId)) {
                    if (!buildingSegmentIds.Contains(segmentId)) {
                        var item = new pathInfoExt {
                            SegmentId =  segmentId,
                            StartNodeId = segmentId.ToSegment().m_startNode,
                            EndNodeId = segmentId.ToSegment().m_endNode,
                        };
                        oldInstanceIds.Add(item);
                    }
                }
            }

            //for (ushort nodeId = 0; nodeId < NetManager.MAX_NODE_COUNT; nodeId++) {
            //    if (NetUtil.IsNodeValid(nodeId) && nodeId.ToNode().CountSegments() == 0) {
            //        bool skip = false;
            //        foreach (ushort buildingId in buildingIds) {
            //            if (buildingBuffer[buildingId].ContainsNode(nodeId)) {
            //                skip = true;
            //                break;
            //            }
            //        }
            //        if (!skip) {
            //            oldInstanceIds.Add(new InstanceID { NetNode = nodeId });
            //        }
            //    }
            //}

            return oldInstanceIds;
        }


    }
}
