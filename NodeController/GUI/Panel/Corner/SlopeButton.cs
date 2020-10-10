namespace NodeController.GUI {
    public class SlopeButton : ButtonBase{
        public new NodeData Data => base.Data as NodeData;
        public override string HintHotkeys => null;

        public override bool ShouldShow => Data?.CanModifyFlatJunctions() ?? false;

        public override void Action(INetworkData data) => Data.UnFlatten();

        public override void Awake() {
            base.Awake();
            text = "Make Sloped";
        }

        public override string HintDescription {
            get {
                if (Data.SegmentCount <= 2)
                    return "make node follow road's slope";
                return "make node follow main road's slope and twist side segments to match the slope";
            }
        }
    }
}


