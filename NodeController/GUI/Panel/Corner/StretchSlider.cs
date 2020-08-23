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
            ScrollWheelAmount = 1;
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
            isEnabled = data.CanMassEditNodeCorners();
            if (isEnabled) {
                value = data.Stretch;
                MixedValues = !data.HasUniformStretch();
            }
        }

        public override void RefreshSegmentEnd(SegmentEndData data) {
            //Log.Debug("UIOffsetSlider.RefreshSegmentEnd() called. this.version=" + this.VersionOf());
            isEnabled = data.CanModifyCorners();
            if (isEnabled)
                value = data.Stretch;
        }
    }
}
