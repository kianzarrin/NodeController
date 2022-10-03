using NodeController.Tool;
using UnityEngine;
using KianCommons;
using NodeController.Util;

namespace NodeController.GUI {
    class CornerMarker {
        internal Vector3 TerrainPosition; // projected on terrain
        internal Vector3 Position; // original height.
        internal static float Radius = 2.5f;

        /// <summary>
        ///  Intersects mouse ray with marker bounds.
        /// </summary>
        /// <returns><c>true</c>if mouse ray intersects with marker <c>false</c> otherwise</returns>
        internal bool IntersectRay() {
            Ray mouseRay = NodeControllerTool.Camera.ScreenPointToRay(Input.mousePosition);
            NodeControllerTool nctool = NodeControllerTool.Instance;
            float hitH = nctool.GetAccurateHitHeight();

            Vector3 pos = Position;
            float mouseH = nctool.m_mousePosition.y;
            if (hitH < mouseH - KianToolBase.MAX_HIT_ERROR) {
                // For metros use projection on the terrain.
                pos = TerrainPosition;
            } else if (hitH - pos.y > KianToolBase.MAX_HIT_ERROR) {
                // if marker is projected on road plane above then modify its height
                pos.y = hitH;
            }
            Bounds bounds = new Bounds(center: pos, size: Vector3.one * Radius);
            return bounds.IntersectRay(mouseRay);
        }

        public void RenderOverlay(RenderManager.CameraInfo cameraInfo, Color color, bool hovered = false, bool selected = false) {
            float magnification = hovered ? 2f : 1f;
            if (selected) magnification = 2.5f;

            RenderManager.instance.OverlayEffect.DrawCircle(
                cameraInfo,
                color,
                TerrainPosition,
                Radius * magnification,
                TerrainPosition.y - 100f, // through all the geometry -100..100
                TerrainPosition.y + 100f,
                false,
                true);

            RenderManager.instance.OverlayEffect.DrawCircle(
                cameraInfo,
                selected ? Color.white : Color.black,
                TerrainPosition,
                Radius * 0.75f * magnification, // inner circle
                TerrainPosition.y - 100f, // through all the geometry -100..100
                TerrainPosition.y + 100f,
                false,
                false);
        }
    }
}
