namespace NodeController.Patches.Nodeless {
    using HarmonyLib;
    using KianCommons;
    using KianCommons.Patches;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;

    // sets node length to 0 when nodeless to avoid glitches.

    [HarmonyPatch]
    static class RenderInstancePatch {
        delegate void RenderInstance(RenderManager.CameraInfo cameraInfo, ushort nodeID, NetInfo info, int iter, NetNode.Flags flags, ref uint instanceIndex, ref RenderManager.Instance data);
        static MethodBase TargetMethod() =>
            TranspilerUtils.DeclaredMethod<RenderInstance>(typeof(NetNode));

        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions, MethodBase original) {
            try {
                var codes = TranspilerUtils.ToCodeList(instructions);
                NodesLengthCommons.Patch(codes, original, occurance: 1, counterGetSegment: 2); //DC
                NodesLengthCommons.Patch(codes, original, occurance: 2, counterGetSegment: 1); //Junction
                return codes;
            } catch (Exception ex) {
                ex.Log();
                throw ex;
            }
        }
    }

    [HarmonyPatch]
    static class CGDPatch {
        delegate bool CalculateGroupData(ushort nodeID, int layer, ref int vertexCount, ref int triangleCount, ref int objectCount, ref RenderGroup.VertexArrays vertexArrays);
        static MethodBase TargetMethod() =>
            TranspilerUtils.DeclaredMethod<CalculateGroupData>(typeof(NetNode));

        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions, MethodBase original) {
            try {
                var codes = TranspilerUtils.ToCodeList(instructions);
                // unlike RenderInstance, here we are only interested odd numbered occurances
                // because the code first checks m_node.lengths != 0 (odd) and then it loops through them (even).
                NodesLengthCommons.Patch(codes, original, occurance: 1, counterGetSegment: 2); //DC
                NodesLengthCommons.Patch(codes, original, occurance: 3, counterGetSegment: 2); //DC // CS has copy pasted code.
                NodesLengthCommons.Patch(codes, original, occurance: 5, counterGetSegment: 1); //Junction
                return codes;
            } catch (Exception ex) {
                ex.Log();
                throw ex;
            }
        }
    }

    [HarmonyPatch]
    static class PGDPatch {
        delegate void PopulateGroupData(ushort nodeID, int groupX, int groupZ, int layer, ref int vertexIndex, ref int triangleIndex, Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance, ref bool requireSurfaceMaps);
        static MethodBase TargetMethod() =>
            TranspilerUtils.DeclaredMethod<PopulateGroupData>(typeof(NetNode));

        public static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions, MethodBase original) {
            try {
                var codes = TranspilerUtils.ToCodeList(instructions);
                // unlike RenderInstance, here we are only interested odd numbered occurances
                // because the even numbered occurances are nested.
                NodesLengthCommons.Patch(codes, original, occurance: 1, counterGetSegment: 2); //DC
                NodesLengthCommons.Patch(codes, original, occurance: 3, counterGetSegment: 2); //DC // CS has copy pasted code.
                NodesLengthCommons.Patch(codes, original, occurance: 5, counterGetSegment: 1); //Junction
                return codes;
            } catch (Exception ex) {
                ex.Log();
                throw ex;
            }
        }
    }
}
