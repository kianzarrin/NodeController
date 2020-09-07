using UnityEngine;

namespace NodeController.GUI {
    public class UIOffsetSlider : UISliderBase {
        public override void Awake() {
            base.Awake();
            CourseDragStep = CourseScrollStep = 0.1f;
            LargeDragStep = LargeScrollStep = 1f;
        }

        public override void ApplyNode(NodeData data) =>
            data.CornerOffset = value;

        public override void Reset() {
            var data = Root?.GetData();
            if (data is SegmentEndData segmentEndData) {
                value = segmentEndData.DefaultCornerOffset;
            } else if (data is NodeData nodeData) {
                foreach (var segEndData in nodeData.IterateSegmentEndDatas()) {
                    value = segEndData.DefaultCornerOffset;
                }
            }
        }

        public override void ApplySegmentEnd(SegmentEndData data) =>
            data.CornerOffset = value;

        public override void RefreshNode(NodeData data) =>
            MixedValues = !data.HasUniformCornerOffset();

        public override string TooltipPostfix => "m";

        public override void RefreshNodeValues(NodeData data) {
            isEnabled = data.CanModifyOffset();
            if (isEnabled) {
                value = data.CornerOffset;
            }
        }

        public override void RefreshSegmentEndValues(SegmentEndData data) {
            isEnabled = data.CanModifyOffset();
            if (isEnabled) {
                value = data.CornerOffset;
                bool mixed = !data.HasUniformCornerOffset();
                if (MixedValues != mixed) {
                    MixedValues = mixed;
                    Invalidate();
                }
            }
        }
    }
}
