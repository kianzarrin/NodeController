using HarmonyLib;
using KianCommons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TrafficManager.API.Traffic.Enums;
using static TrafficManager.API.Hook.IJunctionRestrictionsHook;
using static TrafficManager.API.Implementations;

namespace NodeController.Patches.TMPE {

    [HarmonyPatch]
    internal class JunctionRestrictions {

        public const JunctionRestrictionFlags impactedFlags = JunctionRestrictionFlags.AllowPedestrianCrossing
                                                                | JunctionRestrictionFlags.AllowUTurn
                                                                | JunctionRestrictionFlags.AllowEnterWhenBlocked;

        public static JunctionRestrictions Instance = new JunctionRestrictions();

        private JunctionRestrictions() {
            try {
                var junctionRestrictionsHook = HookFactory.JunctionRestrictionsHook;
                junctionRestrictionsHook.GetDefaultsHook += GetDefaultsHook;
                junctionRestrictionsHook.GetConfigurableHook += GetConfigurableHook;
                Log.Info("TMPE API hooks added");
            }
            catch (Exception ex) {
                Log.Error($"Failed to add TMPE API hooks: {ex}");
                Log.Info(ex.StackTrace);
            }
        }

        private void GetDefaultsHook(FlagsHookArgs args) {
            if (NodeManager.Instance.TryGet(args.SegmentId, args.StartNode, out var nodeData)) {
                GetDefaultsHook(nodeData, args);
            }
        }

        private void GetConfigurableHook(FlagsHookArgs args) {
            if (NodeManager.Instance.TryGet(args.SegmentId, args.StartNode, out var nodeData)) {
                GetConfigurableHook(nodeData, args);
            }
        }

        private void GetDefaultsHook(NodeData nodeData, FlagsHookArgs args) {

            switch (nodeData.NodeType) {
                case NodeTypeT.Crossing:
                    args.Result |= JunctionRestrictionFlags.AllowPedestrianCrossing;
                    args.Result &= ~(JunctionRestrictionFlags.AllowEnterWhenBlocked | JunctionRestrictionFlags.AllowUTurn);
                    break;

                case NodeTypeT.UTurn:
                    args.Result |= JunctionRestrictionFlags.AllowUTurn;
                    args.Result &= ~JunctionRestrictionFlags.AllowPedestrianCrossing;
                    break;

                case NodeTypeT.Stretch:
                    args.Result |= JunctionRestrictionFlags.AllowEnterWhenBlocked;
                    args.Result &= ~(JunctionRestrictionFlags.AllowPedestrianCrossing | JunctionRestrictionFlags.AllowUTurn);
                    break;

                case NodeTypeT.Nodeless:
                    args.Result &= ~(JunctionRestrictionFlags.AllowPedestrianCrossing | JunctionRestrictionFlags.AllowUTurn);
                    break;

                case NodeTypeT.Bend:
                    args.Result &= ~JunctionRestrictionFlags.AllowPedestrianCrossing;
                    break;

                case NodeTypeT.Custom: {
                        if ((args.Mask & JunctionRestrictionFlags.AllowPedestrianCrossing) != 0 && nodeData.SegmentCount == 2) {

                            var netAI1 = nodeData.SegmentEnd1.SegmentID.ToSegment().Info.m_netAI;
                            var netAI2 = nodeData.SegmentEnd2.SegmentID.ToSegment().Info.m_netAI;
                            bool sameAIType = netAI1.GetType() == netAI2.GetType();

                            if (!sameAIType)
                                args.Result &= ~JunctionRestrictionFlags.AllowPedestrianCrossing;
                        }

                        if (nodeData.SegmentCount <= 2) {
                            args.Result |= JunctionRestrictionFlags.AllowEnterWhenBlocked;
                        }
                        break;
                    }

                case NodeTypeT.End:
                    break;

                default:
                    throw new Exception("Unreachable code");
            }
        }

        private void GetConfigurableHook(NodeData nodeData, FlagsHookArgs args) {

            switch (nodeData.NodeType) {
                case NodeTypeT.Crossing:
                    args.Result &= ~(JunctionRestrictionFlags.AllowPedestrianCrossing | JunctionRestrictionFlags.AllowUTurn);
                    break;

                case NodeTypeT.UTurn:
                    args.Result &= ~JunctionRestrictionFlags.AllowPedestrianCrossing;
                    break;

                case NodeTypeT.Stretch:
                case NodeTypeT.Nodeless:
                case NodeTypeT.Bend:
                    args.Result &= ~(JunctionRestrictionFlags.AllowEnterWhenBlocked
                                    | JunctionRestrictionFlags.AllowPedestrianCrossing
                                    | JunctionRestrictionFlags.AllowUTurn);
                    break;

                case NodeTypeT.Custom: {

                        if (nodeData.SegmentCount == 2 && !nodeData.HasPedestrianLanes)
                            args.Result &= ~JunctionRestrictionFlags.AllowPedestrianCrossing;

                        if ((args.Mask & JunctionRestrictionFlags.AllowEnterWhenBlocked) != 0) {

                            bool oneway = nodeData.DefaultFlags.IsFlagSet(NetNode.Flags.OneWayIn) & nodeData.DefaultFlags.IsFlagSet(NetNode.Flags.OneWayOut);
                            if (oneway & !nodeData.HasPedestrianLanes)
                                args.Result &= ~JunctionRestrictionFlags.AllowEnterWhenBlocked;
                        }
                        break;
                    }

                case NodeTypeT.End:
                    break;

                default:
                    throw new Exception("Unreachable code");
            }
        }

    }
}
