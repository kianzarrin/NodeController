using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlendRoadManager
{
    using Util;

    [Serializable]
    public class NodeBlendManager
    {
        public static NodeBlendManager Instance { get; private set; } = new NodeBlendManager();

        public NodeBlendData[] buffer = new NodeBlendData[NetManager.MAX_NODE_COUNT];
        
        public NodeBlendData Get(ref NetNode node)
        {
            ushort nodeID = Util.NetUtil.GetID(node);
            return buffer[nodeID];
        }
    }

    public enum BlendType {
        NoBlending,
        Crossing, // crossing is shorter than Sharp
        Sharp, 
        LaneBasedWidth,
        UTurn,
        CustomWidth
    }

    [Serializable]
    public class NodeBlendData
    {
        public BlendType type;

        public float customWidth = 0;

        public bool NeedMiddleFlag() => type == BlendType.NoBlending;
        public bool NeedJunctionFlag() => !NeedMiddleFlag();
        public bool NeedsTrafficLight() => NeedJunctionFlag();

        /// <summary>
        /// in case of overflow resets type and return true.
        /// </summary>
        public bool IncrementType()
        {
            BlendType max = HelpersExtensions.GetMaxEnumValue<BlendType>();
            type++;
            if (type>max)
            {
                type = 0;
                return true; // overflow
            }
            return false;
        }
    }
}
