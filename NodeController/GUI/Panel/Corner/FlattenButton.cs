namespace NodeController.GUI {
    public class FlattenButton : ButtonBase {
        public new NodeData Data => base.Data as NodeData;
        public override string HintHotkeys => null;
        public override bool ShouldShow => Data?.CanModifyFlatJunctions() ?? false;
        public override void Action(INetworkData data) => Data.Flatten();

        public override void Awake() {
            base.Awake();
            text = "Make flat";
        }

        public override string HintDescription => "flatten the intersection";
    }
}


