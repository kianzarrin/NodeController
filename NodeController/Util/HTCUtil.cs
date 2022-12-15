namespace NodeController.Util; 
using ColossalFramework.Plugins;
using KianCommons;
using KianCommons.Plugins;
using System;
using System.Reflection;
using static ColossalFramework.Plugins.PluginManager;

internal static class HTCUtil {
    static class Delegates {
        public delegate bool ShouldHideCrossing(ushort nodeID, ushort segmentID);
    }

    static HTCUtil() {
        Init();
        PluginManager.instance.eventPluginsStateChanged += Init;
        PluginManager.instance.eventPluginsChanged += Init;
        LoadingManager.instance.m_levelPreLoaded += Init;
    }

    private static void Init() {
        try {
            Log.Stack();
            Plugin = PluginUtil.GetHideCrossings();
            IsActive = Plugin.IsActive();

            if (IsActive) {
                asm = Plugin.GetMainAssembly();
                var version = Plugin.userModInstance.VersionOf() ?? new Version(0, 0);
                Log.Info("HTC Version=" + version);
                shouldHideCrossing_ = DelegateUtil.CreateDelegate<Delegates.ShouldHideCrossing>(Type_CalculateMaterialCommons);
            } else {
                Log.Info("HTC not found.");
                asm = null;
            }
        } catch(Exception ex) { ex.Log(); }
    }

    public static PluginInfo Plugin { get; private set; }

    public static bool IsActive { get; private set; }

    public static Assembly asm { get; private set; }

    /// <summary>
    /// type of <see cref="HideCrosswalks.Patches.CalculateMaterialCommons.ShouldHideCrossing"/>
    /// </summary>
    public static Type Type_CalculateMaterialCommons => asm.GetType("HideCrosswalks.Patches.CalculateMaterialCommons");

    static Delegates.ShouldHideCrossing shouldHideCrossing_;
    public static bool ShouldHideCrossing(ushort nodeID, ushort segmentID) {
        return shouldHideCrossing_?.Invoke(nodeID: nodeID, segmentID: segmentID) ?? false;
    }

}
