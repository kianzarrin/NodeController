namespace NodeController {
    using KianCommons;
    using NodeController.Tool;
    using System;
    using static KianCommons.Assertion;
    using KianCommons.Serialization;
    using System.Linq;
    using UnityEngine.Networking.Types;

    [Serializable]
    public class NodeManager {
        #region LifeCycle
        public static NodeManager Instance { get; private set; } = new NodeManager();

        public static byte[] Serialize() => SerializationUtil.Serialize(Instance);

        public static void Deserialize(byte[] data, Version version) {
            try {
                Log.Called("data.Length=" + (data?.Length).ToSTR(), version);
                if (data == null) {
                    Instance = new NodeManager();
                } else {
                    Instance = SerializationUtil.Deserialize(data, version) as NodeManager;
                    Assertion.NotNull(Instance);
                    Log.Info($"{ReflectionHelpers.ThisMethod} : {Instance.CustomCount} Custom Nodes");
                }
            } catch(Exception ex) {
                Log.Exception(ex);
                Instance = new NodeManager();
            }
        }

        public void OnLoad() {
            UpdateAll();
        }

        #endregion LifeCycle

        public NodeData[] buffer = new NodeData[NetManager.MAX_NODE_COUNT];

        public int CustomCount => buffer.Count(item => item != null);

        #region MoveIT backward compatiblity.

        [Obsolete("delete when moveit is updated")]
        public static byte[] CopyNodeData(ushort nodeID) =>
            SerializationUtil.Serialize(Instance.buffer[nodeID])
            .LogRet($"NodeManager.CopyNodeData({nodeID}) ->");

        public static ushort TargetNodeID = 0;

        [Obsolete("kept here for backward compatibility with MoveIT")]
        /// <param name="nodeID">target nodeID</param>
        public static void PasteNodeData(ushort nodeID, byte[] data) =>
            Instance.PasteNodeDataImp(nodeID, data);

        [Obsolete("kept here for backward compatibility with MoveIT")]
        /// <param name="nodeID">target nodeID</param>
        private void PasteNodeDataImp(ushort nodeID, byte[] data) {
            Log.Debug($"NodeManager.PasteNodeDataImp(nodeID={nodeID}, data={data})");
            if (data == null) {
                // for backward compatibality reasons its not a good idea to do this:
                // ResetNodeToDefault(nodeID); 
            } else {
                foreach (var segmentID in NetUtil.IterateNodeSegments(nodeID))
                    SegmentEndManager.Instance.GetOrCreate(segmentID: segmentID, nodeID: nodeID);
                TargetNodeID = nodeID; // must be done before deserialization.
                buffer[nodeID] = SerializationUtil.Deserialize(data, this.VersionOf()) as NodeData;
                buffer[nodeID].NodeID = nodeID;
                UpdateData(nodeID);
                TargetNodeID = 0;
            }
        }
        #endregion

        public NodeData InsertNode(NetTool.ControlPoint controlPoint, NodeTypeT nodeType = NodeTypeT.Crossing) {
            if (ToolBase.ToolErrors.None != NetUtil.InsertNode(controlPoint, out ushort nodeID))
                return null;
            Assert(nodeID != 0, "nodeID");

            foreach (var segmentID in NetUtil.IterateNodeSegments(nodeID)) {
                var segEnd = new SegmentEndData(segmentID, nodeID);
                SegmentEndManager.Instance.SetAt(segmentID: segmentID, nodeID: nodeID, value: segEnd);
            }

            var info = controlPoint.m_segment.ToSegment().Info;
            int nPedLanes = info.CountPedestrianLanes();
            bool isRoad = info.m_netAI is RoadBaseAI;
            if (nodeType == NodeTypeT.Crossing && (nPedLanes < 2 || !isRoad))
                buffer[nodeID] = new NodeData(nodeID);
            else
                buffer[nodeID] = new NodeData(nodeID, nodeType);

            return buffer[nodeID];
        }

        public ref NodeData GetOrCreate(ushort nodeID) {
            ref NodeData data = ref Instance.buffer[nodeID];
            if (data == null) {
                data = new NodeData(nodeID);
                buffer[nodeID] = data;
            }

            foreach (var segmentID in NetUtil.IterateNodeSegments(nodeID)) {
                SegmentEndManager.Instance.
                    GetOrCreate(segmentID: segmentID, nodeID: nodeID);
            }

            AssertNotNull(data);
            return ref data;
        }

        /// <summary>
        /// Calls update node. releases data for <paramref name="nodeID"/> if uncessary. 
        /// </summary>
        /// <param name="nodeID"></param>
        public void UpdateData(ushort nodeID) {
            if (nodeID == 0 || buffer[nodeID] == null)
                return;
            bool selected = NodeControllerTool.Instance.SelectedNodeID == nodeID;
            if (buffer[nodeID].IsDefault() && !selected) {
                ResetNodeToDefault(nodeID);
            } else {
                foreach (var segmentID in NetUtil.IterateNodeSegments(nodeID)) {
                    var segEnd = SegmentEndManager.Instance.GetAt(segmentID: segmentID, nodeID: nodeID);
                    segEnd.Update();
                }
                buffer[nodeID].Update();
            }
        }

        public void ResetNodeToDefault(ushort nodeID) {
            if (buffer[nodeID] != null)
                Log.Debug($"node:{nodeID} reset to default");
            else
                Log.Debug($"node:{nodeID} is already null. no need to reset to default");

            SetNullNodeAndSegmentEnds(nodeID);

            // update nearby nodes too to calculate velocity
            NodeManager.UpdateNode(nodeID);
        }

        public void UpdateAll() {
            foreach (var nodeData in buffer) {
                if (nodeData == null) continue;
                if (NetUtil.IsNodeValid(nodeData.NodeID))
                    nodeData.Update();
                else
                    ResetNodeToDefault(nodeData.NodeID);
            }
        }

        public static void UpdateNode(ushort nodeID) {
            // update nearby nodes too to calculate velocity
            NetManager.instance.UpdateNode(nodeID, fromSegment: 0, level: -1);
        }

        /// <summary>
        /// Called after stock code and before postfix code.
        /// if node is invalid or otherwise unsupported, it will be set to null.
        /// </summary>
        public void OnBeforeCalculateNodePatch(ushort nodeID) {
            // nodeID.ToNode still has default flags.
            if (buffer[nodeID] == null) return;

            if (!NetUtil.IsNodeValid(nodeID) || !NodeData.IsSupported(nodeID)) {
                SetNullNodeAndSegmentEnds(nodeID);
                return;
            }

            foreach (var segmentID in NetUtil.IterateNodeSegments(nodeID)) {
                var segEnd = SegmentEndManager.Instance.
                    GetOrCreate(segmentID: segmentID, nodeID: nodeID);
                segEnd.Calculate();
            }

            buffer[nodeID].Calculate();

            if (!buffer[nodeID].CanChangeTo(buffer[nodeID].NodeType)/*.LogRet("CanChangeTo()->")*/) {
                ResetNodeToDefault(nodeID);
            }
        }

        public void SetNullNodeAndSegmentEnds(ushort nodeID) {
            foreach (var segmentID in NetUtil.IterateNodeSegments(nodeID)) {
                SegmentEndManager.Instance.
                    SetAt(segmentID: segmentID, nodeID: nodeID, value: null);
            }
            buffer[nodeID] = null;
        }

        public void Validate(string errorMessage, bool showPanel=true) {
            errorMessage = "\n" + errorMessage;
            Log.Info("NodeManager.Validate() called");
            try {
                Assert(buffer[0] == null, "buffer[0] == null"); ;
                for (ushort nodeID = 1; nodeID < buffer.Length; ++nodeID) {
                    var data = buffer[nodeID];
                    if (data == null) continue;

                    Assert(NetUtil.IsNodeValid(nodeID));
                    if(data.NodeID != nodeID) {
                        Assert(!ReferenceEquals(buffer[data.NodeID], buffer[nodeID]),
                            $"!ReferenceEquals(buffer[data.NodeID:{data.NodeID}], buffer[nodeID:{nodeID}]" + errorMessage);
                    }
                    AssertEqual(data.NodeID, nodeID, "data.NodeID == nodeID" + errorMessage);
                }
            }catch(Exception e) {
                Log.Exception(e, showInPanel: showPanel);
            }

        }

        public void Heal() {
            Log.Info("NodeManager.Validate() heal");
            buffer[0] = null;
            for(ushort nodeID=1; nodeID < buffer.Length; ++nodeID) {
                var data = buffer[nodeID];
                if (data == null) continue;
                if (!NetUtil.IsNodeValid(nodeID)) {
                    SetNullNodeAndSegmentEnds(nodeID);
                    continue;
                }
                if (buffer[nodeID].NodeID != nodeID) 
                    buffer[nodeID].NodeID = nodeID;
            }
        }

        /// <param name="showError1">set true to display error panel before healing</param>
        public static void ValidateAndHeal(bool showError1) {
            string m1 = "Node Controller tries to recover from the error";
            Instance.Validate(m1, showError1);
            SegmentEndManager.Instance.Validate(m1, showError1);

            Instance.Heal();
            SegmentEndManager.Instance.Heal();

            string m2 = "Node Controller error: please report this bug and submit NodeController.log";
            Instance.Validate(m2, true);
            SegmentEndManager.Instance.Validate(m2, true);
        }

        public static int CountUpdatingNodes() {
            int ret = 0;
            if (NetManager.instance.m_nodesUpdated) {
                foreach (var block in NetManager.instance.m_updatedNodes) {
                    ret += EnumBitMaskExtensions.CountOnes(block);
                }
            }
            return ret;
        }
    }
}
