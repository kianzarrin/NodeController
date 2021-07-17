namespace NodeController.Patches.Nodeless {
    using HarmonyLib;
    using KianCommons;
    using KianCommons.Patches;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using UnityEngine;
    using static KianCommons.Patches.TranspilerUtils;

    /// <summary>
    /// sets node length to 0 when nodeless to avoid glitches. 
    /// </summary>
    [HarmonyPatch]
    static class NodesLengthPatch {
        delegate void RenderInstance0(RenderManager.CameraInfo cameraInfo, ushort nodeID, int layerMask);
        delegate void RenderInstance(RenderManager.CameraInfo cameraInfo, ushort nodeID, NetInfo info, int iter, NetNode.Flags flags, ref uint instanceIndex, ref RenderManager.Instance data);
        delegate bool CalculateGroupData(ushort nodeID, int layer, ref int vertexCount, ref int triangleCount, ref int objectCount, ref RenderGroup.VertexArrays vertexArrays);
        delegate void PopulateGroupData(ushort nodeID, int groupX, int groupZ, int layer, ref int vertexIndex, ref int triangleIndex, Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance, ref bool requireSurfaceMaps);

        static IEnumerable<MethodBase> TargetMethods() {
            yield return DeclaredMethod<RenderInstance0>(typeof(NetNode), nameof(NetNode.RenderInstance));
            yield return DeclaredMethod<RenderInstance>(typeof(NetNode));
            yield return DeclaredMethod<RenderInstance>(typeof(NetNode));
            yield return DeclaredMethod<CalculateGroupData>(typeof(NetNode));
            yield return DeclaredMethod<PopulateGroupData>(typeof(NetNode));
        }

        static int NodesLength(int length0, ushort nodeID) {
            var nodeData = NodeManager.Instance.buffer[nodeID];
            bool nodeless = nodeData?.IsNodelessJunction() ?? false;
            if (nodeless)
                return 0; // set node length to 0 when nodeless
            else
                return length0;
        }

        static FieldInfo f_nodes =>
            ReflectionHelpers.GetField<NetInfo>(nameof(NetInfo.m_nodes));

        static MethodInfo mNodesLength = ReflectionHelpers.GetMethod(
            typeof(NodesLengthPatch), nameof(NodesLength));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) {
            CodeInstruction ldargNodeID = GetLDArg(original, "nodeID");
            CodeInstruction callNodesLength = new CodeInstruction(OpCodes.Call, mNodesLength);

            bool isLdNodes_prev = false;
            int n = 0;
            foreach (var instruction in instructions) {
                yield return instruction;
                if (isLdNodes_prev && instruction.opcode == OpCodes.Ldlen) {
                    n++;
                    yield return ldargNodeID.Clone();
                    yield return callNodesLength.Clone();
                }

                isLdNodes_prev = instruction.LoadsField(f_nodes);
            }

            Log.Succeeded($"patched {n} instances of m_nodes.Length in {original}");
        }
    }
}
