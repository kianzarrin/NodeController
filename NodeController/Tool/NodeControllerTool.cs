namespace NodeController.Tool {
    using ColossalFramework;
    using KianCommons;
    using KianCommons.UI;
    using NodeController.GUI;
    using System.Threading;
    using UnityEngine;
    using static KianCommons.HelpersExtensions;
    using static KianCommons.UI.RenderUtil;

    public sealed class NodeControllerTool : KianToolBase {
        public static readonly SavedInputKey ActivationShortcut = new SavedInputKey(
            "ActivationShortcut",
            Settings.FileName,
            SavedInputKey.Encode(KeyCode.N, true, false, false),
            true);

        public static readonly SavedBool SnapToMiddleNode = new SavedBool(
            "SnapToMiddleNode", Settings.FileName, def: false, true);

        NodeControllerButton Button => NodeControllerButton.Instace;
        UINodeControllerPanel NCPanel;
        UISegmentEndControllerPanel SECPanel;

        NetTool.ControlPoint m_controlPoint;
        NetTool.ControlPoint m_cachedControlPoint;
        ToolErrors m_errors;
        ToolErrors m_cachedErrors;
        NetInfo m_prefab;

        private object m_cacheLock = new object();

        private CursorInfo CursorInsert, CursorInsertCrossing,
            CursorEdit, CursorSearching, CursorError;

        ref SegmentEndData SelectedSegmentEndData => ref SegmentEndManager.Instance.GetAt(segmentID: SelectedSegmentID, nodeID: SelectedNodeID);

        protected override void Awake() {
            Log.Debug("NodeControllerTool.Awake() called");
            base.Awake();

            NodeControllerButton.CreateButton();
            NCPanel = UINodeControllerPanel.Create();
            SECPanel = UISegmentEndControllerPanel.Create();

            // A)modify node: green pen
            // B)insert middle (highway) green node
            // C)insert pedestrian : green pedestrian
            // D)searching(mouse is not hovering over road) grey geerbox
            // E)fail insert red geerbox
            // F)fail modify (end node) red geerbox.
            // G)inside panel: normal

            CursorEdit = ScriptableObject.CreateInstance<CursorInfo>();
            CursorEdit.m_texture = TextureUtil.GetTextureFromFile("cursor_edit.png"); // green pen
            CursorEdit.m_hotspot = new Vector2(5f, 0f);

            CursorInsert = ScriptableObject.CreateInstance<CursorInfo>();
            CursorInsert.m_texture = TextureUtil.GetTextureFromFile("cursor_insert.png"); // green T node
            CursorInsert.m_hotspot = new Vector2(5f, 0f);

            CursorInsertCrossing = ScriptableObject.CreateInstance<CursorInfo>();
            CursorInsertCrossing.m_texture = TextureUtil.GetTextureFromFile("cursor_insert_crossing.png"); // green crossing.
            CursorInsertCrossing.m_hotspot = new Vector2(5f, 0f);

            CursorError = ScriptableObject.CreateInstance<CursorInfo>();
            CursorError.m_texture = TextureUtil.GetTextureFromFile("cursor_error.png"); // red gear
            CursorError.m_hotspot = new Vector2(5f, 0f);

            CursorSearching = ScriptableObject.CreateInstance<CursorInfo>();
            CursorSearching.m_texture = TextureUtil.GetTextureFromFile("cursor_searching.png"); // grey gear
            CursorSearching.m_hotspot = new Vector2(5f, 0f);
        }

        public static NodeControllerTool Create() {
            Log.Debug("NodeControllerTool.Create()");
            GameObject toolModControl = ToolsModifierControl.toolController.gameObject;
            //var tool = toolModControl.GetComponent<NodeControllerTool>() ?? toolModControl.AddComponent<NodeControllerTool>();
            var tool = toolModControl.AddComponent<NodeControllerTool>();
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
            Log.Debug("NodeControllerTool.OnDestroy()\n" /*+ Environment.StackTrace*/);
            Button?.Hide();
            Destroy(Button);
            NCPanel?.Hide();
            SECPanel?.Hide();
            Destroy(SECPanel);
            Destroy(NCPanel);
            base.OnDestroy();
        }

        protected override void OnEnable() {
            Log.Debug("NodeControllerTool.OnEnable");
            Log.Debug(Button?.ToString());
            base.OnEnable();
            Button?.Activate();
            NCPanel?.Close();
            SelectedNodeID = 0;
            SelectedSegmentID = 0;
            handleHovered_ = false;
        }

        protected override void OnDisable() {
            Log.Debug("NodeControllerTool.OnDisable" /*+ Environment.StackTrace*/);
            base.OnDisable();
            Button?.Deactivate();
            NCPanel?.Close();
            SECPanel?.Close();
            SelectedNodeID = 0;
            SelectedSegmentID = 0;
        }

        override public void SimulationStep() {
            base.SimulationStep();
            if (CornerFocusMode) {
                SegmentEndData segEnd = SelectedSegmentEndData;
                if (SelectedSegmentEndData == null) return;
                bool positionChanged = false;
                if (leftCornerSelected_) {
                    var pos = RaycastMouseLocation(segEnd.CachedLeftCornerPos.y);
                    positionChanged = segEnd.MoveLeftCornerToAbsolutePos(pos);
                } else if (rightCornerSelected_) {
                    var pos = RaycastMouseLocation(segEnd.CachedRightCornerPos.y);
                    positionChanged = segEnd.MoveRightCornerToAbsolutePos(pos);
                }
                if (positionChanged) {
                    SimulationManager.instance.m_ThreadingWrapper.QueueMainThread(delegate () {
                        SECPanel.RefreshTableValuesOnly();
                    });
                }
                return;
            }


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

        CursorInfo GetCursor() {
            // A)modify node: green pen
            // B)insert middle (highway) green node
            // C)insert pedestrian : green pedestrian
            // D)searching(mouse is not hovering over road) grey geerbox
            // E)fail insert red geerbox
            // F)fail modify (end node) red geerbox.
            // G)inside panel: normal

            if (!m_mouseRayValid) // G
                return null;

            bool fail = false;
            bool insert = false;
            bool searching = false;
            bool edit = false;
            bool crossing = false;

            if (IsHoverValid && m_prefab != null) {
                NetTool.ControlPoint controlPoint = m_cachedControlPoint;
                ushort nodeID = controlPoint.m_node;
                edit = nodeID != 0;
                insert = controlPoint.m_segment != 0;
                if (edit) {
                    fail = !NodeData.IsSupported(nodeID);
                } else if (AltIsPressed) {
                    searching = true;
                } else if (insert) {
                    bool isRoad = m_prefab.m_netAI is RoadBaseAI && !NetUtil.IsCSUR(m_prefab);
                    ToolErrors error = m_cachedErrors;
                    error |= m_prefab.m_netAI.CheckBuildPosition(false, false, true, true, ref controlPoint, ref controlPoint, ref controlPoint, out _, out _, out _, out _);
                    fail = error != ToolErrors.None || !isRoad;
                    crossing = m_prefab.CountPedestrianLanes() >= 2;
                }
            } else
                searching = true;

            if (searching)
                return CursorSearching;

            if (fail)
                return CursorError;

            if (insert && crossing)
                return CursorInsertCrossing;

            if (insert && !crossing)
                return CursorInsert;

            if (edit)
                return CursorEdit;

            return null; // race condition
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

            ToolCursor = GetCursor();
        }

        protected override void OnToolLateUpdate() {
            base.OnToolLateUpdate();
            ForceInfoMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.None);
        }

        public Color GetColor(bool error) {
            if (error)
                return base.GetToolColor(false, true);
            Color c = base.GetToolColor(false, false);
            Color ret = Color.yellow;
            ret.a = base.GetToolColor(false, false).a;
            return ret;
        }

        //Vector3 _cachedHitPos;
        public ushort SelectedNodeID;
        public ushort SelectedSegmentID;

        static bool CanSelectSegmentEnd(ushort segmentID, ushort nodeID) {
            if (nodeID == 0 || segmentID == 0)
                return false;
            if (!NodeData.IsSupported(nodeID))
                return false;
            return nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Junction);
        }

        CornerMarker GetCornerMarker(bool left) {
            var segEnd = SelectedSegmentEndData;
            if (segEnd == null) return null;
            var pos = left? segEnd.CachedLeftCornerPos: segEnd.CachedRightCornerPos;
            float terrainY = Singleton<TerrainManager>.instance.SampleDetailHeightSmooth(pos);
            var ret = new CornerMarker {
                Position = pos,
                TerrainPosition = new Vector3(pos.x, terrainY, pos.z),
            };
            return ret;
        }


        bool leftCornerSelected_ = false, leftCornerHovered_ = false;
        bool rightCornerSelected_ = false, rightCornerHovered_ = false;
        bool CornerFocusMode =>
            SelectedSegmentID != 0 &&
            (leftCornerHovered_ | rightCornerHovered_ | leftCornerSelected_ | rightCornerSelected_);



        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo) {
            base.RenderOverlay(cameraInfo);

            if (SelectedSegmentID != 0) {
                GetCornerMarker(left: true)?.RenderOverlay(cameraInfo, Color.red, leftCornerHovered_, leftCornerSelected_);
                GetCornerMarker(left: false)?.RenderOverlay(cameraInfo, Color.red, rightCornerHovered_, rightCornerSelected_);
            }
            if (CornerFocusMode)
                return;


            if (SelectedSegmentID != 0 && SelectedNodeID != 0) {
                DrawCutSegmentEnd(
                    cameraInfo,
                    SelectedSegmentID,
                    0.5f,
                    NetUtil.IsStartNode(segmentId: SelectedSegmentID, nodeId: SelectedNodeID),
                    Color.white,
                    alpha: true);
                ushort nodeID = SelectedSegmentID.ToSegment().GetOtherNode(SelectedNodeID);
                if (nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Middle))
                    DrawNodeCircle(cameraInfo, Color.gray, nodeID, true);
            } else if (SelectedNodeID != 0) {
                DrawNodeCircle(cameraInfo, Color.white, SelectedNodeID, false);
                foreach (var segmentID in NetUtil.IterateNodeSegments(SelectedNodeID)) {
                    ushort nodeID = segmentID.ToSegment().GetOtherNode(SelectedNodeID);
                    if (nodeID.ToNode().m_flags.IsFlagSet(NetNode.Flags.Middle))
                        DrawNodeCircle(cameraInfo, Color.gray, nodeID, true);
                }
            }
            if (!m_mouseRayValid || handleHovered_)
                return;

            if (AltIsPressed) {
                if (CanSelectSegmentEnd(nodeID: HoveredNodeId, segmentID: HoveredSegmentId)) {
                    DrawCutSegmentEnd(
                        cameraInfo,
                        HoveredSegmentId,
                        0.5f,
                        NetUtil.IsStartNode(segmentId: HoveredSegmentId, nodeId: HoveredNodeId),
                        Color.yellow,
                        alpha: true);
                }
            } else if (IsHoverValid && m_prefab != null) {
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
                    RenderStripOnSegment(cameraInfo, controlPoint.m_segment, controlPoint.m_position, 1.5f, color);
                }
                DrawOverlayCircle(cameraInfo, Color.red, raycastOutput.m_hitPos, 1, true);
            }
        }

        protected override void OnToolGUI(Event e) {
            base.OnToolGUI(e); // calls on click events on mosue up
            CornersGUI(e);
            if (CornerFocusMode) return;
            DrawSigns();
        }

        void CornersGUI(Event e) {
            bool mouseDown = e.type == EventType.mouseDown && e.button == 0;
            bool mouseUp = e.type == EventType.mouseUp && e.button == 0;
            if (SelectedSegmentID == 0) {
                leftCornerHovered_ = rightCornerHovered_ = leftCornerSelected_ = rightCornerSelected_ = false;
                return;
            }

            if (e.type == EventType.mouseDown && e.button == 0) {
                leftCornerSelected_ = leftCornerHovered_ = GetCornerMarker(left: true)?.IntersectRay() ?? false;
            } else if (mouseUp) {
                leftCornerSelected_ = false;
                leftCornerHovered_ = GetCornerMarker(left: true).IntersectRay();
            } else {
                leftCornerHovered_ = leftCornerSelected_ || (GetCornerMarker(left: true)?.IntersectRay() ?? false);
            }
            if (mouseDown) {
                rightCornerSelected_ = rightCornerHovered_ = GetCornerMarker(left: false)?.IntersectRay()??false;
            } else if (mouseUp) {
                rightCornerSelected_ = false;
                rightCornerHovered_ = GetCornerMarker(left: false).IntersectRay();
            } else {
                rightCornerHovered_ = rightCornerSelected_ || (GetCornerMarker(left: false)?.IntersectRay()??false);
            }
        }



        bool handleHovered_;
        private void DrawSigns() {
            Vector3 camPos = Singleton<SimulationManager>.instance.m_simulationView.m_position;
            if (SelectedNodeID == 0) {
                TrafficRulesOverlay overlay =
                    new TrafficRulesOverlay(handleClick: false);
                foreach (NodeData nodeData in NodeManager.Instance.buffer) {
                    if (nodeData == null) continue;
                    overlay.DrawSignHandles(
                        nodeData.NodeID, 0, camPos: ref camPos, out _);
                }
            } else {
                TrafficRulesOverlay overlay =
                    new TrafficRulesOverlay(handleClick: true);
                handleHovered_ = overlay.DrawSignHandles(
                    SelectedNodeID, SelectedSegmentID, camPos: ref camPos, out _);
            }
        }

        protected override void OnPrimaryMouseClicked() {
            if (!IsHoverValid || handleHovered_ || CornerFocusMode)
                return;
            Log.Info($"OnPrimaryMouseClicked: segment {HoveredSegmentId} node {HoveredNodeId}");
            if (AltIsPressed) {
                if (CanSelectSegmentEnd(nodeID: HoveredNodeId, segmentID: HoveredSegmentId)) {
                    SelectedSegmentID = HoveredSegmentId;
                    SelectedNodeID = HoveredNodeId;
                    SECPanel.
                        ShowSegmentEnd(segmentID: SelectedSegmentID, nodeID: SelectedNodeID);
                }
                return;
            }

            if (m_errors != ToolErrors.None || m_prefab == null)
                return;
            var c = m_cachedControlPoint;

            if (c.m_node != 0) {
                bool supported = NodeData.IsSupported(c.m_node);
                if (!supported) {
                    return;
                }
                SelectedNodeID = c.m_node;
                SelectedSegmentID = 0;
                NCPanel.ShowNode(SelectedNodeID);
            } else if (c.m_segment != 0) {
                if (m_prefab.m_netAI is RoadBaseAI && !NetUtil.IsCSUR(m_prefab)) {
                    SimulationManager.instance.AddAction(delegate () {
                        NodeData nodeData = NodeManager.Instance.InsertNode(c);
                        if (nodeData != null) {
                            SelectedNodeID = nodeData.NodeID;
                            SelectedSegmentID = 0;
                            NCPanel.ShowNode(SelectedNodeID);
                        }
                    });
                }
            } else {
                // nothing is hovered.
            }
        }

        protected override void OnSecondaryMouseClicked() {
            handleHovered_ = false;
            if (SelectedNodeID == 0) {
                DisableTool();
            } else {
                NCPanel.Close();
                SECPanel.Close();
                SelectedSegmentID = SelectedNodeID = 0;
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
            foreach (ushort segmentID in NetUtil.IterateNodeSegments(HoveredNodeId)) {
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

            snapNode |= SnapToMiddleNode;

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
