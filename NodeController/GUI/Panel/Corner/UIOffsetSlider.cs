namespace NodeController.GUI {

    public class UIOffsetSlider : UISliderBase {
        public override void ApplyNode(NodeData data) {
            //Log.Debug("UIOffsetSlider.ApplyNode() called. this.version=" + this.VersionOf());
            if (data == null) return;
            data.CornerOffset = value;
        }

        public override void ApplySegmentEnd(SegmentEndData data) {
            //Log.Debug("UIOffsetSlider.ApplySegmentEnd() called. this.version=" + this.VersionOf());
            if (data == null) return;
            data.CornerOffset = value;
        }

        public override void RefreshNode(NodeData data) {
            //Log.Debug("UIOffsetSlider.RefreshNode() called. this.version=" + this.VersionOf());
            value = data.CornerOffset;
            MixedValues = !data.HasUniformCornerOffset();
            isEnabled = data.CanModifyOffset();
        }

        public override void RefreshSegmentEnd(SegmentEndData data) {
            //Log.Debug("UIOffsetSlider.RefreshSegmentEnd() called. this.version=" + this.VersionOf());
            value = data.CornerOffset;
            isEnabled = data.CanModifyOffset();
        }
    }
}
