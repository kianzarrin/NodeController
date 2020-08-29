using UnityEngine;

namespace NodeController.GUI {
    public class UIOffsetSlider : UISliderBase {
        public override void ApplyNode(NodeData data) =>
            data.CornerOffset = value;

        public override void ApplySegmentEnd(SegmentEndData data) =>
            data.CornerOffset = value;

        public override void RefreshNode(NodeData data) =>
            MixedValues = !data.HasUniformCornerOffset();

        public override string TooltipPostfix => "m";

        public override void RefreshNodeValues(NodeData data) {
            isEnabled = data.CanModifyOffset();
            if (isEnabled)
                value = data.CornerOffset;
        }

        public override void RefreshSegmentEndValues(SegmentEndData data) {
            isEnabled = data.CanModifyOffset();
            if(isEnabled)
                value = data.CornerOffset;
        }
    }
}
