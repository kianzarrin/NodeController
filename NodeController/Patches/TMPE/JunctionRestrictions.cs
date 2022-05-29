using ColossalFramework;
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
                UpdateFlag(args, JunctionRestrictionsFlags.AllowUTurn, nodeData.GetDefaultUturnAllowed);
                UpdateFlag(args, JunctionRestrictionsFlags.AllowPedestrianCrossing, nodeData.GetDefaultPedestrianCrossingAllowed);
                UpdateFlag(args, JunctionRestrictionsFlags.AllowEnterWhenBlocked, nodeData.GetDefaultEnteringBlockedJunctionAllowed);
            }
        }

        private void GetConfigurableHook(FlagsHookArgs args) {
            if (NodeManager.Instance.TryGet(args.SegmentId, args.StartNode, out var nodeData)) {
                UpdateFlag(args, JunctionRestrictionsFlags.AllowUTurn, nodeData.IsUturnAllowedConfigurable);
                UpdateFlag(args, JunctionRestrictionsFlags.AllowPedestrianCrossing, nodeData.IsPedestrianCrossingAllowedConfigurable);
                UpdateFlag(args, JunctionRestrictionsFlags.AllowEnterWhenBlocked, nodeData.IsEnteringBlockedJunctionAllowedConfigurable);
            }
        }

        public static void UpdateFlag(FlagsHookArgs args, JunctionRestrictionsFlags flag, Func<bool?> func) {
            if (args.Mask.IsFlagSet(flag)) {
                var value = func();
                if (value.HasValue)
                    args.Result = args.Result.SetFlags(flag, value.Value);
            }
        }
    }
}
