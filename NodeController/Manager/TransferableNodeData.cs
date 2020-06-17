namespace NodeController {
    using System;
    using Util;

    [Serializable]
    public class TransferableNodeData {
        public NodeTypeT NodeType;
        public float CornerOffset;
        public bool FlatJunctions;
        public bool ClearMarkings;
        public bool FirstTimeTrafficLight;
        public TransferableNodeData(NodeData nodeData) {
            HelpersExtensions.AssertNotNull(nodeData, "nodeData");
            NodeType = nodeData.NodeType;
            CornerOffset = nodeData.CornerOffset;
            FlatJunctions = nodeData.FlatJunctions;
            ClearMarkings = nodeData.ClearMarkings;
            FirstTimeTrafficLight = nodeData.FirstTimeTrafficLight;
        }

        public static TransferableNodeData Deserialize(byte[] data) =>
        SerializationUtil.Deserialize(data) as TransferableNodeData;

        public byte[] Serialize() => SerializationUtil.Serialize(this);
    }
}
