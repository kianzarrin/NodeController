namespace NodeController {
    using System;
    using System.Collections.Generic;
    using KianCommons;
    using NodeController.Tool;
    using UnityEngine.Assertions;
    using UnityEngine;
    using static KianCommons.Assertion;
    using KianCommons.Serialization;

    [Serializable]
    public class SegmentEndManager {
        #region LifeCycle
        public static SegmentEndManager Instance { get; private set; } = new SegmentEndManager();

        public static byte[] Serialize() => SerializationUtil.Serialize(Instance);

        public static void Deserialize(byte[] data, Version version) {
            if (data == null) {
                Instance = new SegmentEndManager();
                Log.Debug($"SegmentEndManager.Deserialize(data=null)");

            } else {
                Log.Debug($"SegmentEndManager.Deserialize(data): data.Length={data?.Length}");
                Instance = SerializationUtil.Deserialize(data, version) as SegmentEndManager;
            }
        }

        public void OnLoad() {
            UpdateAll();
        }

        #endregion LifeCycle

        public SegmentEndData[] buffer = new SegmentEndData[NetManager.MAX_SEGMENT_COUNT * 2];

        public ref SegmentEndData GetAt(ushort segmentID, ushort nodeID) {
            bool startNode = NetUtil.IsStartNode(segmentId: segmentID, nodeId: nodeID);
            return ref GetAt(segmentID, startNode);
        }
        public ref SegmentEndData GetAt(ushort segmentID, bool startNode) {
            if (startNode)
                return ref buffer[segmentID * 2];
            else
                return ref buffer[segmentID * 2 + 1];
        }

        public void SetAt(ushort segmentID, ushort nodeID, SegmentEndData value) {
            bool startNode = NetUtil.IsStartNode(segmentId: segmentID, nodeId: nodeID);
            SetAt(segmentID, startNode, value);
        }

        public void SetAt(ushort segmentID, bool startNode, SegmentEndData value) {
            GetAt(segmentID, startNode) = value;
        }

        public ref SegmentEndData GetOrCreate(ushort segmentID, ushort nodeID) {
            bool startNode = NetUtil.IsStartNode(segmentId: segmentID, nodeId: nodeID);
            return ref GetOrCreate(segmentID, startNode);
        }

        public ref SegmentEndData GetOrCreate(ushort segmentID, bool startNode) {
            ref SegmentEndData data = ref GetAt(segmentID, startNode);
            if (data == null) {
                ushort nodeID = NetUtil.GetSegmentNode(segmentID, startNode);
                data = new SegmentEndData(segmentID: segmentID, nodeID: nodeID);
                SetAt(segmentID: segmentID, startNode: startNode, data);
            }
            Assertion.AssertNotNull(data);
            return ref data;
        }

        /// <summary>
        /// releases data for <paramref name="segmentID"/> <paramref name="startNode"/> if uncessary. marks segment for update.
        /// </summary>
        public void UpdateData(ushort segmentID, bool startNode) {
            SegmentEndData segEnd = GetAt(segmentID, startNode);
            if (segEnd == null) return;
            if (!NetUtil.IsSegmentValid(segmentID)) {
                ResetSegmentEndToDefault(segmentID, startNode);
                return;
            }
            ushort nodeID = segmentID.ToSegment().GetNode(startNode);
            bool selected = NodeControllerTool.Instance.SelectedNodeID == nodeID;
            if (segEnd.IsDefault() && !selected) {
                ResetSegmentEndToDefault(segmentID, startNode);
            } else {
                segEnd.Update();
            }
        }

        public void ResetSegmentEndToDefault(ushort segmentID, bool startNode) {
            SegmentEndData segEnd = GetAt(segmentID, startNode);
            if (segEnd != null)
                Log.Debug($"segment End:({segmentID},{startNode}) reset to defualt");
            else
                Log.Debug($"segment End:({segmentID},{startNode}) is already null.");
            SetAt(segmentID, startNode, null);
            NetManager.instance.UpdateSegment(segmentID);
        }

        public void UpdateAll() {
            foreach (var segmentEndData in buffer) {
                if (segmentEndData == null) continue;
                if (NetUtil.IsSegmentValid(segmentEndData.SegmentID)) {
                    segmentEndData.Update();
                } else {
                    ResetSegmentEndToDefault(segmentEndData.SegmentID, true);
                    ResetSegmentEndToDefault(segmentEndData.SegmentID, false);
                }
            }
        }

        /// <summary>
        /// Called after stock code and before postfix code.
        /// if node is invalid or otherwise unsupported, it will be set to null.
        /// </summary>
        public void OnBeforeCalculateNodePatch(ushort segmentID, bool startNode) {
            SegmentEndData segEnd = GetAt(segmentID, startNode);
            // nodeID.ToNode still has default flags.
            if (segEnd == null)
                return;
            if (!NodeData.IsSupported(segEnd.NodeID)) {
                SetAt(segmentID, startNode, null);
                return;
            }

            segEnd.Calculate();
        }

        public void Validate(string errorMessage, bool showPanel = true) {
            try {
                Log.Info("SegmentEndManager.Validate() called");
                Assert(buffer[0] == null && buffer[1] == null, "buffer[0] == buffer[1] == null"); ;
                for (int i = 1; i < buffer.Length; ++i) {
                    var data = buffer[i];
                    if (data == null) continue;

                    bool startNode = i % 2 == 0;
                    ushort segmentID = (ushort)UnityEngine.Mathf.FloorToInt(i / 2);
                    ushort nodeID = segmentID.ToSegment().GetNode(startNode);

                    Assert(NetUtil.IsNodeValid(nodeID));
                    Assert(NetUtil.IsSegmentValid(segmentID));

                    if (data.SegmentID != segmentID || data.NodeID !=nodeID) {
                        Assert(!ReferenceEquals(GetAt(data.SegmentID, data.NodeID), GetAt(segmentID,nodeID)),
                            $"!ReferenceEquals(GetAt(data.SegmentID:{data.SegmentID}, data.NodeID:{data.NodeID}), GetAt(segmentID:{segmentID},nodeID:{nodeID})");
                    }
                    AssertEqual(data.NodeID, nodeID, "data.NodeID == nodeID");
                    AssertEqual(data.IsStartNode, startNode, "data.IsStartNode == startNode");
                    AssertEqual(data.SegmentID, segmentID, "data.SegmentID == segmentID");
                }
            } catch(Exception e) {
                Log.Exception(e, m: errorMessage, showInPanel: showPanel);
            }
        }

        public void Heal() {
            Log.Info("SegmentEndManager.Heal() called");
            buffer[0] = buffer[1] = null;
            for (int i = 1; i < buffer.Length; ++i) {
                ref SegmentEndData data = ref buffer[i];
                if (data == null) continue;

                bool startNode = i % 2 == 0;
                ushort segmentID = (ushort)UnityEngine.Mathf.FloorToInt(i / 2);
                ushort nodeID = segmentID.ToSegment().GetNode(startNode);

                if (!NetUtil.IsNodeValid(nodeID) || !NetUtil.IsSegmentValid(segmentID)) {
                    buffer[i] = null;
                    continue;
                }
                if (data.NodeID != nodeID)
                    data.NodeID = nodeID;
                if (data.SegmentID != segmentID)
                    data.SegmentID = segmentID;

                if (data.IsStartNode != startNode)
                    buffer[i] = null;
            }
        }
        public static int CountUpdatingSegments() {
            int ret = 0;
            if (NetManager.instance.m_segmentsUpdated) {
                foreach (var block in NetManager.instance.m_updatedSegments) {
                    ret += EnumBitMaskExtensions.CountOnes(block);
                }
            }
            return ret;
        }
    }
}