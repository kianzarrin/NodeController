namespace NodeController.Patches {
    using HarmonyLib;
    using JetBrains.Annotations;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    [UsedImplicitly]
    //[HarmonyPatch]
    static class FindDirectionPatch {
        [UsedImplicitly]
        static MethodBase TargetMethod() => targetMethod_;

        static MethodInfo targetMethod_ =
            AccessTools.DeclaredMethod(typeof(NetSegment),nameof(NetSegment.FindDirection)) ??
                throw new System.Exception("FindDirectionPatch Could not find target method.");

        [UsedImplicitly]
        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator il, IEnumerable<CodeInstruction> instructions) {
            return FlatJunctionsCommons.ModifyFlatJunctionsTranspiler(instructions, targetMethod_);
        }
    }
}
