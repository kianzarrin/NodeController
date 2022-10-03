using HarmonyLib;
using KianCommons;
using NodeController.Tool;
using System;
using TrafficManager.API.Manager;
using UnityEngine;

namespace NodeController.Util {
    public static class TMPEUtils {
        public static ITrafficLightManager TL =>
            TrafficManager.API.Implementations.ManagerFactory.TrafficLightManager;

        internal static void TryEnableTL(ushort nodeID) {
            if (!TL.HasTrafficLight(nodeID) && TL.CanToggleTrafficLight(nodeID)) {
                TL.ToggleTrafficLight(nodeID);
            }
        }

        internal static bool WorldToScreenPoint(Vector3 worldPos, out Vector3 screenPos) {
            screenPos = NodeControllerTool.Camera.WorldToScreenPoint(worldPos);
            screenPos.y = Screen.height - screenPos.y;

            return screenPos.z >= 0;
        }
        internal static bool IsMouseOver(Rect boundingBox) {
            return boundingBox.Contains(Event.current.mousePosition);
        }

        delegate float dGetHandleAlphaT_(bool hovered);
        static dGetHandleAlphaT_ dGetHandleAlpha_;
        internal static float GetHandleAlpha(bool hovered) {
            if (dGetHandleAlpha_ == null) {
                var mGetHandleAlpha = AccessTools.DeclaredMethod(
                    typeof(TrafficManager.UI.TrafficManagerTool),
                    "GetHandleAlpha");
                dGetHandleAlpha_ = (dGetHandleAlphaT_)Delegate.CreateDelegate(
                    typeof(dGetHandleAlphaT_), mGetHandleAlpha);
            }
            return dGetHandleAlpha_(hovered);
        }

        internal static float GetBaseZoom() {
            return Screen.height / 1200f;
        }

        internal static Version TMPEVersion =>
            TrafficManager.API.Implementations.ManagerFactory.VersionOf();

        internal static Version TMPEThemesVersion = new Version(11, 6, 4);
    }
}
