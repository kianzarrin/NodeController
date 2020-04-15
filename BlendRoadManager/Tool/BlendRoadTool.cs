using ColossalFramework;
using ColossalFramework.UI;
using System;
using UnityEngine;
using BlendRoadManager.Util;

namespace BlendRoadManager.Tool {
    using static Util.RenderUtil;

    public sealed class BlendRoadTool : KianToolBase {
        UIButton button;

        protected override void Awake() {
            var uiView = UIView.GetAView();
            button = uiView.AddUIComponent(typeof(ToolButton)) as UIButton;
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
            button?.Hide();
            Destroy(button);
            base.OnDestroy();
        }

        //public override void EnableTool() => ToolsModifierControl.SetTool<PedBridgeTool>();

        protected override void OnEnable() {
            Log.Debug("PedBridgeTool.OnEnable");
            button?.Focus();
            base.OnEnable();
            button?.Focus();
            button?.Invalidate();
        }

        protected override void OnDisable() {
            Log.Debug("PedBridgeTool.OnDisable");
            button?.Unfocus();
            base.OnDisable();
            button?.Unfocus();
            button?.Invalidate();

        }

        protected override void OnToolUpdate() {
            base.OnToolUpdate();
            ToolCursor = HoverValid ? NetUtil.netTool.m_upgradeCursor : null;
        }

        Vector3 _cachedHitPos;

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo) {
            base.RenderOverlay(cameraInfo);
            if (!HoverValid)
                return;
            if (IsGood1() || IsGood2())
            {
                DrawNodeCircle(cameraInfo, Color.yellow, HoveredNodeId, false);
            }
            DrawOverlayCircle(cameraInfo, Color.red, HitPos, 1, true);
        }

        protected override void OnPrimaryMouseClicked() {
            if (!HoverValid)
                return;
            Log.Info($"OnPrimaryMouseClicked: segment {HoveredSegmentId} node {HoveredNodeId}");
            if (IsGood1())
            {

            }

        }

        protected override void OnSecondaryMouseClicked() {
            //throw new System.NotImplementedException();
        }

        bool IsGood1(){
            return HoveredNodeId.ToNode().CountSegments() == 2 && HoveredNodeId.ToNode().Info.m_netAI is RoadBaseAI;
        }
        bool IsGood2()
        {
            return HoveredNodeId.ToNode().CountSegments() > 2 && HoveredNodeId.ToNode().Info.m_netAI is RoadBaseAI;
        }


    } //end class
}
