using BlendRoadManager.Util;
using ColossalFramework.UI;
using System;
using UnityEngine;

namespace BlendRoadManager.Tool {
    using ColossalFramework;
    using GUI;
    using static Util.RenderUtil;

    public sealed class BlendRoadTool : KianToolBase {
        UIButton button_;
        UINodeControllerPanel panel_;

        protected override void Awake() {
            button_ = ToolButton.Create();
            panel_ = UINodeControllerPanel.Create();
            base.Awake();
        }

        public static BlendRoadTool Create() {
            Log.Debug("PedBridgeTool.Create()");
            GameObject toolModControl = ToolsModifierControl.toolController.gameObject;
            var tool = toolModControl.GetComponent<BlendRoadTool>() ?? toolModControl.AddComponent<BlendRoadTool>();
            return tool;
        }

        public static BlendRoadTool Instance {
            get {
                GameObject toolModControl = ToolsModifierControl.toolController?.gameObject;
                return toolModControl?.GetComponent<BlendRoadTool>();
            }
        }

        public static void Remove() {
            Log.Debug("PedBridgeTool.Remove()");
            var tool = Instance;
            if (tool != null)
                Destroy(tool);
        }

        protected override void OnDestroy() {
            Log.Debug("PedBridgeTool.OnDestroy()\n" + Environment.StackTrace);
            button_?.Hide();
            Destroy(button_);
            panel_?.Hide();
            Destroy(panel_);
            base.OnDestroy();
        }

        //public override void EnableTool() => ToolsModifierControl.SetTool<PedBridgeTool>();

        protected override void OnEnable() {
            Log.Debug("PedBridgeTool.OnEnable");
            button_?.Focus();
            base.OnEnable();
            button_?.Focus();
            button_?.Invalidate();
            panel_?.Close();
            SelectedNodeID = 0;
        }

        protected override void OnDisable() {
            Log.Debug("PedBridgeTool.OnDisable");
            button_?.Unfocus();
            base.OnDisable();
            button_?.Unfocus();
            button_?.Invalidate();
            panel_?.Close();
            SelectedNodeID = 0;
        }

        protected override void OnToolUpdate() {
            base.OnToolUpdate();
            ToolCursor = HoverValid ? NetUtil.netTool.m_upgradeCursor : null;
        }

        Vector3 _cachedHitPos;
        public ushort SelectedNodeID;
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo) {
            base.RenderOverlay(cameraInfo);
            if (SelectedNodeID != 0) {
                DrawNodeCircle(cameraInfo, Color.white, SelectedNodeID, false);
            }
            if (!HoverValid)
                return;
            if (SelectedNodeID != HoveredNodeId && NodeData.IsSupported(HoveredNodeId)) {
                DrawNodeCircle(cameraInfo, Color.yellow, HoveredNodeId, false);
            }
            DrawOverlayCircle(cameraInfo, Color.red, HitPos, 1, true);
        }

        protected override void OnPrimaryMouseClicked() {
            if (!HoverValid)
                return;
            Log.Info($"OnPrimaryMouseClicked: segment {HoveredSegmentId} node {HoveredNodeId}");
            if (NodeData.IsSupported(HoveredNodeId)) {
                panel_.ShowNode(HoveredNodeId);
                SelectedNodeID = HoveredNodeId;
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
