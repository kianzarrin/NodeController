namespace NodeController.Patches {
    using HarmonyLib;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using TrafficManager.Util.Extensions;
    using UnityEngine;
    using NodeController;
    [HarmonyPatch(typeof(CitizenAI), "GetPathTargetPosition")]
    static class GetPathTargetPositionPatch {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            foreach (var instruction in instructions) {
                yield return instruction;
                if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float value && value == 64) {
                    yield return new CodeInstruction(OpCodes.Ldloc, 4);
                    yield return new CodeInstruction(OpCodes.Call, mGetGap_);
                }
            }
        }

        private static MethodInfo mGetGap_ =>
            AccessTools.Method(typeof(GetPathTargetPositionPatch), nameof(GetPathTargetPositionPatch.GetGap));

        private static float GetGap(float gap, PathUnit.Position pathPos) {
            ref var segment = ref pathPos.m_segment.ToSegment();
            bool startNode = pathPos.m_offset == 0;
            var nodeId = segment.GetNodeId(startNode);
            NodeData nodeData = NodeManager.Instance.buffer[nodeId];
            return nodeData?.Gap ?? Mathf.Max(64f, segment.Info.Gap());
        }

        private static float Gap(this NetInfo info) => Mathf.Max(info.m_minCornerOffset, info.m_halfWidth * 2);
        
    }
}
