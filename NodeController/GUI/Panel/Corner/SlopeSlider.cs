namespace NodeController.GUI {
    public class SlopeSlider : UISliderBase {
        public override void Start() {
            base.Start();
            minValue = -180;
            maxValue = 180;
        }

        public override void ApplyNode(NodeData data)
            => data.SlopeAngle = value;

        public override void ApplySegmentEnd(SegmentEndData data)
            => data.SlopeAngleDeg = value;

        public override void Refresh() {
            base.Refresh();
            tooltip += " degrees";
            RefreshTooltip();
        }

        public override void RefreshNode(NodeData data) {
            isEnabled = data.CanMassEditNodeCorners();
            if (isEnabled) {
                value = data.SlopeAngle;
                MixedValues = !data.HasUniformSlopeAngle();
            }
        }

        public override void RefreshSegmentEnd(SegmentEndData data) {
            //Log.Debug("UIOffsetSlider.RefreshSegmentEnd() called. this.version=" + this.VersionOf());
            isEnabled = data.CanModifyCorners();
            if(isEnabled)
                value = data.SlopeAngleDeg;
        }
    }
}
