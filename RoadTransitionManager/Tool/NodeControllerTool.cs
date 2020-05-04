using ColossalFramework.UI;
using RoadTransitionManager.Util;
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

        NetTool.ControlPoint m_controlPoint;
        NetTool.ControlPoint m_cachedControlPoint;
        ToolErrors m_errors;
        ToolErrors m_cachedErrors;
        NetInfo m_prefab;

        private object m_cacheLock = new object();

        protected override void Awake() {
            button_ = RoadTransitionButton.CreateButton(); // ToolButton.Create();
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

        override public void SimulationStep() {
            ServiceTypeGuide optionsNotUsed = Singleton<NetManager>.instance.m_optionsNotUsed;
            if (optionsNotUsed != null && !optionsNotUsed.m_disabled) {
                optionsNotUsed.Disable();
            }

            ToolBase.ToolErrors errors = ToolBase.ToolErrors.None;
            if (m_prefab != null) {
                if (this.m_mouseRayValid && MakeControlPoint()) {
                    //Log.Debug("SimulationStep control point is " + LogControlPoint(m_controlPoint));
                    if (m_controlPoint.m_node == 0) {
                        errors = NetUtil.InsertNode(m_controlPoint, out _, test: true);
                    }
                } else {
                    errors |= ToolBase.ToolErrors.RaycastFailed;
                }
            }

            m_toolController.ClearColliding();

            while (!Monitor.TryEnter(this.m_cacheLock, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
            }
            try {
                this.m_errors = errors;
            }
            finally {
                Monitor.Exit(this.m_cacheLock);
            }
        }

        protected override void OnToolUpdate() {
            base.OnToolUpdate();

            while (!Monitor.TryEnter(this.m_cacheLock, SimulationManager.SYNCHRONIZE_TIMEOUT)) {
            }
            try {
                m_cachedControlPoint = m_controlPoint;
                m_cachedErrors = m_errors;
            }
            finally {
                Monitor.Exit(this.m_cacheLock);
            }

            if (HoveredSegmentId != 0) {
                m_prefab = HoveredSegmentId.ToSegment().Info;
            } else {
                m_prefab = null;
            }
        }

        protected override void OnToolLateUpdate() {
            base.OnToolLateUpdate();
            ForceInfoMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.None);
        }

        public Color GetColor(bool error) {
            if (error)
                return base.GetToolColor(false,true);
            Color c = base.GetToolColor(false, false);
            Color ret = Color.yellow;
            ret.a = base.GetToolColor(false, false).a;
            return ret;
        }

        Vector3 _cachedHitPos;
        public ushort SelectedNodeID;
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo) {
            base.RenderOverlay(cameraInfo);
            if (!m_mouseRayValid || handleHovered_)
                return;

            if (SelectedNodeID != 0) {
                DrawNodeCircle(cameraInfo, Color.white, SelectedNodeID, false);
                foreach (var segmentID in NetUtil.GetSegmentsCoroutine(SelectedNodeID)) {
                    ushort nodeID = segmentID.ToSegment().GetOtherNode(SelectedNodeID);
                    if (nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Middle))
                        DrawNodeCircle(cameraInfo, Color.gray, nodeID, true);
                }
            }

            if (IsHoverValid && m_prefab != null) {
                NetTool.ControlPoint controlPoint = m_cachedControlPoint;
                ushort nodeID = controlPoint.m_node;
                if (nodeID != 0) {
                    bool fail = !NodeData.IsSupported(nodeID);
                    DrawNodeCircle(cameraInfo, GetColor(fail), nodeID, false);
                } else if (controlPoint.m_segment != 0) {
                    bool isRoad = m_prefab.m_netAI is RoadBaseAI && !NetUtil.IsCSUR(m_prefab);
                    ToolErrors error = m_cachedErrors;
                    error |= m_prefab.m_netAI.CheckBuildPosition(false, false, true, true, ref controlPoint, ref controlPoint, ref controlPoint, out _, out _, out _, out _);
                    bool fail = error != ToolErrors.None || !isRoad;
                    Color color = GetColor(fail);
                    DrawOverlayCircle(cameraInfo, color, controlPoint.m_position, m_prefab.m_halfWidth, false);
                }
                DrawOverlayCircle(cameraInfo, Color.red, raycastOutput.m_hitPos, 1, true);
            }
        }

        protected override void OnToolGUI(Event e) {
            base.OnToolGUI(e);
            DrawSigns();
        }

        bool handleHovered_;
        //ushort updatedNodeId_;
        private void DrawSigns() {
            Vector3 camPos = Singleton<SimulationManager>.instance.m_simulationView.m_position;
            if (SelectedNodeID == 0) {
                TrafficRulesOverlay overlay =
                        new GUI.TrafficRulesOverlay(handleClick: false);
                foreach (NodeData nodeData in NodeManager.Instance.buffer) {
                    if (nodeData == null) continue;
                    overlay.DrawSignHandles(
                        nodeData.NodeID, camPos: ref camPos, out _);
                }
            } else {
                TrafficRulesOverlay overlay =
                        new GUI.TrafficRulesOverlay(handleClick: true);
                handleHovered_ = overlay.DrawSignHandles(
                    SelectedNodeID, camPos: ref camPos, out _);
            }
        }

        protected override void OnPrimaryMouseClicked() {
            if (!IsHoverValid || handleHovered_)
                return;
            Log.Info($"OnPrimaryMouseClicked: segment {HoveredSegmentId} node {HoveredNodeId}");
            if (m_errors != ToolErrors.None || m_prefab == null)
                return;
            var c = m_cachedControlPoint;
            if (c.m_node != 0) {
                bool supported = NodeData.IsSupported(c.m_node);
                if (!supported) {
                    return;
                }
                SelectedNodeID = c.m_node;
                panel_.ShowNode(SelectedNodeID);
            } else if (c.m_segment != 0) {
                if (m_prefab.m_netAI is RoadBaseAI && !NetUtil.IsCSUR(m_prefab)) {
                    SimulationManager.instance.AddAction(delegate () {
                        NodeData nodeData = NodeManager.Instance.InsertNode(c);
                        if (nodeData != null) {
                            SelectedNodeID = nodeData.NodeID;
                            panel_.ShowNode(SelectedNodeID);
                        }
                    });
                }
            } else {
                // nothing is hovered.
            }
        }

        protected override void OnSecondaryMouseClicked() {
            if (SelectedNodeID == 0) {
                DisableTool();
            } else {
                panel_.Close();
                SelectedNodeID = 0;
            }
        }

        static string LogControlPoint(NetTool.ControlPoint c) {
            return $"<node:{c.m_node} segment:{c.m_segment} " +
                $"position:{c.m_position}" + $"elevation:{c.m_elevation}>";
        }

        bool MakeControlPoint() {
            if (!IsHoverValid) {
                //Log.Debug("MakeControlPoint: HoverValid is not valid");
                m_controlPoint = default;
                return false;
            }
            ushort segmentID0 = 0, segmentID1 = 0;
            int count = 0;
            foreach (ushort segmentID in NetUtil.GetSegmentsCoroutine(HoveredNodeId)) {
                if (segmentID == 0)
                    continue;
                if (count == 0) segmentID0 = segmentID;
                if (count == 1) segmentID1 = segmentID;
                count++;
            }

            bool snapNode =
                count != 2 ||
                segmentID0.ToSegment().Info != segmentID1.ToSegment().Info ||
                !HoveredNodeId.ToNode().m_flags.IsFlagSet(NetNode.Flags.Moveable);
            if (snapNode) {
                Vector3 diff = raycastOutput.m_hitPos - HoveredNodeId.ToNode().m_position;
                const float distance = 2 * NetUtil.MPU;
                if (diff.sqrMagnitude < distance * distance) {
                    m_controlPoint = new NetTool.ControlPoint { m_node = HoveredNodeId };
                    //Log.Debug("MakeControlPoint: On node");
                    return true;
                }
            }
            ref NetSegment segment = ref HoveredSegmentId.ToSegment();
            float elevation = 0.5f * (segment.m_startNode.ToNode().m_elevation + segment.m_endNode.ToNode().m_elevation);
            m_controlPoint = new NetTool.ControlPoint {
                m_segment = HoveredSegmentId,
                m_position = segment.GetClosestPosition(raycastOutput.m_hitPos),
                m_elevation = elevation,
            };
            //Log.Debug("MakeControlPoint: on segment.");
            return true;
        }

    } //end class
}
