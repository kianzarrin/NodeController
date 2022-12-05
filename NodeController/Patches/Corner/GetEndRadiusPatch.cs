namespace NodeController.Patches.Corner;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using KianCommons;
using KianCommons.Patches;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using System;
// TODO should end rad influence mesh?

#if DEBUG
// test
//[HarmonyPatch]
static class GetEndRadiusPatch {
    // public virtual float GetEndRadius()
    static IEnumerable<MethodBase> TargetMethods() {
        yield return typeof(RoadBaseAI).GetMethod(nameof(RoadBaseAI.GetEndRadius));
        yield return typeof(NetAI).GetMethod(nameof(RoadBaseAI.GetEndRadius));
        yield return typeof(PedestrianPathAI).GetMethod(nameof(RoadBaseAI.GetEndRadius));
    }


    static void Postfix(ref float __result) {
        __result *= 2f;
    }
}
#endif

// TODO: public static void UpdateBollards(ushort nodeID, ref NetNode nodeData)

[HarmonyPatch]
static class GetEndRadiusPatch1 {
    // public virtual float GetEndRadius()
    static MethodInfo mGetEndRadius = typeof(NetAI).GetMethod("GetEndRadius", throwOnError: true);
    static MethodInfo mModifyEndRadius = typeof(GetEndRadiusPatch1).GetMethod(nameof(ModifyEndRadius), throwOnError: true);

    // protected void VehicleAI.UpdatePathTargetPositions(ushort vehicleID, ref Vehicle vehicleData, Vector3 refPos, ref int index, int max, float minSqrDistanceA, float minSqrDistanceB)
    [HarmonyPatch(typeof(VehicleAI), "UpdatePathTargetPositions")]
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) {
        var codes = instructions.ToList();

        bool LoadsPathPos(CodeInstruction c) => c.IsLdLoc(typeof(PathUnit.Position), original);

        int iGetEndRadius = codes.Search(c => c.Calls(mGetEndRadius));
        int iLoadPathPos = codes.Search(predicate: LoadsPathPos, startIndex: iGetEndRadius, count: -1);
        codes.InsertInstructions(iGetEndRadius + 1, // insert after
            new[] {
                // radius already on stack
                codes[iLoadPathPos].Clone(),
                new CodeInstruction(OpCodes.Call,mModifyEndRadius ),
        });

        return codes;
    }

    static float ModifyEndRadius(float radius0, ref PathUnit.Position pathPosition) {
        ushort segmentId = pathPosition.m_segment;
        bool startNode = pathPosition.m_offset == 0;
        var segEnd = SegmentEndManager.Instance.GetAt(segmentId, startNode);
        if (segEnd != null) {
            radius0 += segEnd.DeltaEndRadius * 0.01f * radius0;
        }
        return radius0;
    }
}

[HarmonyPatch]
internal static class GetEndRadiusPatch2 {
    // public virtual float GetEndRadius()
    static MethodInfo mGetEndRadius = typeof(NetAI).GetMethod("GetEndRadius", throwOnError: true);
    static MethodInfo mModifyEndRadius = typeof(GetEndRadiusPatch2).GetMethod(nameof(ModifyEndRadius), throwOnError: true);

    static float ModifyEndRadius(float radius0, ushort segmentId, ushort nodeId) {
        if(segmentId == 0) {
            // end node
            segmentId = nodeId.ToNode().GetFirstSegment();
        }

        Log.Called(radius0, segmentId, nodeId);
        var segEnd = SegmentEndManager.Instance.GetAt(segmentID: segmentId, nodeID: nodeId);
        if (segEnd != null) {
            radius0 += segEnd.DeltaEndRadius * 0.01f * radius0;
        }
        return radius0      ;
    }


    // public override float RoadAI.GetMinCornerOffset(ushort segmentID, ref NetSegment segmentData, ushort nodeID, ref NetNode nodeData)
    [HarmonyPatch(typeof(RoadAI), "GetMinCornerOffset"), HarmonyTranspiler]
    static IEnumerable<CodeInstruction> GetMinCornerOffset_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) {
        var codes = instructions.ToList();
        int iGetEndRadius = codes.Search(c => c.Calls(mGetEndRadius));
        codes.InsertInstructions(iGetEndRadius + 1, new[] {
            TranspilerUtils.GetLDArg(original, "segmentID"),
            TranspilerUtils.GetLDArg(original, "nodeID"),
            new CodeInstruction(OpCodes.Call,mModifyEndRadius ),
        });
        return codes;
    }

    // private void NetNode.RefreshEndData(ushort nodeID, NetInfo info, uint instanceIndex, ref RenderManager.Instance data)
    [HarmonyPatch(typeof(NetNode), "RefreshEndData"), HarmonyTranspiler]
    static IEnumerable<CodeInstruction> RefreshEndData_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) {
        var codes = instructions.ToList();
        int iGetEndRadius = codes.Search(c => c.Calls(mGetEndRadius));
        codes.InsertInstructions(iGetEndRadius + 1, new[] {
            new CodeInstruction(OpCodes.Ldc_I4_0),
            TranspilerUtils.GetLDArg(original, "nodeID"),
            new CodeInstruction(OpCodes.Call,mModifyEndRadius ),
        });
        return codes;
    }

    // private void NetNode.RefreshJunctionData(ushort nodeID, int segmentIndex, ushort nodeSegment, Vector3 centerPos, ref uint instanceIndex, ref RenderManager.Instance data)
    internal static IEnumerable<CodeInstruction> RefreshJunctionData_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) {
        var codes = instructions.ToList();
        int iGetEndRadius = codes.Search(c => c.Calls(mGetEndRadius));
        codes.InsertInstructions(iGetEndRadius + 1, new[] {
            TranspilerUtils.GetLDArg(original, "nodeSegment"),
            TranspilerUtils.GetLDArg(original, "nodeID"),
            new CodeInstruction(OpCodes.Call,mModifyEndRadius ),
        });
        return codes;
    }
}

[HarmonyPatch2(typeof(NetNode), typeof(RefreshJunctionData))]
internal static class GetEndRadiusPatch3 {
    // private void NetNode.RefreshJunctionData(ushort nodeID, int segmentIndex, ushort nodeSegment, Vector3 centerPos, ref uint instanceIndex, ref RenderManager.Instance data)
    delegate void RefreshJunctionData(ushort nodeID, int segmentIndex, ushort nodeSegment, Vector3 centerPos, ref uint instanceIndex, ref RenderManager.Instance data);
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) {
        return GetEndRadiusPatch2.RefreshJunctionData_Transpiler(instructions, original);
    }
}