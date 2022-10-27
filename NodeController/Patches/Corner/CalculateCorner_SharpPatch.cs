namespace NodeController.Patches.Corner; 
using ColossalFramework;
using HarmonyLib;
using KianCommons;
using KianCommons.Patches;
using System.Reflection;
using UnityEngine;
using KianCommons.Plugins;
using ColossalFramework.Math;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

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
        MethodInfo mMax = typeof(Mathf).GetMethod<Max>(throwOnError:true);
        MethodInfo mModifySharpness = typeof(CalculateCorner_SharpPatch).GetMethod(nameof(ModifySharpness), throwOnError:true);

        bool LoadsSharpnessPredicate(int i) => codes[i].LoadsConstant(2f) && codes[i+1].Calls(mMax);
        int iLoadSharpness = codes.Search( LoadsSharpnessPredicate);

        codes.InsertInstructions(iLoadSharpness + 1, new[] {
            // 2 is already on the stack
            TranspilerUtils.GetLDArg(original, "ignoreSegmentID"),
            TranspilerUtils.GetLDArg(original, "startNodeID"),
            new CodeInstruction(OpCodes.Call, mModifySharpness),
        });

        return codes;

    }

    public static float ModifySharpness(float sharpness, ushort segmentId, ushort nodeId) {
        //var data = SegmentEndManager.Instance.GetAt(segmentId, nodeId);
        var data = SegmentEndManager.Instance.GetAt(segmentID: segmentId, nodeID: nodeId);
        if (data != null && data.SharpCorners) {
            sharpness = 0.1f;
        }
        return sharpness;
    }

}