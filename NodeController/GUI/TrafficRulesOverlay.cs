namespace NodeController.GUI {
    using ColossalFramework;
    using NodeController.Util;
    using TrafficManager.API.Manager;
    using TrafficManager.API.Traffic.Data;
    using TrafficManager.Manager.Impl;
    using TrafficManager.UI.Textures;
    using UnityEngine;
    using Log = Util.Log;

    /// <summary>
    /// Class handles rendering of priority signs overlay.
    /// Create one and set its fields before calling DrawSignHandles
    /// </summary>
    public struct TrafficRulesOverlay {
        private const float SIGN_SIZE_PIXELS = 80f;
        private const float AVERAGE_METERS_PER_PIXEL = 0.075f;
        private const float SIGN_SIZE_METERS = SIGN_SIZE_PIXELS * AVERAGE_METERS_PER_PIXEL;
        private const float VIEW_SIZE_RATIO = 0.8f;

        // are sprites clickable?
        private readonly bool handleClick_;

        /// <summary>
        /// Handles layout for the Junction Restriction signs being rendered.
        /// One <see cref="SignsLayout"/> is created per junction.
        /// Defines basis of rotated coordinate system, aligned somewhere near the node center,
        /// and directed along the road segment.
        /// </summary>
        private struct SignsLayout {
            /// <summary>starting point to draw signs.</summary>
            private readonly Vector3 origin_;

            /// <summary>normalized vector across segment (sideways).
            /// dirX_ is not necessarily perpendicular to dirY_ for asymmetrical junctions.
            /// </summary>
            private readonly Vector3 dirX_;

            /// <summary>normalized vector going away from the node.</summary>
            private readonly Vector3 dirY_;

            private readonly int signsPerRow_;

            /// <summary>Zoom level inherited from the MainTool.</summary>
            private readonly float baseZoom_;

            // outermost position to start drawing signs in x direction (sideways).
            private float startX_;

            /// <summary>How many signs been drawn to calculate the position of the new sign.</summary>
            private int counter_;

            public SignsLayout(ushort segmentId,
                               bool startNode,
                               int signsPerRow,
                               float baseZoom) {
                int segmentEndIndex = ExtSegmentEndManager.Instance.GetIndex(segmentId, startNode);
                ref ExtSegmentEnd segmentEnd = ref ExtSegmentEndManager.Instance.ExtSegmentEnds[segmentEndIndex];

                dirX_ = (segmentEnd.LeftCorner - segmentEnd.RightCorner).normalized;

                // for curved angled segements, corner1Direction may slightly differ from corner2Direction
                dirY_ = (segmentEnd.LeftCornerDir + segmentEnd.RightCornerDir) * 0.5f;

                // origin point to start drawing sprites from.
                origin_ = (segmentEnd.LeftCorner + segmentEnd.RightCorner) * 0.5f;

                this.signsPerRow_ = signsPerRow;
                this.baseZoom_ = baseZoom;
                this.counter_ = 0;

                float lenX = SIGN_SIZE_METERS * (signsPerRow - 1);
                this.startX_ = -lenX * 0.5f;
            }

            public bool DrawSign(bool handleClick,
                                 ref Vector3 camPos,
                                 Color guiColor,
                                 Texture2D signTexture) {
                int col = counter_ / signsPerRow_;
                int row = counter_ - (col * signsPerRow_);
                counter_++;

                // +0.5f so that the signs don't cover crossings.
                Vector3 signCenter =
                    origin_ +
                    ((SIGN_SIZE_METERS * (col + 0.5f)) * dirY_) +
                    (((SIGN_SIZE_METERS * row) + startX_) * dirX_);

                bool visible = TMPEUtils.WorldToScreenPoint(
                    worldPos: signCenter, screenPos: out Vector3 signScreenPos);
                if (!visible) {
                    return false;
                }

                Vector3 diff = signCenter - camPos;
                float zoom = 100.0f * baseZoom_ / diff.magnitude;
                float size = SIGN_SIZE_PIXELS * zoom;
                if (!handleClick) size *= 0.75f * VIEW_SIZE_RATIO;

                var boundingBox = new Rect(
                    x: signScreenPos.x - (size * 0.5f),
                    y: signScreenPos.y - (size * 0.5f),
                    width: size,
                    height: size);

                bool hoveredHandle = handleClick && TMPEUtils.IsMouseOver(boundingBox);
                if (!handleClick) {
                    // Readonly signs look grey-ish
                    guiColor = Color.Lerp(guiColor, Color.gray, 0.5f);
                    guiColor.a = TMPEUtils.GetHandleAlpha(hovered: false);
                } else {
                    // Handles in edit mode are always visible. Hovered handles are also highlighted.
                    guiColor.a = 1f;

                    if (hoveredHandle) {
                        guiColor = Color.Lerp(
                            a: guiColor,
                            b: new Color(r: 1f, g: .7f, b: 0f),
                            t: 0.5f);
                    }
                }
                // guiColor.a = TrafficManagerTool.GetHandleAlpha(hoveredHandle);

                GUI.color = guiColor;
                GUI.DrawTexture(boundingBox, signTexture);
                return hoveredHandle;
            }
        }

        public bool DrawTrafficLightSign(
            ushort nodeId,
            float baseZoom,
            bool handleClick,
            ref Vector3 camPos,
            Color guiColor,
            Texture2D signTexture) {


            // +0.5f so that the signs don't cover crossings.
            Vector3 signCenter = nodeId.ToNode().m_position;
            bool visible = TMPEUtils.WorldToScreenPoint(
                worldPos: signCenter, screenPos: out Vector3 signScreenPos);
            if (!visible) {
                return false;
            }

            Vector3 diff = signCenter - camPos;
            float zoom = 100.0f * baseZoom / diff.magnitude;
            float size = SIGN_SIZE_PIXELS * zoom;
            if (!handleClick) size *= 0.75f * VIEW_SIZE_RATIO;

            var boundingBox = new Rect(
                x: signScreenPos.x - (size * 0.5f),
                y: signScreenPos.y - (size * 0.5f),
                width: size,
                height: size);

            bool hoveredHandle = handleClick && TMPEUtils.IsMouseOver(boundingBox);
            if (!handleClick) {
                // Readonly signs look grey-ish
                guiColor = Color.Lerp(guiColor, Color.gray, 0.5f);
                guiColor.a = TMPEUtils.GetHandleAlpha(hovered: false);
            } else {
                // Handles in edit mode are always visible. Hovered handles are also highlighted.
                guiColor.a = 1f;

                if (hoveredHandle) {
                    guiColor = Color.Lerp(
                        a: guiColor,
                        b: new Color(r: 1f, g: .7f, b: 0f),
                        t: 0.5f);
                }
            }
            // guiColor.a = TrafficManagerTool.GetHandleAlpha(hoveredHandle);

            GUI.color = guiColor;
            GUI.DrawTexture(boundingBox, signTexture);
            return hoveredHandle;
        }

        bool CheckClicked =>
            Event.current.type == EventType.MouseDown &&
            Event.current.button == 0;

        /// <summary>Initializes a new instance of the <see cref="TrafficRulesOverlay"/> struct for rendering.</summary>
        /// <param name="mainTool">Parent <see cref="TrafficManagerTool"/>.</param>
        /// <param name="debug">Is debug rendering on.</param>
        /// <param name="handleClick">Whether clicks are to be handled.</param>
        public TrafficRulesOverlay(bool handleClick) {
            handleClick_ = handleClick;
        }

        /// <summary>
        /// Draws clickable or readonly sign handles for all segments in the junction.
        /// </summary>
        /// <param name="nodeId">Junction node id.</param>
        /// <param name="node">Junction node ref.</param>
        /// <param name="camPos">Camera position.</param>
        /// <param name="stateUpdated">?</param>
        /// <returns>Whether any of the signs was hovered.</returns>
        public bool DrawSignHandles(ushort nodeId,
                                    ref Vector3 camPos,
                                    out bool stateUpdated) {
            ref NetNode node = ref nodeId.ToNode();
            bool isAnyHovered = false;
            stateUpdated = false;

            NodeData nodeData = NodeManager.Instance.buffer[nodeId];
            if (nodeData == null) {
                return false;
            }

            // NetManager netManager = Singleton<NetManager>.instance;
            Color guiColor = GUI.color;
            // Vector3 nodePos = Singleton<NetManager>.instance.m_nodes.m_buffer[nodeId].m_position;
            IExtSegmentEndManager segEndMan = ExtSegmentEndManager.Instance;

            for (int i = 0; i < 8; ++i) {
                ushort segmentId = nodeId.ToNode().GetSegment(i);

                if (segmentId == 0) {
                    continue;
                }

                bool isStartNode =
                    (bool)NetUtil.IsStartNode(segmentId, nodeId);

                bool isIncoming = segEndMan
                                  .ExtSegmentEnds[segEndMan.GetIndex(segmentId, isStartNode)]
                                  .incoming;

                NetInfo segmentInfo = Singleton<NetManager>
                                      .instance
                                      .m_segments
                                      .m_buffer[segmentId]
                                      .Info;

                ItemClass connectionClass = segmentInfo.GetConnectionClass();

                if (connectionClass.m_service != ItemClass.Service.Road) {
                    continue; // only for road junctions
                }

                SignsLayout signsLayout = new SignsLayout(
                    segmentId: segmentId,
                    startNode: isStartNode,
                    signsPerRow: isIncoming ? 2 : 1,
                    baseZoom: TMPEUtils.GetBaseZoom());

                JunctionRestrictionsManager jrMan = JunctionRestrictionsManager.Instance;

                #region UTURN
                // draw "u-turns allowed" sign at (1; 0)
                bool allowed = jrMan.IsUturnAllowed(segmentId, isStartNode);
                bool configurable = jrMan.IsUturnAllowedConfigurable(
                    segmentId: segmentId,
                    startNode: isStartNode,
                    node: ref node);
                bool isDefault = allowed == jrMan.GetDefaultUturnAllowed(
                    segmentId: segmentId, startNode: isStartNode, node: ref node);

                {
                    bool signHovered = signsLayout.DrawSign(
                        handleClick: configurable && handleClick_,
                        camPos: ref camPos,
                        guiColor: guiColor,
                        signTexture: allowed
                                         ? JunctionRestrictions.UturnAllowed
                                         : JunctionRestrictions.UturnForbidden);

                    if (signHovered && handleClick_ && configurable) {
                        isAnyHovered = true;

                        if (CheckClicked) {
                            if (!jrMan.ToggleUturnAllowed(
                                    segmentId,
                                    isStartNode)) {
                                // TODO MainTool.ShowTooltip(Translation.GetString("..."), Singleton<NetManager>.instance.m_nodes.m_buffer[nodeId].m_position);
                            } else {
                                stateUpdated = true;
                            }
                        }
                    }
                }
                #endregion

                #region keep clear
                // draw "entering blocked junctions allowed" sign at (0; 1)
                allowed = jrMan.IsEnteringBlockedJunctionAllowed(
                    segmentId: segmentId,
                    startNode: isStartNode);
                configurable = jrMan.IsEnteringBlockedJunctionAllowedConfigurable(
                    segmentId: segmentId,
                    startNode: isStartNode,
                    node: ref node);
                isDefault = allowed == jrMan.GetDefaultEnteringBlockedJunctionAllowed(
                    segmentId, isStartNode, ref node);
                {
                    bool signHovered = signsLayout.DrawSign(
                        handleClick: configurable && handleClick_,
                        camPos: ref camPos,
                        guiColor: guiColor,
                        signTexture: allowed
                                         ? JunctionRestrictions.EnterBlockedJunctionAllowed
                                         : JunctionRestrictions.EnterBlockedJunctionForbidden);

                    if (signHovered && this.handleClick_ && configurable) {
                        isAnyHovered = true;

                        if (CheckClicked) {
                            Log.Debug($"calling ToggleEnteringBlockedJunctionAllowed() for {segmentId} ...");
                            jrMan.ToggleEnteringBlockedJunctionAllowed(
                                segmentId: segmentId,
                                startNode: isStartNode);
                            stateUpdated = true;
                        }
                    }
                }
                #endregion

                #region Zebra crossings
                // draw "pedestrian crossing allowed" sign at (1; 1)
                allowed = jrMan.IsPedestrianCrossingAllowed(
                    segmentId: segmentId,
                    startNode: isStartNode);
                configurable = jrMan.IsPedestrianCrossingAllowedConfigurable(
                    segmentId: segmentId,
                    startNode: isStartNode,
                    node: ref node);



                //{
                //    bool defaultVal = jrMan.GetDefaultPedestrianCrossingAllowed(
                //        segmentId, isStartNode, ref node);
                //    isDefault = allowed == defaultVal;

                //    SegmentEndFlags flags = jrMan.GetFlags(segmentId, isStartNode);

                //    //TernaryBool saved_allowed = jrMan.GetPedestrianCrossingAllowed(segmentId, isStartNode);

                //    Log.Debug($"pedestrian crossing for segment:segmentId node:{nodeId}\n" +
                //        $"    configuragle={configurable} IsAllowed={allowed}  GetDefault={defaultVal}\n" +
                //        $"    saved_allowed={flags.pedestrianCrossingAllowed} saved_default={flags.defaultPedestrianCrossingAllowed}");

                //}

                {
                    bool signHovered = signsLayout.DrawSign(
                        handleClick: configurable && handleClick_,
                        camPos: ref camPos,
                        guiColor: guiColor,
                        signTexture: allowed
                            ? JunctionRestrictions.PedestrianCrossingAllowed
                            : JunctionRestrictions.PedestrianCrossingForbidden);

                    if (signHovered && this.handleClick_ && configurable) {
                        isAnyHovered = true;

                        if (CheckClicked) {
                            jrMan.TogglePedestrianCrossingAllowed(
                                segmentId,
                                isStartNode);
                            stateUpdated = true;
                        }
                    }
                }
                #endregion

            }


            #region traffic light
            {
                var tlman = TrafficLightManager.Instance;
                // draw "entering blocked junctions allowed" sign at (0; 1)
                bool allowed = tlman.HasTrafficLight(
                        nodeId,
                        ref node);

                bool configurable = tlman.CanToggleTrafficLight(
                    nodeId: nodeId,
                    flag: !allowed,
                    ref node,
                    out _);
                {
                    Texture2D overlayTex;
                    if (TrafficLightSimulationManager.Instance.HasTimedSimulation(nodeId)) {
                        overlayTex = TrafficLightTextures.TrafficLightEnabledTimed;
                    } else if (allowed) {
                        // Render traffic light icon
                        overlayTex = TrafficLightTextures.TrafficLightEnabled;
                    } else {
                        // Render traffic light possible but disabled icon
                        overlayTex = TrafficLightTextures.TrafficLightDisabled;
                    }

                    bool signHovered = DrawTrafficLightSign(
                        nodeId:nodeId,
                        baseZoom: TMPEUtils.GetBaseZoom(),
                        handleClick: configurable && handleClick_,
                        camPos: ref camPos,
                        guiColor: guiColor,
                        signTexture: overlayTex);

                    if (signHovered && this.handleClick_ && configurable) {
                        isAnyHovered = true;

                        if (CheckClicked) {
                            Log.Debug($"calling ToggleTrafficLight() for {nodeId} ...");
                            tlman.ToggleTrafficLight(nodeId, ref node);
                            stateUpdated = true;
                        }
                    }
                }
            }
            #endregion



            guiColor.a = 1f;
            GUI.color = guiColor;

            return isAnyHovered;
        }
    } // end class
}