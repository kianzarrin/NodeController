using RoadTransitionManager.Util;
using ColossalFramework.UI;
using System;
using UnityEngine;

namespace RoadTransitionManager.Tool {
    using ColossalFramework;
    using GUI;
    using System.Threading;
    using static Util.RenderUtil;

    public sealed class NodeControllerTool : KianToolBase {
        UIButton button_;
        UINodeControllerPanel panel_;

        #region
        NetTool.ControlPoint m_controlPoint;
        NetTool.ControlPoint m_cachedControlPoint;
        ToolErrors m_buildErrors;
        ToolErrors m_cachedErrors;
        NetInfo segmentPrefab_;
        bool fail_;

        private object m_cacheLock = new object();


        bool MakeControlPoint() {
            NetTool.ControlPoint c = default;
            if (!IsHoverValid) {
                m_controlPoint = c;
                //Log.Debug("MakeControlPoint: HoverValid is not valid");
                return false;
            }
            if (!HoveredNodeId.ToNode().m_flags.IsFlagSet(NetNode.Flags.Moveable) ||
                HoveredNodeId.ToNode().m_flags.IsFlagSet(NetNode.Flags.End)) {
                Vector3 diff = raycastOutput.m_hitPos - HoveredNodeId.ToNode().m_position;
                const float distance = 2 * NetUtil.MPU;
                if (diff.sqrMagnitude < distance*distance) {
                    c.m_node = HoveredNodeId;
                    //c.m_position = c.m_node.ToNode().m_position;
                    Log.Debug("MakeControlPoint: On node");
                    return true;
                }
            }
            c.m_segment = HoveredSegmentId;
            ref NetSegment segment = ref c.m_segment.ToSegment();
            c.m_position = segment.GetClosestPosition(raycastOutput.m_hitPos);
            c.m_elevation = 0.5f * (segment.m_startNode.ToNode().m_elevation + segment.m_endNode.ToNode().m_elevation);
            m_controlPoint = c;
            Log.Debug("MakeControlPoint: on segment.");
            return true;
        }

        override public void SimulationStep() {
            if(!MakeControlPoint()) {
                fail_ = true;
                //Log.Debug("SimulationStep: MakeControlPoint failed");
                return;
            }
            //Log.Debug($"SimulationStep: Control point= node:{m_controlPoint.m_node} segment:{m_controlPoint.m_segment} position:{m_controlPoint.m_position}" +
            //    $"elevation:{m_controlPoint.m_elevation} ");
            if (m_controlPoint.m_node != 0) {
                fail_ = NodeData.IsSupported(m_controlPoint.m_node);
                Log.Debug("SimulationStep: node type. fail_=" + fail_);
                return;
            }
            segmentPrefab_ = HoveredSegmentId.ToSegment().Info;
            ToolBase.ToolErrors errors = NetTool.CreateNode(
                    segmentPrefab_, m_controlPoint, m_controlPoint, m_controlPoint,
                    NetTool.m_nodePositionsSimulation,
                    maxSegments: 0,
                    test: true, visualize: false, autoFix: true, needMoney: false,
                    invert: false, switchDir: false,
                    relocateBuildingID: 0,
                    out ushort newNode, out var newSegment, out var cost, out var productionRate);
            Log.Debug($"[KIAN] CreateNode test result:  errors:{errors} newNode:{newNode} newSegment:{newSegment} cost:{cost} productionRate{productionRate}");
            fail_ = errors != ToolBase.ToolErrors.None;
        }

        protected override void OnToolUpdate() {
            base.OnToolUpdate();
            while (!Monitor.TryEnter(this.m_cacheLock, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
            }
            try {
                Log.Debug("m_cachedControlPoint = m_controlPoint");
                m_cachedControlPoint = m_controlPoint;
                Log.Debug($"m_cachedControlPoint: node:{m_cachedControlPoint.m_node} segment:{m_cachedControlPoint.m_segment} " +
                    $"position:{m_cachedControlPoint.m_position}" + $"elevation:{m_cachedControlPoint.m_elevation} ");
            }
            finally {
                Monitor.Exit(this.m_cacheLock);
            }
        }

        protected override void OnToolLateUpdate() {
            base.OnToolLateUpdate();
            ForceInfoMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.None);
        }


        #endregion

        protected override void Awake() {
            button_ = ToolButton.Create();
            panel_ = UINodeControllerPanel.Create();
            base.Awake();
        }

        public static NodeControllerTool Create() {
            Log.Debug("NodeControllerTool.Create()");
            GameObject toolModControl = ToolsModifierControl.toolController.gameObject;
            var tool = toolModControl.GetComponent<NodeControllerTool>() ?? toolModControl.AddComponent<NodeControllerTool>();
            return tool;
        }

