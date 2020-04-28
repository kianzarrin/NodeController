using ColossalFramework;
using HarmonyLib;


namespace BlendRoadManager.Patches {
    using Util;
    [HarmonyPatch(typeof(NetNode), nameof(NetNode.CalculateNode))]
    class CalculateNode {
        static void Postfix(ref NetNode __instance) {
            ushort nodeID = NetUtil.GetID(__instance);
            NodeData blendData = NodeManager.Instance.buffer[nodeID];
            if (blendData == null)
                return;


            if (__instance.m_flags.IsFlagSet(NetNode.Flags.Outside)) {
                // Do nothing
            } else {
                if (blendData.NeedsTransitionFlag()) {
                    __instance.m_flags |= NetNode.Flags.Transition;
                } else {
                    __instance.m_flags &= ~NetNode.Flags.Transition;
                }

                if (
                __instance.m_flags.IsFlagSet(NetNode.Flags.Junction) &&
                blendData.SegmentCount == 2)
                //NetNode.BlendJunction(nodeID) )
                {
                    if (blendData.NeedMiddleFlag()) {
                        __instance.m_flags &= ~NetNode.Flags.Junction;
                        __instance.m_flags |= NetNode.Flags.Middle; // TODO set asymForward and asymBackward
                    } else if (blendData.NeedBendFlag()) {
                        __instance.m_flags &= ~NetNode.Flags.Junction;
                        __instance.m_flags |= NetNode.Flags.Bend; // TODO set asymForward and asymBackward
                    }
                } else if (__instance.m_flags.IsFlagSet(NetNode.Flags.Middle | NetNode.Flags.Bend)) {
                    if (blendData.NeedJunctionFlag()) {
                        __instance.m_flags |= NetNode.Flags.Junction;
                        __instance.m_flags &= ~(NetNode.Flags.Moveable | NetNode.Flags.Middle | NetNode.Flags.AsymForward | NetNode.Flags.AsymBackward | NetNode.Flags.Bend | NetNode.Flags.End);
                    }
                }
            }
        } // end postfix
    }
}