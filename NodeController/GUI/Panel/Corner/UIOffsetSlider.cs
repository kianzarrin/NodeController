namespace NodeController.GUI {
    using UnityEngine;
    using KianCommons;

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
            //Log.Debug($"UIOffsetSlider.RefreshNode() " +
            //    $"data.CanModifyOffset()={data.CanModifyOffset()} " +
            //    $"this.version:{this.VersionOf()} " +
            //    $"data.version={data.VersionOf()} " +
            //    $"NodeData.version={typeof(NodeData).VersionOf()}");
            isEnabled = data.CanModifyOffset();



            if (data.HasUniformCornerOffset()) {
                thumbObject.color = Color.white;
                thumbObject.opacity = 1;
            } else {
                thumbObject.color = Color.grey;
                thumbObject.opacity = 0.2f;
            }


        }

        public override void RefreshSegmentEnd(SegmentEndData data) {
            //Log.Debug("UIOffsetSlider.RefreshSegmentEnd() called. this.version=" + this.VersionOf());
            value = data.CornerOffset;
            isEnabled = data.CanModifyOffset();
        }
    }
}
