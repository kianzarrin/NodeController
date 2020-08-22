namespace NodeController.GUI {
    using UnityEngine;
    using KianCommons;
    using System;

    public class EmbankmentSlider : UISliderBase {
        public override void Start() {
            base.Start();
            maxValue = -180;
            minValue = 180;
            stepSize = 0.1f;
        }


        public override void ApplyNode(NodeData data) => throw new NotImplementedException();

        public override void ApplySegmentEnd(SegmentEndData data) => data.EmbankmentAngle = value;

        public override void RefreshNode(NodeData data) {
            //value = data.CornerOffset;

            //isEnabled = data.CanModifyCorners();

            //if (data.HasUniformCornerOffset()) {
            //    thumbObject.color = Color.white;
            //    thumbObject.opacity = 1;
            //} else {
            //    thumbObject.color = Color.grey;
            //    thumbObject.opacity = 0.2f;
            //}


        }

        public override void RefreshSegmentEnd(SegmentEndData data) {
            //Log.Debug("UIOffsetSlider.RefreshSegmentEnd() called. this.version=" + this.VersionOf());
            value = data.EmbankmentAngle;
            isEnabled = data.CanModifyCorners();
        }
    }
}
