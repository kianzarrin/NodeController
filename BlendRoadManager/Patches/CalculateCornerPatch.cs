namespace BlendRoadManager.Patches {
    using BlendRoadManager.Util;
    using HarmonyLib;
    using JetBrains.Annotations;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using static TranspilerUtils;
    using static Util.HelpersExtensions;

    [UsedImplicitly]
    [HarmonyPatch]
    static class CalculateCornerPatch {
        static float GetMinCornerOffset(NetInfo info, ushort nodeID) {
            var data = NodeBlendManager.Instance.buffer[nodeID];
            return data?.CornerOffset ?? info.m_minCornerOffset;
        }

        [UsedImplicitly]
        static MethodBase TargetMethod() {
            return typeof(NetSegment).GetMethod(
                    nameof(NetSegment.CalculateCorner),
                    BindingFlags.Public | BindingFlags.Static) ??
                    throw new System.Exception("CalculateCornerPatch Could not find target method.");
        }

        static FieldInfo f_minCornerOffset =
            typeof(NetInfo).GetField(nameof(NetInfo.m_minCornerOffset)) ??
            throw new Exception("f_minCornerOffset is null");

        static MethodInfo mGetMinCornerOffset = AccessTools.DeclaredMethod(
            typeof(CalculateCornerPatch), nameof(GetMinCornerOffset)) ??
            throw new Exception("mGetMinCornerOffset is null");

        static MethodInfo targetMethod_ = TargetMethod() as MethodInfo;

        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator il, IEnumerable<CodeInstruction> instructions) {
            CodeInstruction ldarg_startNodeID = GetLDArg(targetMethod_, "startNodeID"); // push startNodeID into stack,
            CodeInstruction call_GetMinCornerOffset = new CodeInstruction(OpCodes.Call, mGetMinCornerOffset);

            int n = 0;
            foreach (var innstruction in instructions) {
                bool is_ldfld_minCornerOffset =
                    innstruction.opcode == OpCodes.Ldfld && innstruction.operand == f_minCornerOffset;
                if (is_ldfld_minCornerOffset) {
                    n++;
                    yield return ldarg_startNodeID;
                    yield return call_GetMinCornerOffset;
                } else {
                    yield return innstruction;
                }
            }

            Log.Debug($"TRANSPILER CalculateCornerPatch: Successfully patched NetSegment.CalculateCorner(). " +
                $"found {n} instances of Ldfld NetInfo.m_minCornerOffset");
            yield break;
        }
    }

    //public static int SearchNext_Ldfld_minCornerOffset(List<CodeInstruction> codes, int index) {
    //    try {
    //        index = SearchInstruction(codes, new CodeInstruction(OpCodes.Ldfld, f_minCornerOffset), index, counter: 1);
    //        Assert(index != 0, "index!=0");
    //        return index;
    //    }
    //    catch (InstructionNotFoundException) {
    //        return 0;
    //    }
    //}


    //public static void Replace_Call_GetMinCornerOffset(List<CodeInstruction> codes, int index) {
    //    var newInstructions = new[] {
    //            GetLDArg(targetMethod_, "startNodeID"), // push startNodeID into stack,
    //            new CodeInstruction(OpCodes.Call, mGetMinCornerOffset), // call float GetMinCornerOffset(info, startNodeID)
    //        };
    //    ReplaceInstructions(codes, newInstructions, index);
    //}
}
