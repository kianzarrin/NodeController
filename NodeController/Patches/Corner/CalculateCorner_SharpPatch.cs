namespace NodeController.Patches.Corner;
using HarmonyLib;
using KianCommons;
using KianCommons.Patches;
using KianCommons.Plugins;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

[HarmonyPatch]
static class CalculateCorner_SharpPatch {
    static void Prepare(MethodBase method) {
        if (method == null) {
            AdaptiveRoadsUtil.OverrideARSharpner(true);
        }
    }

    static void Cleanup(MethodBase method) {
        if (method == null) {
            AdaptiveRoadsUtil.OverrideARSharpner(false);
        }
    }

    static MethodBase TargetMethod() {
        // public void CalculateCorner(ushort segmentID, bool heightOffset, bool start, bool leftSide,
        // out Vector3 cornerPos, out Vector3 cornerDirection, out bool smooth)
        return typeof(NetSegment).GetMethod(
                nameof(NetSegment.CalculateCorner),
                BindingFlags.Public | BindingFlags.Static) ??
                throw new System.Exception("CalculateCornerPatch Could not find target method.");
    }

    delegate float Max(float a, float b);

    static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions, MethodBase original) {
        var codes = instructions.ToList();
        MethodInfo mMax = typeof(Mathf).GetMethod<Max>(throwOnError: true);
        MethodInfo mModifySharpness = typeof(CalculateCorner_SharpPatch).GetMethod(nameof(ModifySharpness), throwOnError: true);

        int iLoadSharpness = codes.Search(c => c.LoadsConstant(2f), count: 3);

        codes.InsertInstructions(iLoadSharpness + 1, new[] {
            // 2 is already on the stack
            TranspilerUtils.GetLDArg(original, "ignoreSegmentID"),
            TranspilerUtils.GetLDArg(original, "startNodeID"),
            new CodeInstruction(OpCodes.Call, mModifySharpness),
        });

        return codes;

    }

    public static float ModifySharpness(float sharpness, ushort segmentId, ushort nodeId) {
        var data = SegmentEndManager.Instance.GetAt(segmentID: segmentId, nodeID: nodeId);
        bool sharp = data?.SharpCorners ?? nodeId.ToNode().Info.GetARSharpCorners();
        if (sharp) {
            const float OFFSET_SAFETYNET = 0.02f;
            sharpness = OFFSET_SAFETYNET;
        }
        return sharpness;
    }

}