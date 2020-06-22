namespace NodeController.Patches {
    using HarmonyLib;
    using NodeController.LifeCycle;
    using NodeController.Util;
    using System.Collections.Generic;
    using System.Linq;


    //public static void LoadPaths(BuildingInfo info, ushort buildingID, ref Building data, float elevation)
    [HarmonyPatch(typeof(BuildingDecoration), nameof(BuildingDecoration.LoadPaths))]
    public class LoadPathsPatch {
        public static int Index = -1;
        public static BuildingInfo Prefab;
        public static List<pathInfoExt> PathInfoExts;
        public static Dictionary<InstanceID, InstanceID> Map;
        public static bool Mapping => Index != -1;

        /// <summary>
        /// restart mapping for <paramref name="info"/>
        /// </summary>
        public static void Prefix(BuildingInfo info) {
            if (!HelpersExtensions.InSimulationThread())
                return;
            Log.Debug($"LoadPathsPatch.Prefix({info?.ToString() ?? "null"})");
            if (info == null)
                return;

            Index = 0;
            Map = new Dictionary<InstanceID, InstanceID>();
            Prefab = info;

            var Asset2PathInfoExts = AssetDataExtension.Instance.Asset2PathInfoExts;
            Log.Debug("LoadPathsPatch.Prefix(): Asset2PathInfoExts.keys=" +
                Asset2PathInfoExts.Select(item => item.Key).ToSTR());
            if (!Asset2PathInfoExts.TryGetValue(info, out PathInfoExts)) {
                PathInfoExts = null;
            }
            if (PathInfoExts == null) Index = -1;
            Log.Debug($"LoadPathsPatch.Prefix(): Prefab={Prefab} PathInfoExts={PathInfoExts.ToSTR()} Mapping={Mapping}" +
                $"PathInfoExts.Count:{PathInfoExts?.Count}, Prefab.m_paths.Length:{Prefab.m_paths.Length}");
        }

        public static void Postfix(BuildingInfo info) {
            Log.Debug($"LoadPathsPatch.Postfix({info?.ToString() ?? "null"}): Mapping={Mapping} ");
            if (!Mapping)
                return;
            Index = -1;


            // early calculate networks to prepair them for applying data.
            foreach (var item in Map)
                CalculateNetwork(item.Value);

            if (AssetDataExtension.Instance.Asset2NodeDatas.TryGetValue(info, out var nodeDatas)) {
                Log.Debug("LoadPathsPatch.Postfix(): nodeDatas =" + nodeDatas.ToSTR());
                foreach (NodeData nodeData in nodeDatas) {
                    ushort mappedNodeID = GetMappedNodeID(nodeData.NodeID, Map);
                    NodeManager.Instance.TransferNodeData(mappedNodeID, nodeData, refresh: false); // refresh is done when level is loaded
                }
            } else {
                Log.Debug("LoadPathsPatch.Postfix(): nodeDatas not found");
            }
        }

        public static ushort GetMappedNodeID(ushort oldNodeId, Dictionary<InstanceID, InstanceID> map) {
            var ret= map[new InstanceID { NetNode = oldNodeId }].NetNode;
            Log.Debug($"GetMappedNodeID: {oldNodeId} -> {ret}");
            return ret;
        }

        public static void CalculateNetwork(InstanceID instanceId) {
            switch (instanceId.Type) {
                case InstanceType.NetNode:
                    ushort nodeId = instanceId.NetNode;
                    nodeId.ToNode().CalculateNode(nodeId);
                    break;
                case InstanceType.NetSegment:
                    ushort segmentId = instanceId.NetSegment;
                    segmentId.ToSegment().CalculateSegment(segmentId);
                    break;
            }
        }
    }
}
