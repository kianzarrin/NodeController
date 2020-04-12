using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;
using System;

using static BlendRoadManager.Util.HelpersExtensions;
using BlendRoadManager.Util;
using ColossalFramework.Math;

namespace BlendRoadManager.Tool {
    public abstract class KianToolBase : DefaultTool
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

        protected override void OnToolUpdate()
        {
            base.OnToolUpdate();
            DetermineHoveredElements();

            if (Input.GetMouseButtonDown(0))
            {
                OnPrimaryMouseClicked();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                OnSecondaryMouseClicked();
            }
        }

        public ushort HoveredNodeId { get; private set; } = 0;
        public ushort HoveredSegmentId { get; private set; } = 0;
        public Vector3 HitPos { get; private set; }

        protected bool IsMouseRayValid => !UIView.IsInsideUI() && Cursor.visible && m_mouseRayValid;
        protected bool HoverValid => IsMouseRayValid && (HoveredSegmentId != 0 || HoveredNodeId != 0);

        private bool DetermineHoveredElements()
        {
            HoveredSegmentId = 0;
            HoveredNodeId = 0;
            HitPos = Vector3.zero;
            if (!IsMouseRayValid)
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

            if (RayCast(nodeInput, out RaycastOutput nodeOutput))
            {
                HoveredNodeId = nodeOutput.m_netNode;
                HitPos = nodeOutput.m_hitPos;
            }

            HoveredSegmentId = GetSegmentFromNode();

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

            if (RayCast(segmentInput, out RaycastOutput segmentOutput))
            {
                HoveredSegmentId = segmentOutput.m_netSegment;
                HitPos = segmentOutput.m_hitPos;
            }


            if (HoveredNodeId <= 0 && HoveredSegmentId > 0)
            {
                // alternative way to get a node hit: check distance to start and end nodes
                // of the segment
                ushort startNodeId = HoveredSegmentId.ToSegment().m_startNode;
                ushort endNodeId = HoveredSegmentId.ToSegment().m_endNode;

                var vStart = segmentOutput.m_hitPos - startNodeId.ToNode().m_position;
                var vEnd = segmentOutput.m_hitPos - endNodeId.ToNode().m_position;

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

        static float GetAgnele(Vector3 v1, Vector3 v2) {
            float ret = Vector3.Angle(v1, v2);
            if (ret > 180) ret -= 180; //future proofing
            ret = Math.Abs(ret);
            return ret;
        }

        internal ushort GetSegmentFromNode() {
            bool considerSegmentLenght = false;
            ushort minSegId = 0;
            if (HoveredNodeId != 0) {
                NetNode node = HoveredNodeId.ToNode();
                Vector3 dir0 = node.m_position - m_mousePosition;
                float min_angle = float.MaxValue;
                for (int i = 0; i < 8; ++i) {
                    ushort segmentId = node.GetSegment(i);
                    if (segmentId == 0)
                        continue;
                    NetSegment segment = segmentId.ToSegment();
                    Vector3 dir;
                    if (segment.m_startNode == HoveredNodeId) {
                        dir = segment.m_startDirection;

                    } else {
                        dir = segment.m_endDirection;
                    }
                    float angle = GetAgnele(-dir,dir0);
                    if(considerSegmentLenght)
                        angle *= segment.m_averageLength;
                    if (angle < min_angle) {
                        min_angle = angle;
                        minSegId = segmentId;
                    }
                }
            }
            return minSegId;
        }

 
    }
}
