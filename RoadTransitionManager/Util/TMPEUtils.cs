using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using HarmonyLib;
using System.Reflection;

namespace NodeController.Util {
    public static class TMPEUtils {
        internal static bool WorldToScreenPoint(Vector3 worldPos, out Vector3 screenPos) {
            screenPos = Camera.main.WorldToScreenPoint(worldPos);
            screenPos.y = Screen.height - screenPos.y;

            return screenPos.z >= 0;
        }
        internal static bool IsMouseOver(Rect boundingBox) {
            return boundingBox.Contains(Event.current.mousePosition);
        }

        static MethodInfo mGetHandleAlpha => AccessTools.DeclaredMethod(
                typeof(TrafficManager.UI.TrafficManagerTool),
                "GetHandleAlpha");

        internal static float GetHandleAlpha(bool hovered) {
            object[] args = new object[] { hovered };
            return (float)mGetHandleAlpha.Invoke(null, args);
        }

        internal static float GetBaseZoom() {
            return Screen.height / 1200f;
        }
    }
}
