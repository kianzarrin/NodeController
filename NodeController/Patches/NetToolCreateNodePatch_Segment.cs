namespace NodeController.Patches {
    using HarmonyLib;
    using System.Reflection;
    using NodeController.Util;
    using System;

    /// <summary>
    /// this overload of CreateNode is for creating segment.
    /// </summary>
    [HarmonyPatch]
    public class NetToolCreateNodePatch_Segment {
        //public static ToolBase.ToolErrors CreateNode(NetInfo info, NetTool.ControlPoint startPoint, NetTool.ControlPoint middlePoint, NetTool.ControlPoint endPoint, FastList<NetTool.NodePosition> nodeBuffer, int maxSegments, bool test, bool testEnds, bool visualize, bool autoFix, bool needMoney, bool invert, bool switchDir, ushort relocateBuildingID, out ushort firstNode, out ushort lastNode, out ushort segment, out int cost, out int productionRate)
        delegate ToolBase.ToolErrors TargetDelegate(NetInfo info, NetTool.ControlPoint startPoint, NetTool.ControlPoint middlePoint, NetTool.ControlPoint endPoint, FastList<NetTool.NodePosition> nodeBuffer,
            int maxSegments, bool test, bool testEnds, bool visualize, bool autoFix, bool needMoney, bool invert, bool switchDir, ushort relocateBuildingID,
            out ushort firstNode, out ushort lastNode, out ushort segment, out int cost, out int productionRate);
        public static MethodBase TargetMethod() =>
            TranspilerUtils.DeclaredMethod<TargetDelegate>(typeof(NetTool), nameof(NetTool.CreateNode));

        /// <summary>
        /// called once per segment in PathInfos. Also called by the other overload of CreateNode in which case it does not create segment.
        /// maps old ids to new ids.
        /// </summary>
        /// <param name="segment">new segment</param>
        public static void Postfix(ref ushort segment, ref ushort firstNode, bool test, bool visualize) {
            if (test || visualize || !LoadPathsPatch.Mapping)return;
            if (segment == 0)return; // called by the othre CreateNode
            Log.Debug($"NetToolCreateNodePatch_Segment.Postfix(segment={segment}, firstNode={firstNode})");
            Log.Debug($"NetToolCreateNodePatch_Segment.Postfix(): index={LoadPathsPatch.Index} PathInfoExts={LoadPathsPatch.PathInfoExts.ToSTR()}   ");
            Log.Debug("stacktrace: " + Environment.StackTrace);
            LoadPathsPatch.PathInfoExts[LoadPathsPatch.Index].
                MapInstanceIDs(newSegmentId: segment, map: LoadPathsPatch.Map);
            LoadPathsPatch.Index++;
        }
    }
}

