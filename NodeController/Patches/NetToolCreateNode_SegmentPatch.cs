namespace NodeController.Patches {
    using HarmonyLib;
    using NodeController.LifeCycle;
    using System.Collections.Generic;
    using System.Reflection;

    [HarmonyPatch]
    public class NetToolCreateNode_SegmentPatch {
        //public static ToolBase.ToolErrors CreateNode(NetInfo info, NetTool.ControlPoint startPoint, NetTool.ControlPoint middlePoint, NetTool.ControlPoint endPoint, FastList<NetTool.NodePosition> nodeBuffer, int maxSegments, bool test, bool visualize, bool autoFix, bool needMoney, bool invert, bool switchDir, ushort relocateBuildingID, out ushort node, out ushort segment, out int cost, out int productionRate)
        delegate ToolBase.ToolErrors TargetDelegate(NetInfo info, NetTool.ControlPoint startPoint, NetTool.ControlPoint middlePoint, NetTool.ControlPoint endPoint, FastList<NetTool.NodePosition> nodeBuffer, int maxSegments, bool test, bool visualize, bool autoFix, bool needMoney, bool invert, bool switchDir, ushort relocateBuildingID, out ushort node, out ushort segment, out int cost, out int productionRate);
        public static MethodBase TargetMethod() =>
            TranspilerUtils.DeclaredMethod<TargetDelegate>(typeof(NetTool), nameof(NetTool.CreateNode));

        /// <summary>
        /// called once per segment in PathInfos.
        /// maps old ids to new ids.
        /// </summary>
        /// <param name="segment">new segment</param>
        public static void Postfix(ushort segment) {
            if (!LoadPathsPatch.Mapping)return;
            LoadPathsPatch.PathInfoExts[LoadPathsPatch.Index++].
                MapInstanceIDs(newSegmentId: segment, map: LoadPathsPatch.Map);
        }
    }
}

