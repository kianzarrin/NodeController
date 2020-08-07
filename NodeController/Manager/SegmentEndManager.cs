namespace NodeController {
    using System;
    using KianCommons;

    [Serializable]
    public class SegmentEndManager {
        #region LifeCycle
        public static SegmentEndManager Instance { get; private set; } = new SegmentEndManager();

        public static byte[] Serialize() => SerializationUtil.Serialize(Instance);

        public static void Deserialize(byte[] data) {
            if (data == null) {
                Instance = new SegmentEndManager();
                Log.Debug($"SegmentEndManager.Deserialize(data=null)");

            } else {
                Log.Debug($"SegmentEndManager.Deserialize(data): data.Length={data?.Length}");
                Instance = SerializationUtil.Deserialize(data) as SegmentEndManager;
            }
        }

        public void OnLoad() {
            RefreshAllSegmentEnds();
        }

        #endregion LifeCycle

        public SegmentEndData[] buffer = new SegmentEndData[NetManager.MAX_SEGMENT_COUNT * 2];

        public ref SegmentEndData GetSegmentEnd(ushort segmentID, ushort nodeID) {
            bool startNode = NetUtil.IsStartNode(segmentId: segmentID, nodeId: nodeID);
            return ref GetOrCreate(segmentID, startNode);
        }
        public ref SegmentEndData GetSegmentEnd(ushort segmentID, bool startNode) {
            if (startNode)
                return ref buffer[segmentID * 2];
            else
                return ref buffer[segmentID * 2 + 1];
        }

        public void SetSegmentEnd(ushort segmentID, bool startNode, SegmentEndData segmentEnd) {
            GetSegmentEnd(segmentID, startNode) = segmentEnd;
        }

        public ref SegmentEndData GetOrCreate(ushort segmentID, ushort nodeID) {
            bool startNode = NetUtil.IsStartNode(segmentId: segmentID, nodeId: nodeID);
            return ref GetOrCreate(segmentID, startNode);
        }

        public ref SegmentEndData GetOrCreate(ushort segmentID, bool startNode) {
            ref SegmentEndData data = ref GetSegmentEnd(segmentID, startNode);
            if (data == null) {
                ushort nodeID = NetUtil.GetSegmentNode(segmentID, startNode);
                data = new SegmentEndData(segmentID: segmentID, nodeID: nodeID);
                SetSegmentEnd(segmentID: segmentID, startNode: startNode, data);
            }
            return ref data;
        }


        #region data tranfer
        public static byte[] CopySegmentEndData(ushort segmentID, bool startNode) =>
            Instance.CopySegmentEndDataImp(segmentID, startNode);

        public static void PasteSegmentEndData(ushort segmentID, bool startNode, byte[] data) =>
            Instance.PasteSegmentEndDataImp(segmentID, startNode, data);


        private byte[] CopySegmentEndDataImp(ushort segmentID, bool startNode) {
            var nodeData = GetSegmentEnd(segmentID, startNode);
            if (nodeData == null) {
                Log.Debug($"node:{segmentID} startNode:{startNode} has no custom data");
                return null;
            }
            return SerializationUtil.Serialize(nodeData);
        }

        private void PasteSegmentEndDataImp(ushort segmentID, bool startNode, byte[] data) {
            if (data == null) {
                ResetSegmentEndToDefault(segmentID, startNode);
            } else {
                var segEnd = SerializationUtil.Deserialize(data) as SegmentEndData;
                SetSegmentEnd(segmentID, startNode, segEnd);
                segEnd.SegmentID = segmentID;
                segEnd.NodeID = NetUtil.GetSegmentNode(segmentID, startNode);
                RefreshData(segmentID, startNode);
            }
        }
        #endregion


        /// <summary>
        /// releases data for <paramref name="nodeID"/> if uncessary. Calls update node.
        /// </summary>
        /// <param name="nodeID"></param>
        public void RefreshData(ushort segmentID, bool startNode) {
            SegmentEndData segEnd = GetSegmentEnd(segmentID, startNode);
            if (segmentID == 0 || segEnd == null) 
                return;
            if (segEnd.IsDefault()) {
                ResetSegmentEndToDefault(segmentID,startNode);
            } else {
                segEnd.Refresh();
            }
        }

        public void ResetSegmentEndToDefault(ushort segmentID, bool startNode) {
            SegmentEndData segEnd = GetSegmentEnd(segmentID, startNode);
            if (segEnd != null)
                Log.Debug($"segment End:({segmentID},{startNode}) reset to defualt");
            else
                Log.Debug($"segment End:({segmentID},{startNode}) is already null.");
            segEnd = null;
            NetManager.instance.UpdateSegment(segmentID);
        }

        public void RefreshAllSegmentEnds() {
            foreach (var nodeData in buffer)
                nodeData?.Refresh();
        }

        public void OnBeforeCalculateSegmentEnd(ushort segmentID, bool startNode) {
            SegmentEndData segEnd = GetSegmentEnd(segmentID, startNode);
            // nodeID.ToNode still has default flags.
            if (segEnd == null)
                return;
            if (!NodeData.IsSupported(segEnd.NodeID)) {
                SetSegmentEnd(segmentID, startNode, null);
                return;
            }

            segEnd.Calculate();
        }

    }
}
