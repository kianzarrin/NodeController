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
            Log.Debug($"Copy node:{nodeData.NodeID} => {this}");

            var data = Serialize();
            var copy = Deserialize(data);
            Log.Debug($"TEST: Serialize->{data}->Deserialize->{copy}");

        }

        public override string ToString() =>
            $"TransferableNodeData[NodeType={NodeType} CornerOffset={CornerOffset} FJ={FlatJunctions} CM={ClearMarkings} FTTL={FirstTimeTrafficLight}]";


        public static TransferableNodeData Deserialize(byte[] data) =>
            (TransferableNodeData)SerializationUtil.Deserialize(data);

        public byte[] Serialize() => SerializationUtil.Serialize(this);
    }
}
