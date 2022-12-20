#if DEBUG
namespace NodeController.Patches._NetManager; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using KianCommons;
using KianCommons.Patches;

//[HarmonyPatch2(typeof(NetManager), typeof(UpdateNode))]
static class UpdateNodePatch {
    delegate void UpdateNode(ushort node, ushort fromSegment, int level);
    static void Prefix(ushort node, ushort fromSegment, int level) {
        Log.Called("node:" + node, "fromSegment:" + fromSegment, "level:" + level);
        Log.Stack();
    }
}

//[HarmonyPatch2(typeof(NetManager), typeof(UpdateSegment))]
static class UpdateSegmentPatch {
    delegate void UpdateSegment(ushort segment);
    static void Prefix(ushort segment) {
        Log.Called("segment:" + segment);
        Log.Stack();
    }
}
#endif