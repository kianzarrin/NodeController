namespace NodeController.Patches {
    using HarmonyLib;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using UnityEngine;
    using NodeController;
    using KianCommons;

    [HarmonyPatch(typeof(CitizenAI), "GetPathTargetPosition")]
    static class GetPathTargetPositionPatch {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            int replacements = 0;
            foreach (var instruction in instructions) {
                if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float value && value == 128) {
                    replacements++;
                    yield return new CodeInstruction(OpCodes.Ldloc, 4);
                    yield return new CodeInstruction(OpCodes.Call, mGetGap_);
                } else {
                    yield return instruction;
                }
            }
            Assertion.GT(replacements, 0, "no replacements could be made.");
        }

        private static MethodInfo mGetGap_ =>
            AccessTools.Method(typeof(GetPathTargetPositionPatch), nameof(GetPathTargetPositionPatch.GetGap));

        private static float GetGap(PathUnit.Position pathPos) {
            ref var segment = ref pathPos.m_segment.ToSegment();
            bool startNode = pathPos.m_offset == 0;
            var nodeId = segment.GetNode(startNode);
            NodeData nodeData = NodeManager.Instance.buffer[nodeId];
            return nodeData?.Gap ?? Mathf.Max(64f, segment.Info.Gap());
        }

        private static float Gap(this NetInfo info) => Mathf.Max(info.m_minCornerOffset, info.m_halfWidth * 2);
        
    }
}
