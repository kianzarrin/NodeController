using ColossalFramework;
using System;

namespace BlendRoadManager {
    using System.IO;
    using System.Runtime.Serialization.Formatters;
    using System.Runtime.Serialization.Formatters.Binary;
    using Util;

    [Serializable]
    public class NodeBlendManager {
        #region LifeCycle
        public static NodeBlendManager Instance { get; private set; } = new NodeBlendManager();

        static BinaryFormatter GetBinaryFormatter =>
            new BinaryFormatter { AssemblyFormat = FormatterAssemblyStyle.Simple };

        public static void Deserialize(byte[] data) {
            if (data == null) {
                Instance = new NodeBlendManager();
                Log.Debug($"NodeBlendManager.Deserialize(data=null)");
                return;
            } 
            Log.Debug($"NodeBlendManager.Deserialize(data): data.Length={data?.Length}");

            var memoryStream = new MemoryStream();
            memoryStream.Write(data, 0, data.Length);
            memoryStream.Position = 0;
            Instance = GetBinaryFormatter.Deserialize(memoryStream) as NodeBlendManager;
        }

        public static byte[] Serialize() {
            var memoryStream = new MemoryStream();
            GetBinaryFormatter.Serialize(memoryStream, Instance);
            memoryStream.Position = 0; // redundant
            return memoryStream.ToArray();
        }
        #endregion LifeCycle

        public NodeBlendData[] buffer = new NodeBlendData[NetManager.MAX_NODE_COUNT];

        public NodeBlendData GetOrCreate(ushort nodeID) {
            NodeBlendData data = NodeBlendManager.Instance.buffer[nodeID];
            if (data == null) {
                data = new NodeBlendData(nodeID);
                NodeBlendManager.Instance.buffer[nodeID] = data;
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
            if(buffer[nodeID].IsDefault()) {
                Log.Info($"node reset to defualt");
                buffer[nodeID] = null;
            } else {
                buffer[nodeID].Refresh();
            }
            NetManager.instance.UpdateNode(nodeID);
        }

        public void ChangeNode(ushort nodeID) {
            Log.Info($"ChangeNode({nodeID}) called");
            NodeBlendData data = GetOrCreate(nodeID);
            data.ChangeNodeType();
            Instance.buffer[nodeID] = data;
            RefreshData(nodeID);
        }


        public void ChangeOffset(ushort nodeID) {
            Log.Info($"ChangeOffset({nodeID}) called");
            if (!nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Junction)) {
                Log.Info($"Not a junction");
                return;
            }

            NodeBlendData data = GetOrCreate(nodeID);
            if (!data.CanModifyOffset())
                return;

            data.IncrementOffset();
            Instance.buffer[nodeID] = data;
            RefreshData(nodeID);
        }
    }

    public enum NodeTypeT {
        Middle,
        Crossing, // change dataMatrix.w to render crossings in the middle.
        Node,
        Segment,
    }

    [Serializable]
    public class NodeBlendData {
        public ushort NodeID;
        public int SegmentCount;

        public float DefaultCornerOffset => NodeID.ToNode().Info.m_minCornerOffset;
        public NetNode.Flags DefaultFlags;

        public NodeTypeT NodeType;
        public NodeTypeT DefaultNodeType;
        public float CornerOffset;

        public NodeBlendData(ushort nodeID) {
            NodeID = nodeID;
            SegmentCount = nodeID.ToNode().CountSegments();
            DefaultFlags = nodeID.ToNode().m_flags;

            if (DefaultFlags.IsFlagSet(NetNode.Flags.Middle))
                DefaultNodeType = NodeTypeT.Middle;
            else if (DefaultFlags.IsFlagSet(NetNode.Flags.Junction))
                DefaultNodeType = NodeTypeT.Node;
            else
                throw new NotImplementedException("unsupported node flags: " + DefaultFlags);

            NodeType = DefaultNodeType;
            CornerOffset = DefaultCornerOffset;
        }

        public bool IsDefault() {
            bool ret = CornerOffset - DefaultCornerOffset < OFFSET_STEP / 2 + 0.001f;
            ret &= NodeType == DefaultNodeType;
            return ret;
        }

        public void ChangeNodeType() {
            if (!CanModifyNodeType()) {
                throw new Exception("cannot change junction type");
            } else if (DefaultNodeType == NodeTypeT.Node) {
                switch (NodeType) {
                    case NodeTypeT.Node :
                        NodeType = NodeTypeT.Middle ;
                        break;
                    case NodeTypeT.Middle:
                        NodeType = NodeTypeT.Crossing ;
                        break;
                    case NodeTypeT.Crossing:
                        NodeType = NodeTypeT.Segment ;
                        break;
                    case NodeTypeT.Segment:
                        NodeType = NodeTypeT.Node ;
                        break;
                    default:
                        throw new Exception("Unreachable code");
                }
            }
            if (DefaultNodeType == NodeTypeT.Middle) {
                NodeType++;
                if (NodeType > HelpersExtensions.GetMaxEnumValue<NodeTypeT>())
                    NodeType = 0;
            }
        }

        public const float OFFSET_STEP = 5f;
        /// <summary>
        /// in case of overflow resets type and return true.
        /// </summary>
        public void IncrementOffset() {
            CornerOffset += OFFSET_STEP;
            if (CornerOffset > OFFSET_STEP * 10)
                CornerOffset = 0;
        }

        public void Refresh() {
            if (!CanModifyOffset())
                CornerOffset = DefaultCornerOffset;
            NetManager.instance.UpdateNode(NodeID);
        }

        public bool NeedMiddleFlag() => NodeType == NodeTypeT.Middle;
        public bool NeedJunctionFlag() => !NeedMiddleFlag();
        public bool WantsTrafficLight() => NeedJunctionFlag();
        public bool CanModifyOffset() => NodeType == NodeTypeT.Segment || NodeType == NodeTypeT.Node;
        public bool CanModifyNodeType() => SegmentCount == 2;
    }
}
