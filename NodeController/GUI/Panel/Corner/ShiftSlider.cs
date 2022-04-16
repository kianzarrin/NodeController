namespace NodeController.GUI {
    using System;
    using ColossalFramework.UI;
    using KianCommons;
    using static KianCommons.HelpersExtensions;

    public class ShiftSlider : UISliderBase {
        public override void Start() {
            base.Start();
            minValue = -20;
            maxValue = +20;
            LargeScrollStep = 1;
            LargeDragStep = 0.5f;
            CourseScrollStep = 0.1f;
            CourseDragStep = 0.1f;
        }

        public override void ApplyNode(NodeData data) 
            => data.Shift = value;

        public override void ApplySegmentEnd(SegmentEndData data)
            => data.Shift = value;

        public override string TooltipPostfix => "m";


        public override void RefreshNode(NodeData data) =>
            MixedValues = !data.HasUniformShift();

        public override void RefreshNodeValues(NodeData data) {
            isEnabled = data.CanMassEditNodeCorners();
            if (isEnabled)
                value = data.Shift;
        }

        public override void RefreshSegmentEndValues(SegmentEndData data) {
            isEnabled = data.CanModifyCorners();
            if (isEnabled)
                value = data.Shift;
        }
    }
}
