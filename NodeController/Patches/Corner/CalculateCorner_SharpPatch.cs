namespace NodeController.Patches.Corner;

using ColossalFramework.Math;
using HarmonyLib;
using KianCommons;
using KianCommons.Math;
using KianCommons.Patches;
using KianCommons.Plugins;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;

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
        MethodInfo mModify140 = typeof(CalculateCorner_SharpPatch).GetMethod(nameof(Modify140), throwOnError: true);

        int iLoadSharpness = codes.Search(c => c.LoadsConstant(2f), count: 3);

        codes.InsertInstructions(iLoadSharpness + 1, new[] {
            // 2 is already on the stack
            TranspilerUtils.GetLDArg(original, "ignoreSegmentID"),
            TranspilerUtils.GetLDArg(original, "startNodeID"),
            new CodeInstruction(OpCodes.Call, mModifySharpness),
        });

        const int locLessThan140 = 27;
        int iStoreLessThan140 = codes.Search(c => c.IsStLoc(locLessThan140));
        codes.InsertInstructions(iStoreLessThan140, // insert before
            new[] {
            TranspilerUtils.GetLDArg(original, "ignoreSegmentID"),
            TranspilerUtils.GetLDArg(original, "startNodeID"),
            new CodeInstruction(OpCodes.Call, mModify140),
        });

        return codes;

    }

    public static float ModifySharpness(float sharpness, ushort segmentId, ushort nodeId) {
        if (IsSharp(segmentId, nodeId)) {
            const float OFFSET_SAFETYNET = 0.02f;
            return OFFSET_SAFETYNET;
        } else {
            return sharpness;
        }
    }

    public static bool Modify140(bool angle140, ushort segmentId, ushort nodeId) {
        if (IsSharp(segmentId, nodeId)) {
            return true;
        } else {
            return angle140;
        }
    }

    private static bool IsSharp(ushort segmentId, ushort nodeId) {
        var data = SegmentEndManager.Instance.GetAt(segmentID: segmentId, nodeID: nodeId);
        return data?.SharpCorners ?? nodeId.ToNode().Info.GetARSharpCorners();
    }

    public static Bezier3 GetBezier(this ref NetSegment segment, ushort nodeId) {
        bool startNode = segment.IsStartNode(nodeId);
        return segment.CalculateSegmentBezier3(startNode);
    }

    public static void Sharpen2(
        ushort segmentId1, bool startNode, bool leftSide,
        ref Vector3 cornerPos, ref Vector3 cornerDirection) {
        ref NetSegment segment1 = ref segmentId1.ToSegment();
        ushort nodeId = segment1.GetNode(startNode);
        if (!IsSharp(segmentId1, nodeId: nodeId)) return;

        ref NetNode node = ref nodeId.ToNode();
        int nSegments = node.CountSegments();
        if (nSegments < 2) {
            return;
        }

        ushort segmentId2;
        if (leftSide /*right going toward junction*/) {
            segmentId2 = segment1.GetRightSegment(nodeId);
        } else {
            segmentId2 = segment1.GetLeftSegment(nodeId);
        }
        ref NetSegment segment2 = ref segmentId2.ToSegment();

        Vector3 pos = node.m_position;
        float hw1 = segment1.Info.m_halfWidth;
        float hw2 = segment2.Info.m_halfWidth;
        Vector3 dir1 = NormalizeXZ(segment1.GetDirection(nodeId));
        Vector3 dir2 = NormalizeXZ(segment2.GetDirection(nodeId));

        float det = VectorUtil.DetXZ(dir1, dir2);
        bool lessThan180 = det > 0 == leftSide;
        if(lessThan180) {
            return; // already handled
        }

        float sin = Vector3.Cross(dir1, dir2).y;
        sin = -sin;

#if SAMEDIR
        static bool SameDir(Vector3 tangent, Vector3 dir) {
            return DotXZ(tangent, NormalizeXZ(dir)) > 0.95f;
        }

        Vector3 otherPos1 = segment1.GetOtherNode(nodeId).ToNode().m_position;
        Vector3 otherPos2 = segment2.GetOtherNode(nodeId).ToNode().m_position;
        bool isStraight1 = SameDir(dir1, otherPos1 - pos);
        bool isStraight2 = SameDir(dir2, otherPos2 - pos);
        if (!isStraight1 || !isStraight2) {
            //var otherDir1 = otherPos1 - pos;
            //var otherDir1n = NormalizeXZ(otherDir1);
            //float dotxz1 = DotXZ(dir1, otherDir1n);
            //Log.Debug($"isStraight1={isStraight1} dir1={dir1} otherDir1={otherDir1} otherDir1.normalizedXZ={otherDir1n} dotxz1={dotxz1}");
            //var otherDir2 = otherPos2 - pos;
            //var otherDir2n = NormalizeXZ(otherDir2);
            //float dotxz2 = DotXZ(dir2, otherDir2n);
            //Log.Debug($"isStraight2={isStraight2} dir2={dir2} otherDir2={otherDir2} otherDir2.normalizedXZ={otherDir2n} dotxz2={dotxz2}");
            return;
        }
#endif


        Log.Debug($"p3: node:{nodeId}, segment:{segmentId1} segmentId2:{segmentId2} leftSide={leftSide} sin={sin}", false);
        if (Mathf.Abs(sin) > 0.001) {
            float scale = 1 / sin;
            if (!leftSide)
                scale = -scale;

            pos += dir2 * hw1 * scale;
            pos += dir1 * hw2 * scale; // intersection.

            const float OFFSET_SAFETYNET = 0.02f;
            cornerPos = pos + dir1 * OFFSET_SAFETYNET;
        }
    }
}