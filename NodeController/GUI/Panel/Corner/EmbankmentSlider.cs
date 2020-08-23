namespace NodeController.GUI {
    using UnityEngine;
    using KianCommons;
    using System;

    public class EmbankmentSlider : UISliderBase {
        public override void Start() {
            base.Start();
            minValue = -180;
            maxValue = 180;
        }

        public override void ApplyNode(NodeData data)
            => data.EmbankmentAngle = value;

        public override void ApplySegmentEnd(SegmentEndData data)
            => data.EmbankmentAngleDeg = value;

        public override void Refresh() {
            base.Refresh();
            tooltip += " degrees";
            RefreshTooltip();
        }

        public override void RefreshNode(NodeData data) {
            isEnabled = data.CanMassEditNodeCorners();
            if (isEnabled) {
                value = data.EmbankmentAngle;
                MixedValues = !data.HasUniformEmbankmentAngle();
            }
        }

        public override void RefreshSegmentEnd(SegmentEndData data) {
            isEnabled = data.CanModifyCorners();
            if(isEnabled)
                value = data.EmbankmentAngleDeg;
        }
    }
}
