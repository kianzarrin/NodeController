
namespace BlendRoadManager
{
    using Harmony;
    using BlendRoadManager.Util;
    using System.Reflection;

    public static class HarmonyExtension
    {
        public static string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        public static string HARMONY_ID = "CS.Kian." + AssemblyName; 

        public static void InstallHarmony()
        {
            Log.Info("Patching...");
            var harmony = HarmonyInstance.Create(HARMONY_ID);
            harmony.PatchAll();
            Log.Info("Patched.");
        }

        public static void UninstallHarmony()
        {
            Log.Info("UnPatching...");
            var harmony = HarmonyInstance.Create(HARMONY_ID);
            harmony.UnpatchAll();
            Log.Info("UnPatched.");
        }
    }
}