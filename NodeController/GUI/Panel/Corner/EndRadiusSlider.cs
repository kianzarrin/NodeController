namespace NodeController.GUI; 
public class EndRadiusSlider : UISliderBase {
    public override void Start() {
        base.Start();
        minValue = 0;
        maxValue = +200;
    }

    public override void ApplyNode(NodeData data) 
        => data.DeltaEndRadius = value - 100;

    public override void ApplySegmentEnd(SegmentEndData data)
        => data.DeltaEndRadius = value - 100;


    public override void RefreshNode(NodeData data) =>
        MixedValues = !data.HasUniformDeltaEndRadius();

    public override void RefreshNodeValues(NodeData data) {
        isEnabled = data.CanModifyOffset();
        if (isEnabled)
            value = data.DeltaEndRadius + 100;
    }

    public override void RefreshSegmentEndValues(SegmentEndData data) {
        isEnabled = data.CanModifyCorners();
        if (isEnabled)
            value = data.DeltaEndRadius + 100;
    }
}
