namespace NodeController {
    using ColossalFramework;
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters;
    using System.Runtime.Serialization.Formatters.Binary;
    using Util;

    [Serializable]
    public class NodeManager {
        #region LifeCycle
        public static NodeManager Instance { get; private set; } = new NodeManager();

        static BinaryFormatter GetBinaryFormatter =>
            new BinaryFormatter { AssemblyFormat = FormatterAssemblyStyle.Simple };

        public static void Deserialize(byte[] data) {
            if (data == null) {
                Instance = new NodeManager();
                Log.Debug($"NodeBlendManager.Deserialize(data=null)");
                return;
            }
            Log.Debug($"NodeBlendManager.Deserialize(data): data.Length={data?.Length}");

            var memoryStream = new MemoryStream();
            memoryStream.Write(data, 0, data.Length);
            memoryStream.Position = 0;
            Instance = GetBinaryFormatter.Deserialize(memoryStream) as NodeManager;
        }

        public static byte[] Serialize() {
            var memoryStream = new MemoryStream();
            GetBinaryFormatter.Serialize(memoryStream, Instance);
            memoryStream.Position = 0; // redundant
            return memoryStream.ToArray();
        }

        public void OnLoad() {
            RefreshAllNodes();
        }

        #endregion LifeCycle

        public NodeData[] buffer = new NodeData[NetManager.MAX_NODE_COUNT];

        public NodeData InsertNode(NetTool.ControlPoint controlPoint, NodeTypeT nodeType = NodeTypeT.Crossing) {
            if(ToolBase.ToolErrors.None != NetUtil.InsertNode(controlPoint, out ushort nodeID))
                return null;
            HelpersExtensions.Assert(nodeID!=0,"nodeID");

            int nPedLanes = controlPoint.m_segment.ToSegment().Info.CountPedestrianLanes();
            if (nodeType == NodeTypeT.Crossing && nPedLanes<2)
                buffer[nodeID] = new NodeData(nodeID);
            else
                buffer[nodeID] = new NodeData(nodeID, nodeType);
            return buffer[nodeID];
        }

        public NodeData GetOrCreate(ushort nodeID) {
            NodeData data = Instance.buffer[nodeID];
            if (data == null) {
                data = new NodeData(nodeID);
                buffer[nodeID] = data;
            }
            return data;
        }

        /// <summary>
        /// releases data for <paramref name="nodeID"/> if uncessary. Calls update node.
        /// </summary>
        /// <param name="nodeID"></param>
        public void RefreshData(ushort nodeID) {
            if (nodeID == 0 || buffer[nodeID] == null)
                return;
            if (buffer[nodeID].IsDefault()) {
                Log.Info($"node reset to defualt");
                buffer[nodeID] = null;
                NetManager.instance.UpdateNode(nodeID);
            } else {
                buffer[nodeID].Refresh();
            }
        }

        public void RefreshAllNodes() {
            foreach (var nodeData in buffer)
                nodeData?.Refresh();
        }

        public void OnBeforeCalculateNode(ushort nodeID) {
            // nodeID.ToNode still has default flags.
            if (buffer[nodeID] == null)
                return;
            if (!NodeData.IsSupported(nodeID)) {
                buffer[nodeID] = null;
                return;
            }

            buffer[nodeID].Calculate();

            if (!buffer[nodeID].CanChangeTo(buffer[nodeID].NodeType)) {
                buffer[nodeID] = null;
            }
        }

        //public void ChangeNode(ushort nodeID) {
        //    Log.Info($"ChangeNode({nodeID}) called");
        //    NodeBlendData data = GetOrCreate(nodeID);
        //    data.ChangeNodeType();
        //    Instance.buffer[nodeID] = data;
        //    RefreshData(nodeID);
        //}
    }
}
