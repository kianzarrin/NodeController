namespace NodeController.Patches {
    using CSUtil.Commons;
    public static class PrefixUtils {
        public static bool HandleTernaryBool(TernaryBool? res, ref bool __result) {
            if (res == null || res == TernaryBool.Undefined)
                return true; // do nothing

            __result = res == TernaryBool.True;
            return false; // override
        }
    }
}