        public static NodeControllerTool Instance {
            get {
                GameObject toolModControl = ToolsModifierControl.toolController?.gameObject;
                return toolModControl?.GetComponent<NodeControllerTool>();
            }
        }

        public static void Remove() {
            Log.Debug("NodeControllerTool.Remove()");
            var tool = Instance;
            if (tool != null)
                Destroy(tool);
        }

        protected override void OnDestroy() {
            Log.Debug("NodeControllerTool.OnDestroy()\n" + Environment.StackTrace);
            button_?.Hide();
            Destroy(button_);
            panel_?.Hide();
            Destroy(panel_);
            base.OnDestroy();
        }

        //public override void EnableTool() => ToolsModifierControl.SetTool<PedBridgeTool>();

        protected override void OnEnable() {
            Log.Debug("NodeControllerTool.OnEnable");
            button_?.Focus();
            base.OnEnable();
            button_?.Focus();
            button_?.Invalidate();
            panel_?.Close();
            SelectedNodeID = 0;
        }

        protected override void OnDisable() {
            Log.Debug("NodeControllerTool.OnDisable");
            button_?.Unfocus();
            base.OnDisable();
            button_?.Unfocus();
            button_?.Invalidate();
            panel_?.Close();
            SelectedNodeID = 0;
        }


        Vector3 _cachedHitPos;
        public ushort SelectedNodeID;
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo) {
            base.RenderOverlay(cameraInfo);
            if (SelectedNodeID != 0) {
                DrawNodeCircle(cameraInfo, Color.white, SelectedNodeID, false);
                foreach (var segmentID in NetUtil.GetSegmentsCoroutine(SelectedNodeID)) {
                    ushort nodeID = segmentID.ToSegment().GetOtherNode(SelectedNodeID);
                    if(nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Middle))
                        DrawNodeCircle(cameraInfo, Color.gray, nodeID, true);
                }
            }
            var c = m_cachedControlPoint;
            Log.Debug($"RenderOverlay: Control point= node:{c.m_node} segment:{c.m_segment} position:{c.m_position}" +
                $"elevation:{c.m_elevation} ");
            Color color = fail_ ? Color.red : Color.yellow;
            if (c.m_node != 0) {
                Log.Debug("control point is node type");
                DrawNodeCircle(cameraInfo, color, HoveredNodeId, false);
            }else if (c.m_segment != 0) {
                Log.Debug("control point is segment type");
                DrawOverlayCircle(cameraInfo, color, c.m_position, segmentPrefab_.m_halfWidth, false);
            } else {
                // nothing is hovered.
            }
            DrawOverlayCircle(cameraInfo, Color.red, raycastOutput.m_hitPos, 1, true);
        }

        protected override void OnPrimaryMouseClicked() {
            if (!IsHoverValid)
                return;
            Log.Info($"OnPrimaryMouseClicked: segment {HoveredSegmentId} node {HoveredNodeId}");
            if (fail_)
                return;
            var c = m_cachedControlPoint;
            if (c.m_node!=0) {
                HelpersExtensions.Assert(NodeData.IsSupported(c.m_node), "NodeData.IsSupported(c.m_node)");
                panel_.ShowNode(HoveredNodeId);
                SelectedNodeID = HoveredNodeId;
            } else if(c.m_segment != 0) {
                ToolBase.ToolErrors errors = NetTool.CreateNode(
                    segmentPrefab_, c, c, c,
                    NetTool.m_nodePositionsSimulation,
                    maxSegments: 0,
                    test: false, visualize: false, autoFix: true, needMoney: false,
                    invert: false, switchDir: false,
                    relocateBuildingID: 0,
                    out ushort newNode, out var newSegment, out var cost, out var productionRate);
                panel_.ShowNode(newNode);
                SelectedNodeID = newNode;
            } else {
                // nothing is hovered.
            }
        }

        protected override void OnSecondaryMouseClicked() {
            panel_.Close();
            SelectedNodeID = 0;
        }


        bool IsGood1() {
            return HoveredNodeId.ToNode().CountSegments() == 2 &&
                   HoveredNodeId.ToNode().Info.m_netAI is RoadBaseAI &&
                   !HoveredNodeId.ToNode().m_flags.IsFlagSet(NetNode.Flags.Bend);
        }
        bool IsGood2() {
            return HoveredNodeId.ToNode().CountSegments() > 2 && HoveredNodeId.ToNode().Info.m_netAI is RoadBaseAI;
        }


    } //end class
}
