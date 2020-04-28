using ColossalFramework;
using System;


namespace BlendRoadManager {
    using System.IO;
    using System.Runtime.Serialization.Formatters;
    using System.Runtime.Serialization.Formatters.Binary;
    using UnityEngine;
    using Util;
    using TernaryBool = CSUtil.Commons.TernaryBool;
    using TrafficManager.Manager.Impl;

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
            //Instance.UpdateAllNodes();
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
            if (buffer[nodeID].IsDefault()) {
                Log.Info($"node reset to defualt");
                buffer[nodeID] = null;
                NetManager.instance.UpdateNode(nodeID);
            } else {
                buffer[nodeID].Refresh();
            }
        }

        public void UpdateAllNodes() {
            foreach(var blendData in buffer)
                blendData?.Refresh();
        }


        //public void ChangeNode(ushort nodeID) {
        //    Log.Info($"ChangeNode({nodeID}) called");
        //    NodeBlendData data = GetOrCreate(nodeID);
        //    data.ChangeNodeType();
        //    Instance.buffer[nodeID] = data;
        //    RefreshData(nodeID);
        //}


        //public void ChangeOffset(ushort nodeID) {
        //    Log.Info($"ChangeOffset({nodeID}) called");
        //    if (!nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Junction)) {
        //        Log.Info($"Not a junction");
        //        return;
        //    }

        //    NodeBlendData data = GetOrCreate(nodeID);
        //    if (!data.CanModifyOffset())
        //        return;

        //    data.IncrementOffset();
        //    Instance.buffer[nodeID] = data;
        //    RefreshData(nodeID);
        //}
    }

    public enum NodeTypeT {
        Middle,
        Crossing, // change dataMatrix.w to render crossings in the middle.
        UTurn, // set offset to 5.
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

        public float HWDiff;
        public bool HasPedestrianLanes;

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

            if (SegmentCount == 2) {
                float hw0 = 0;
                foreach (ushort segmetnID in NetUtil.GetSegmentsCoroutine(nodeID)) {
                    if (hw0 == 0)
                        hw0 = segmetnID.ToSegment().Info.m_halfWidth;
                    else
                        HWDiff = Mathf.Abs(segmetnID.ToSegment().Info.m_halfWidth - hw0);
                }
            }

            foreach (ushort segmetnID in NetUtil.GetSegmentsCoroutine(nodeID))
                HasPedestrianLanes |= segmetnID.ToSegment().Info.m_hasPedestrianLanes;
        }

        public bool IsDefault() {
            bool ret = CornerOffset - DefaultCornerOffset < 0.5f;
            ret &= NodeType == DefaultNodeType;
            return ret;
        }

        public void Refresh() {
            if (!CanModifyOffset()) {
                if (NodeType == NodeTypeT.UTurn)
                    CornerOffset = 8f;
                else
                    CornerOffset = DefaultCornerOffset;
            }
            NetManager.instance.UpdateNode(NodeID);
        }

        public bool NeedMiddleFlag() => NodeType == NodeTypeT.Middle;
        public bool NeedJunctionFlag() => !NeedMiddleFlag();
        public bool WantsTrafficLight() => NodeType == NodeTypeT.Node || NodeType == NodeTypeT.Crossing;
        public bool CanModifyOffset() => NodeType == NodeTypeT.Segment || NodeType == NodeTypeT.Node;
        public bool NeedsTransitionFlag() => SegmentCount == 2 && NodeType != NodeTypeT.Segment && NodeType != NodeTypeT.Middle;

        public static bool IsSupported(ushort nodeID) {
            var flags = nodeID.ToNode().m_flags;
            if (flags.IsFlagSet(NetNode.Flags.LevelCrossing | NetNode.Flags.Bend))
                return false;
            int n = nodeID.ToNode().CountSegments();
            if (n > 2) return true;
            if (n == 2) return nodeID.ToNode().Info.m_netAI is RoadBaseAI;
            return false;
        }

        public bool CanChangeTo(NodeTypeT newNodeType) {
            if (SegmentCount > 2)
                return newNodeType == NodeTypeT.Node;

            switch (newNodeType) {
                case NodeTypeT.Crossing:
                    return HasPedestrianLanes;
                case NodeTypeT.UTurn:
                    return NodeID.ToNode().Info.m_forwardVehicleLaneCount > 0 && NodeID.ToNode().Info.m_backwardVehicleLaneCount > 0;
                case NodeTypeT.Segment:
                    return !DefaultFlags.IsFlagSet(NetNode.Flags.Middle); // not middle by default.
                case NodeTypeT.Middle:
                    return true;// HWDiff < 2f; // TODO options
                case NodeTypeT.Node:
                    return true;
                default:
                    throw new Exception("Unreachable code");
            }
        }

        #region External Mods
        public TernaryBool ShouldHideCrossingTexture() {
            switch (NodeType) {
                case NodeTypeT.Crossing:
                    return TernaryBool.False; // always show
                case NodeTypeT.UTurn:
                    return TernaryBool.True; // allways hide
                case NodeTypeT.Segment:
                    return TernaryBool.False; // always don't modify
                case NodeTypeT.Middle:
                    return TernaryBool.Undefined; // don't care
                case NodeTypeT.Node:
                    return TernaryBool.Undefined; // default
                default:
                    throw new Exception("Unreachable code");
            }
        }

        // undefined -> don't touch prev value
        // true -> force true
        // false -> force false.
        public TernaryBool IsUturnAllowedConfigurable() {
            switch (NodeType) {
                case NodeTypeT.Crossing:
                    return TernaryBool.False; // always off
                case NodeTypeT.UTurn:
                    return TernaryBool.Undefined; // default on
                case NodeTypeT.Segment:
                    return TernaryBool.False; // always off
                case NodeTypeT.Middle:
                    return TernaryBool.Undefined; // don't care
                case NodeTypeT.Node:
                    return TernaryBool.Undefined; // default
                default:
                    throw new Exception("Unreachable code");
            }
        }

        public TernaryBool GetDefaultUturnAllowed() {
            switch (NodeType) {

                case NodeTypeT.Crossing:
                    return TernaryBool.False; // always off
                case NodeTypeT.UTurn:
                    return TernaryBool.True; // default on
                case NodeTypeT.Segment:
                    return TernaryBool.False; // always off
                case NodeTypeT.Middle:
                    return TernaryBool.Undefined; // don't care
                case NodeTypeT.Node:
                    return TernaryBool.Undefined; // default
                default:
                    throw new Exception("Unreachable code");
            }
        }

        public TernaryBool IsPedestrianCrossingAllowedConfigurable() {
            switch (NodeType) {
                case NodeTypeT.Crossing:
                    return TernaryBool.False; // always on
                case NodeTypeT.UTurn:
                    return TernaryBool.False; // always off
                case NodeTypeT.Segment:
                    return TernaryBool.False; // always off
                case NodeTypeT.Middle:
                    return TernaryBool.Undefined;
                case NodeTypeT.Node:
                    if (!HasPedestrianLanes) {
                        return TernaryBool.False; // TODO move to TMPE.
                    }
                    return TernaryBool.Undefined; // default off
                default:
                    throw new Exception("Unreachable code");
            }
        }

        public TernaryBool GetDefaultPedestrianCrossingAllowed() {
            switch (NodeType) {
                case NodeTypeT.Crossing:
                    return TernaryBool.True; // always on
                case NodeTypeT.UTurn:
                    return TernaryBool.False; // default off
                case NodeTypeT.Segment:
                    return TernaryBool.False; // always off
                case NodeTypeT.Middle:
                    return TernaryBool.Undefined; // don't care
                case NodeTypeT.Node:
                    return TernaryBool.False; // default off
                default:
                    throw new Exception("Unreachable code");
            }
        }

        public TernaryBool IsEnteringBlockedJunctionAllowedConfigurable() {
            switch (NodeType) {
                case NodeTypeT.Crossing:
                    return TernaryBool.Undefined; // default off
                case NodeTypeT.UTurn:
                    return TernaryBool.Undefined; // default
                case NodeTypeT.Segment:
                    return TernaryBool.False; // always on
                case NodeTypeT.Middle:
                    return TernaryBool.Undefined; // don't care
                case NodeTypeT.Node:
                    if (SegmentCount > 2)
                        return TernaryBool.Undefined;
                    bool oneway = DefaultFlags.IsFlagSet(NetNode.Flags.OneWayIn) & DefaultFlags.IsFlagSet(NetNode.Flags.OneWayOut);
                    if(oneway & !HasPedestrianLanes) {
                        return TernaryBool.False; // always on.
                    }
                    return TernaryBool.Undefined;
                default:
                    throw new Exception("Unreachable code");
            }
        }

        public TernaryBool GetDefaultEnteringBlockedJunctionAllowed() {
            switch (NodeType) {
                case NodeTypeT.Crossing:
                    return TernaryBool.False; // default off
                case NodeTypeT.UTurn:
                    return TernaryBool.Undefined; // default
                case NodeTypeT.Segment:
                    return TernaryBool.True; // always on
                case NodeTypeT.Middle:
                    return TernaryBool.Undefined; // don't care
                case NodeTypeT.Node:
                    if (SegmentCount > 2)
                        return TernaryBool.Undefined;
                    bool oneway = DefaultFlags.IsFlagSet(NetNode.Flags.OneWayIn) & DefaultFlags.IsFlagSet(NetNode.Flags.OneWayOut);
                    if (oneway & !HasPedestrianLanes) {
                        return TernaryBool.True; // always on.
                    }
                    return TernaryBool.Undefined;
                default:
                    throw new Exception("Unreachable code");
            }
        }
        #endregion

        #region old code
        //public void ChangeNodeType() {
        //    if (!CanModifyNodeType()) {
        //        throw new Exception("cannot change junction type");
        //    } else if (DefaultNodeType == NodeTypeT.Node) {
        //        switch (NodeType) {
        //            case NodeTypeT.Node :
        //                NodeType = NodeTypeT.Middle ;
        //                break;
        //            case NodeTypeT.Middle:
        //                NodeType = NodeTypeT.Crossing ;
        //                break;
        //            case NodeTypeT.Crossing:
        //                NodeType = NodeTypeT.Segment ;
        //                break;
        //            case NodeTypeT.Segment:
        //                NodeType = NodeTypeT.Node ;
        //                break;
        //            default:
        //                throw new Exception("Unreachable code");
        //        }
        //    }
        //    if (DefaultNodeType == NodeTypeT.Middle) {
        //        NodeType++;
        //        if (NodeType > HelpersExtensions.GetMaxEnumValue<NodeTypeT>())
        //            NodeType = 0;
        //    }
        //}

        //public const float OFFSET_STEP = 5f;
        ///// <summary>
        ///// in case of overflow resets type and return true.
        ///// </summary>
        //public void IncrementOffset() {
        //    CornerOffset += OFFSET_STEP;
        //    if (CornerOffset > OFFSET_STEP * 10)
        //        CornerOffset = 0;
        //}
        #endregion old code
    }
}
