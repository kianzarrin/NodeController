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

        public override void ApplyNode(NodeData data) => throw new NotImplementedException();

        public override void ApplySegmentEnd(SegmentEndData data)
            => data.EmbankmentAngle = value;

        public override void Refresh() {
            base.Refresh();
            tooltip += " degrees";
            RefreshTooltip();
        }

        public override void RefreshNode(NodeData data) {
            //value = data.EmbankmentAngle;
            // MixedValues = !data.HasUniformEmbankmentAngle();
            isEnabled = data.CanMassEditNodeCorners();
            tooltip += " degrees";
        }

        public override void RefreshSegmentEnd(SegmentEndData data) {
            //Log.Debug("UIOffsetSlider.RefreshSegmentEnd() called. this.version=" + this.VersionOf());
            value = data.EmbankmentAngle;

            isEnabled = data.CanModifyCorners();
            tooltip += " degrees";
        }
    }
}
