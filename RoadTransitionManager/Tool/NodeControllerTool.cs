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

        #region
        NetTool.ControlPoint m_controlPoint;
        NetTool.ControlPoint m_cachedControlPoint;
        ToolErrors m_errors;
        ToolErrors m_cachedErrors;
        NetInfo m_prefab;

        private object m_cacheLock = new object();

        override public void SimulationStep() {
            ServiceTypeGuide optionsNotUsed = Singleton<NetManager>.instance.m_optionsNotUsed;
            if (optionsNotUsed != null && !optionsNotUsed.m_disabled) {
                optionsNotUsed.Disable();
            }

            Vector3 position = this.m_controlPoint.m_position;
            bool failed = false;

            NetTool.ControlPoint controlPoint = default(NetTool.ControlPoint);
            NetNode.Flags ignoreNodeFlags;
            NetSegment.Flags ignoreSegmentFlags;

            ignoreNodeFlags = NetNode.Flags.None;
            ignoreSegmentFlags = NetSegment.Flags.None;

            Building.Flags ignoreBuildingFlags = Building.Flags.All;
            ToolBase.ToolErrors errors = ToolBase.ToolErrors.None;
            if (m_prefab != null) {
                if (this.m_mouseRayValid && NetTool.MakeControlPoint(this.m_mouseRay, this.m_mouseRayLength, m_prefab, false, ignoreNodeFlags, ignoreSegmentFlags, ignoreBuildingFlags, 0, false, out controlPoint)) {
                    errors = NetTool.CreateNode(
                            m_prefab, controlPoint, controlPoint, controlPoint,
                            NetTool.m_nodePositionsSimulation,
                            maxSegments: 0,
                            test: true, visualize: false, autoFix: true, needMoney: false,
                            invert: false, switchDir: false,
                            relocateBuildingID: 0,
                            out ushort newNode, out var newSegment, out var cost, out var productionRate);
                    Log.Debug($"[KIAN] CreateNode test result:  errors:{errors} newNode:{newNode} newSegment:{newSegment} cost:{cost} productionRate{productionRate}");
                    if (newNode != 0) {
                        controlPoint.m_node = newNode; // node mode
                    }
                } else {
                    errors |= ToolBase.ToolErrors.RaycastFailed;
                }
            }

            m_controlPoint = controlPoint;
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
                //Log.Debug("m_cachedControlPoint = m_controlPoint");
                m_cachedControlPoint = m_controlPoint;
                //Log.Debug($"m_cachedControlPoint: node:{m_cachedControlPoint.m_node} segment:{m_cachedControlPoint.m_segment} " +
                //    $"position:{m_cachedControlPoint.m_position}" + $"elevation:{m_cachedControlPoint.m_elevation} ");
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
                    if (nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Middle))
                        DrawNodeCircle(cameraInfo, Color.gray, nodeID, true);
                }
            }
            //var c = m_cachedControlPoint;
            //Log.Debug($"RenderOverlay: Control point= node:{c.m_node} segment:{c.m_segment} position:{c.m_position}" +
            //    $"elevation:{c.m_elevation} ");
            //Debug.Log ("[Crossings] Render Overlay");
            base.RenderOverlay(cameraInfo);

            if (IsHoverValid && m_prefab != null) {
                NetTool.ControlPoint controlPoint = m_cachedControlPoint;
                m_prefab.m_netAI.CheckBuildPosition(false, false, true, true, ref controlPoint, ref controlPoint, ref controlPoint, out _, out _, out _, out _);

                bool error = CanCreateOrSelect(HoveredSegmentId, HoveredSegmentId);
                error = m_cachedErrors != ToolErrors.None;
                Color color = base.GetToolColor(false, error);
                DrawOverlayCircle(cameraInfo, color, controlPoint.m_position, m_prefab.m_halfWidth, false);
                DrawOverlayCircle(cameraInfo, Color.red, raycastOutput.m_hitPos, 1, true);
            }
        }

        private static bool CanCreateOrSelect(ushort segmentID, ushort nodeID) {
            if (segmentID == 0)
                return false;

            ref NetSegment segment = ref segmentID.ToSegment();
            NetInfo info = segment.Info;
            ItemClass.Level level = info.m_class.m_level;
            if (!(info.m_netAI is RoadBaseAI))
                return false; // No crossings on non-roads

            if (nodeID == 0)
                return true; // No node means we can create one

            NetNode.Flags flags = NetManager.instance.m_nodes.m_buffer[nodeID].m_flags;
            return !flags.IsFlagSet(NetNode.Flags.End);
        }

        protected override void OnPrimaryMouseClicked() {
            if (!IsHoverValid)
                return;
            Log.Info($"OnPrimaryMouseClicked: segment {HoveredSegmentId} node {HoveredNodeId}");
            if (m_errors !=ToolErrors.None || m_prefab == null)
                return;
            var c = m_cachedControlPoint;
            if (c.m_node != 0) {
                bool supported = NodeData.IsSupported(c.m_node);
                HelpersExtensions.Assert(supported, "supported");
                panel_.ShowNode(HoveredNodeId);
                SelectedNodeID = HoveredNodeId;
            } else if (c.m_segment != 0) {
                SimulationManager.instance.AddAction(delegate () {
                    ToolErrors errors = NetTool.CreateNode(
                        m_prefab, c, c, c,
                        NetTool.m_nodePositionsSimulation,
                        maxSegments: 0,
                        test: false, visualize: false, autoFix: true, needMoney: false,
                        invert: false, switchDir: false,
                        relocateBuildingID: 0,
                        out ushort newNode, out var newSegment, out var cost, out var productionRate);
                    if (errors == ToolErrors.None) {
                        panel_.ShowNode(newNode);
                        SelectedNodeID = newNode;
                    }
                });
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
