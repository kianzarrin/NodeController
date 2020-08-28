using ColossalFramework;
using HarmonyLib;


namespace NodeController.Patches {
    using KianCommons;
    [HarmonyPatch(typeof(NetNode), nameof(NetNode.CalculateNode))]
    class CalculateNode {
        // TODO: do this inside a transpiler.
        // insert function call before this.m_Flags is assigned :

        /* if ((flags & NetNode.Flags.Outside) != NetNode.Flags.None)
         *     this.m_flags = flags;

        IL_05fa: ldarg.0      // this
        IL_05fb: ldfld        valuetype NetNode/Flags NetNode::m_flags
        IL_0600: ldc.i4       -1610613233 // 0x9ffffe0f
        IL_0605: and
        IL_0606: stloc.s      flags

        IL_0665: ldarg.0      // this
        IL_0666: ldloc.s      flags
        IL_0668: ldc.i4.s     64 // 0x40
        IL_066a: or
        IL_066b: stfld        valuetype NetNode/Flags NetNode::m_flags
        */

        static void Postfix(ref NetNode __instance) {
            //Log.Debug("CalculateNode.PostFix() was called");
            ushort nodeID = NetUtil.GetID(__instance);
            if (!NetUtil.IsNodeValid(nodeID)) return;

            NodeManager.Instance.OnBeforeCalculateNodePatch(nodeID);

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
            if (nodeData.NeedJunctionFlag()) {
                __instance.m_flags |= NetNode.Flags.Junction;
                __instance.m_flags &= ~(NetNode.Flags.Middle | NetNode.Flags.AsymForward | NetNode.Flags.AsymBackward | NetNode.Flags.Bend | NetNode.Flags.End);
            }
            __instance.m_flags &= ~NetNode.Flags.Moveable;
        } // end postfix
    }
}
