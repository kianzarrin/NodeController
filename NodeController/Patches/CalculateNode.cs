using ColossalFramework;
using HarmonyLib;


namespace NodeController.Patches {
    using Util;
    [HarmonyPatch(typeof(NetNode), nameof(NetNode.CalculateNode))]
    class CalculateNode {
        static void Postfix(ref NetNode __instance) {
            //Log.Debug("CalculateNode.PostFix() was called");
            ushort nodeID = NetUtil.GetID(__instance);
            if (!NetUtil.IsNodeValid(nodeID)) return;

            NodeManager.Instance.OnBeforeCalculateNode(nodeID);

            NodeData nodeData = NodeManager.Instance.buffer[nodeID];

            if (nodeData == null || nodeData.SegmentCount != 2)
                return;
            if (__instance.m_flags.IsFlagSet(NetNode.Flags.Outside))
                return;
          
            if (nodeData.NeedsTransitionFlag()) {
                __instance.m_flags |= NetNode.Flags.Transition;
            } else {
                __instance.m_flags &= ~NetNode.Flags.Transition;
            }
            if (nodeData.NeedMiddleFlag()) {
                __instance.m_flags &= ~(NetNode.Flags.Junction | NetNode.Flags.AsymForward | NetNode.Flags.AsymBackward);
                __instance.m_flags |= NetNode.Flags.Middle;
            }
            if (nodeData.NeedBendFlag()) {
                __instance.m_flags &= ~(NetNode.Flags.Junction | NetNode.Flags.Middle);
                __instance.m_flags |= NetNode.Flags.Bend; // TODO set asymForward and asymBackward
            }
            if (nodeData.NeedJunctionFlag() ) {
                __instance.m_flags |= NetNode.Flags.Junction;
                __instance.m_flags &= ~(NetNode.Flags.Middle | NetNode.Flags.AsymForward | NetNode.Flags.AsymBackward | NetNode.Flags.Bend | NetNode.Flags.End);
            }
            __instance.m_flags &= ~NetNode.Flags.Moveable;
        } // end postfix
    }
}