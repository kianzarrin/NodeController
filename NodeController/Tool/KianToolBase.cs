namespace NodeController.Tool {
    using ColossalFramework.UI;
    using UnityEngine;
    using KianCommons;

    public abstract class KianToolBase : ToolBase
    {
        public bool ToolEnabled => ToolsModifierControl.toolController?.CurrentTool == this;

        protected override void OnDestroy() {
            DisableTool();
            base.OnDestroy();
        }

        protected abstract void OnPrimaryMouseClicked();
        protected abstract void OnSecondaryMouseClicked();

        public void ToggleTool()
        {
            Log.Debug("ToggleTool: called");
            if (!ToolEnabled)
                EnableTool();
            else
                DisableTool();
        }

        public void EnableTool()
        {
            Log.Debug("EnableTool: called");
            //WorldInfoPanel.HideAllWorldInfoPanels();
            //GameAreaInfoPanel.Hide();
            ToolsModifierControl.toolController.CurrentTool = this;
        }

        public void DisableTool()
        {
            Log.Debug("DisableTool: called");
            if(ToolsModifierControl.toolController?.CurrentTool == this)
                ToolsModifierControl.SetTool<DefaultTool>();
        }

        protected override void OnToolGUI(Event e) {
            base.OnToolGUI(e);
            if (e.type == EventType.MouseUp && m_mouseRayValid) {
                if (e.button == 0) OnPrimaryMouseClicked();
                else if (e.button == 1) OnSecondaryMouseClicked();
            }
        }

        #region hover
        internal Ray m_mouseRay;
        internal float m_mouseRayLength;
        internal bool m_mouseRayValid;
        internal Vector3 m_mousePosition;
        internal RaycastOutput raycastOutput;

        public override void SimulationStep() {
            IsHoverValid = DetermineHoveredElements();
        }

        protected override void OnToolLateUpdate()
        {
            base.OnToolUpdate();
            m_mousePosition = Input.mousePosition;
            m_mouseRay = Camera.main.ScreenPointToRay(m_mousePosition);
            m_mouseRayLength = Camera.main.farClipPlane;
            m_mouseRayValid = !UIView.IsInsideUI() && Cursor.visible;
        }

        public ushort HoveredNodeId { get; private set; } = 0;
        public ushort HoveredSegmentId { get; private set; } = 0;
        protected bool IsHoverValid { get;private set; }

        protected bool DetermineHoveredElements()
        {
            HoveredSegmentId = 0;
            HoveredNodeId = 0;
            if (!m_mouseRayValid)
            {
                return false;
            }

            // find currently hovered node
            RaycastInput nodeInput = new RaycastInput(m_mouseRay, m_mouseRayLength)
            {
                m_netService = {
                        // find road segments
                        m_itemLayers = ItemClass.Layer.Default | ItemClass.Layer.MetroTunnels,
                        m_service = ItemClass.Service.Road
                    },
                m_ignoreTerrain = true,
                m_ignoreNodeFlags = NetNode.Flags.None
            };

            if (RayCast(nodeInput, out raycastOutput))
            {
                HoveredNodeId = raycastOutput.m_netNode;
            }

            HoveredSegmentId = GetSegmentFromNode(raycastOutput.m_hitPos);

            if (HoveredSegmentId != 0) {
                Debug.Assert(HoveredNodeId != 0, "unexpected: HoveredNodeId == 0");
                return true;
            }

            // find currently hovered segment
            var segmentInput = new RaycastInput(m_mouseRay, m_mouseRayLength)
            {
                m_netService = {
                    // find road segments
                    m_itemLayers = ItemClass.Layer.Default | ItemClass.Layer.MetroTunnels,
                    m_service = ItemClass.Service.Road
                },
                m_ignoreTerrain = true,
                m_ignoreSegmentFlags = NetSegment.Flags.None
            };

            if (RayCast(segmentInput, out raycastOutput))
            {
                HoveredSegmentId = raycastOutput.m_netSegment;
            }


            if (HoveredNodeId <= 0 && HoveredSegmentId > 0)
            {
                // alternative way to get a node hit: check distance to start and end nodes
                // of the segment
                ushort startNodeId = HoveredSegmentId.ToSegment().m_startNode;
                ushort endNodeId = HoveredSegmentId.ToSegment().m_endNode;

                var vStart = raycastOutput.m_hitPos - startNodeId.ToNode().m_position;
                var vEnd = raycastOutput.m_hitPos - endNodeId.ToNode().m_position;

                float startDist = vStart.magnitude;
                float endDist = vEnd.magnitude;

                if (startDist < endDist && startDist < 75f)
                {
                    HoveredNodeId = startNodeId;
                }
                else if (endDist < startDist && endDist < 75f)
                {
                    HoveredNodeId = endNodeId;
                }
            }
            return HoveredNodeId != 0 || HoveredSegmentId != 0;
        }

        internal ushort GetSegmentFromNode(Vector3 hitPos) {
            if (HoveredNodeId == 0) {
                return 0;
            }
            ushort minSegId = 0;
            NetNode node = NetManager.instance.m_nodes.m_buffer[HoveredNodeId];
            float minDistance = float.MaxValue;
            foreach (ushort segmentId in NetUtil.IterateNodeSegments(HoveredNodeId)) {
                Vector3 pos = segmentId.ToSegment().GetClosestPosition(hitPos);
                float distance = (hitPos - pos).sqrMagnitude;
                if (distance < minDistance) {
                    minDistance = distance;
                    minSegId = segmentId;
                }
            }
            return minSegId;
        }

        private static float prev_H = 0f;
        private static float prev_H_Fixed;

        /// <summary>Maximum error of HitPos field.</summary>
        internal const float MAX_HIT_ERROR = 2.5f;

        /// <summary>
        /// Calculates accurate vertical element of raycast hit position.
        /// </summary>
        internal float GetAccurateHitHeight() {
            // cache result.
            float current_hitH = raycastOutput.m_hitPos.y;
            if (KianCommons.Math.MathUtil.EqualAprox(current_hitH, prev_H,1e-12f)) {
                return prev_H_Fixed;
            }
            prev_H = current_hitH;

            if (HoveredSegmentId.ToSegment().GetClosestLanePosition(
                raycastOutput.m_hitPos,
                NetInfo.LaneType.All,
                VehicleInfo.VehicleType.All,
                out Vector3 pos,
                out uint laneId,
                out int laneIndex,
                out float laneOffset)) {
                return prev_H_Fixed = pos.y;
            }
            return prev_H_Fixed = current_hitH + 0.5f;
        }

        #endregion
    }
}
