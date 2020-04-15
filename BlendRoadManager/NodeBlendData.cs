using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework;

namespace BlendRoadManager
{
    using Util;

    [Serializable]
    public class NodeBlendManager
    {
        public static NodeBlendManager Instance { get; private set; } = new NodeBlendManager();

        public NodeBlendData[] buffer = new NodeBlendData[NetManager.MAX_NODE_COUNT];
        
        public NodeBlendData GetOrCreate(ushort nodeID)
        {
            NodeBlendData data = NodeBlendManager.Instance.buffer[nodeID];
            if (data == null)
            {
                data = new NodeBlendData(nodeID);
                NodeBlendManager.Instance.buffer[nodeID] = data;
            }
            return data;
        }
        
        public void ChangeNode(ushort nodeID)
        {
            NodeBlendData data = GetOrCreate(nodeID);
            
            if (data.nodeType == NodeType.Middle)
            {
                data.nodeType = NodeType.Blend;
            } else {
                data.textureType++;
                if( data.textureType > HelpersExtensions.GetMaxEnumValue<TextureType>() )
                {
                    data.textureType = TextureType.Node;
                    data.nodeType = NodeType.Middle;
                }
            }

            if (data.IsDefault())
            {
                Instance.buffer[nodeID] = null;
            }

            NetManager.instance.UpdateNode(nodeID);
        }


        public void ChangeJunctionOffset(ushort nodeID)
        {
            if (!nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Junction)) return;
            NodeBlendData data = NodeBlendManager.Instance.buffer[nodeID];
            data.IncrementOffset();
            NetManager.instance.UpdateNode(nodeID);
        }
    }

    public enum NodeType {
        Middle,
        Blend,
    }

    public enum TextureType
    {
        Node=0, // default
        Corssing, // change dataMatrix.w to render crossings in the middle.
        Segment,
    }

    [Serializable]
    public class NodeBlendData
    {
        public ushort NodeID;
        public int SegmentCount;

        public float DefaultCornerOffset => NodeID.ToNode().Info.m_minCornerOffset;
        public NetNode.Flags DefaultFlags;

        public NodeType nodeType;
        public TextureType textureType;
        public float CornerOffset;


        public NodeBlendData(ushort nodeID) {
            NodeID = nodeID;
            SegmentCount = nodeID.ToNode().CountSegments();
            
            var DefaultFlags = nodeID.ToNode().m_flags;
            if (DefaultFlags.IsFlagSet(NetNode.Flags.Middle))
                nodeType = NodeType.Middle;
            else if (DefaultFlags.IsFlagSet(NetNode.Flags.Junction))
                nodeType = NodeType.Blend;

            textureType = TextureType.Node;

            CornerOffset = DefaultCornerOffset;
        }

        public bool IsDefault()
        {
            bool ret = CornerOffset - DefaultCornerOffset < 0.5f;
            ret &= DefaultFlags.IsFlagSet(NetNode.Flags.Middle) == (nodeType == NodeType.Middle);
            ret &= textureType == TextureType.Node;
            return ret;
        }


        public bool NeedMiddleFlag() => nodeType == NodeType.Middle;
        public bool NeedJunctionFlag() => !NeedMiddleFlag();
        public bool NeedsTrafficLight() => NeedJunctionFlag() && textureType != TextureType.Segment;

        /// <summary>
        /// in case of overflow resets type and return true.
        /// </summary>
        public void IncrementOffset()
        {
            CornerOffset++;
            if (CornerOffset > 10)
                CornerOffset = 1;
        }
    }
}
