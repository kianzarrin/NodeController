namespace NodeController.GUI {
    public class SlopeSlider : UISliderBase {
        public override void Start() {
            base.Start();
            minValue = -180;
            maxValue = 180;
        }

        public override void Reset() {
            var data = Root?.GetData();
            if (data == null) return;

            if (data is SegmentEndData segmentEndData) {
                segmentEndData.DeltaSlopeAngleDeg = 0;
            } else if (data is NodeData nodeData) {
                foreach (var segEndData in nodeData.IterateSegmentEndDatas()) {
                    segEndData.DeltaSlopeAngleDeg = 0;
                }
            }
            data.Update();
            Root.Refresh();
        }

        public override void ApplyNode(NodeData data)
            => data.SlopeAngleDeg = value;

        public override void ApplySegmentEnd(SegmentEndData data)
            => data.SlopeAngleDeg = value;

        public override string TooltipPostfix => " degrees";

        public override void RefreshNode(NodeData data) =>
            MixedValues = !data.HasUniformSlopeAngle();

        public override void RefreshNodeValues(NodeData data) {
            isEnabled = data.CanMassEditNodeCorners();
            if (isEnabled)
                value = data.SlopeAngleDeg;
        }

        public override void RefreshSegmentEndValues(SegmentEndData data) {
            isEnabled = data.CanModifyCorners();
            if (isEnabled)
                value = data.SlopeAngleDeg;
        }
    }
}
