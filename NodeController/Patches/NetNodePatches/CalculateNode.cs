namespace NodeController.Patches {
    using ColossalFramework;
    using HarmonyLib;
    using KianCommons;

    [HarmonyPatch(typeof(NetNode), nameof(NetNode.CalculateNode))]
    class CalculateNode {
        static void Postfix(ushort nodeID) {
            NodeManager.Instance.OnBeforeCalculateNodePatch(nodeID); // invalid/unsupported nodes are set to null.
            NodeData nodeData = NodeManager.Instance.buffer[nodeID];
            if (nodeData == null)
                return;

            ref NetNode node = ref nodeID.ToNode();
            bool outside = node.m_flags.IsFlagSet(NetNode.Flags.Outside);
            if (nodeData.SegmentCount == 2 && !outside) {
                if (nodeData.NeedsTransitionFlag()) {
                    node.m_flags |= NetNode.Flags.Transition;
                } else {
                    node.m_flags &= ~NetNode.Flags.Transition;
                }
                if (nodeData.NeedMiddleFlag()) {
                    node.m_flags &= ~(NetNode.Flags.Junction | NetNode.Flags.AsymForward | NetNode.Flags.AsymBackward);
                    node.m_flags |= NetNode.Flags.Middle;
                }
                if (nodeData.NeedBendFlag()) {
                    node.m_flags &= ~(NetNode.Flags.Junction | NetNode.Flags.Middle);
                    node.m_flags |= NetNode.Flags.Bend; // TODO set asymForward and asymBackward
                }
                if (nodeData.NeedJunctionFlag()) {
                    node.m_flags |= NetNode.Flags.Junction;
                    node.m_flags &= ~(NetNode.Flags.Middle | NetNode.Flags.AsymForward | NetNode.Flags.AsymBackward | NetNode.Flags.Bend | NetNode.Flags.End);
                }
                node.m_flags &= ~NetNode.Flags.Moveable;
            }
        } // end postfix
    }
}
