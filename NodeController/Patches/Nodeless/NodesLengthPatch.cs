namespace NodeController.Patches.Nodeless {
    using HarmonyLib;
    using KianCommons;
    using KianCommons.Patches;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using UnityEngine;
    using static KianCommons.Patches.TranspilerUtils;

    /// <summary>
    /// sets node length to 0 when node-less to avoid glitches.
    /// at close up camera, I check for node-less on a per segment basis.
    /// at far away camera, small glitches are not noticeable and no need for a complicated transpiler.
    /// I settle for only hiding glitches when all segment end are node-less.
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
            yield return DeclaredMethod<CalculateGroupData>(typeof(NetNode));
            yield return DeclaredMethod<PopulateGroupData>(typeof(NetNode));
        }

        static int NodesLength(int length0, ushort nodeID, uint instanceIndex) {
            try {
                bool nodeless;
                if(instanceIndex == ushort.MaxValue) {
                    var nodeData = NodeManager.Instance.buffer[nodeID];
                    nodeless = nodeData?.IsNodelessJunction() ?? false;
                } else {
                    ref var renderData = ref RenderManager.instance.m_instances[instanceIndex];
                    ref var node = ref nodeID.ToNode();
                    ushort segmentID = node.GetSegment(renderData.m_dataInt0 & 7);
                    ushort segmentID2 = node.GetSegment(renderData.m_dataInt0 >> 4);
                    var segmentData = SegmentEndManager.Instance.GetAt(segmentID: segmentID, nodeID: nodeID);
                    var segmentData2 = SegmentEndManager.Instance.GetAt(segmentID: segmentID2, nodeID: nodeID);
                    nodeless = (segmentData?.IsNodeless ?? false) || (segmentData2?.IsNodeless ?? false);
                }
                if(nodeless)
                    return 0;
            } catch(Exception ex) {
                ex.Log();
            }
            return length0;
        }

        static FieldInfo f_nodes =>
            ReflectionHelpers.GetField<NetInfo>(nameof(NetInfo.m_nodes));

        static MethodInfo mNodesLength = ReflectionHelpers.GetMethod(
            typeof(NodesLengthPatch), nameof(NodesLength));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) {
            CodeInstruction ldargNodeID = GetLDArg(original, "nodeID");
            CodeInstruction callNodesLength = new CodeInstruction(OpCodes.Call, mNodesLength);
            CodeInstruction LoadRenderIndex =
                GetLDArg(original, "instanceIndex", throwOnError:false) ?? // when camera is close, we can easily get segmentID from render data.
                new CodeInstruction(OpCodes.Ldc_I4, (int)ushort.MaxValue); // getting segmentIDs is too complicated and not worth it from far camera range.

            bool isLdNodes_prev = false;
            int n = 0;
            foreach (var instruction in instructions) {
                yield return instruction;
                if (isLdNodes_prev && instruction.opcode == OpCodes.Ldlen) {
                    n++;
                    yield return ldargNodeID.Clone();
                    yield return LoadRenderIndex.Clone();
                    if(LoadRenderIndex.IsLdarg())
                        yield return new CodeInstruction(OpCodes.Ldind_U4); // convert ref uint to uint
                    yield return callNodesLength.Clone();
                }

                isLdNodes_prev = instruction.LoadsField(f_nodes);
            }

            Log.Succeeded($"patched {n} instances of m_nodes.Length in {original}");
        }
    }
}
