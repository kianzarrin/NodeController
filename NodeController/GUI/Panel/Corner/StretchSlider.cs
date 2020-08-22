namespace NodeController.GUI {
    using System;
    using ColossalFramework.UI;
    using KianCommons;
    using static KianCommons.HelpersExtensions;

    public class StretchSlider : UISliderBase {
        public override void Start() {
            base.Start();
            minValue = -100;
            maxValue = 1000;
            stepSize = 1;
        }

        protected override void OnMouseWheel(UIMouseEventParameter p) {
            scrollWheelAmount = 1f;
            if (ShiftIsPressed) scrollWheelAmount *= 10f;
            base.OnMouseWheel(p);
        }

        public override void ApplyNode(NodeData data) 
            => data.Stretch = value;

        public override void ApplySegmentEnd(SegmentEndData data)
            => data.Stretch = value;

        public override void Refresh() {
            base.Refresh();
            tooltip += "%";
            RefreshTooltip();
        }

        public override void RefreshNode(NodeData data) {
            value = data.Stretch;
            MixedValues = !data.HasUniformStretch();
            isEnabled = data.CanMassEditNodeCorners();
        }

        public override void RefreshSegmentEnd(SegmentEndData data) {
            //Log.Debug("UIOffsetSlider.RefreshSegmentEnd() called. this.version=" + this.VersionOf());
            value = data.Stretch;
            isEnabled = data.CanModifyCorners();
        }
    }
}
