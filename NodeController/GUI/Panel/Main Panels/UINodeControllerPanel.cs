namespace NodeController.GUI {
    using ColossalFramework.UI;
    using KianCommons;

    using NodeController.Tool;

    public class UINodeControllerPanel : UIPanelBase {
        #region Instanciation
        public static UINodeControllerPanel Instance { get; private set; }

        public static UINodeControllerPanel Create() {
            var uiView = UIView.GetAView();
            UINodeControllerPanel panel = uiView.AddUIComponent(typeof(UINodeControllerPanel)) as UINodeControllerPanel;
            return panel;
        }

        public static void Release() {
            var uiView = UIView.GetAView();
            var panel = (UINodeControllerPanel)uiView.FindUIComponent<UINodeControllerPanel>("UINodeControllerPanel");
            Destroy(panel);
        }
        #endregion Instanciation

        public override NetworkTypeT NetworkType => NetworkTypeT.Node;
        public override INetworkData GetData() => NodeData;

        public ushort NodeID { get; private set; }

        public NodeData NodeData {
            get {
                if (NodeID == 0) return null;
                return NodeManager.Instance.GetOrCreate(NodeID);
            }
        }

        public override void Awake() {
            base.Awake();
            Instance = this;
        }

        public override void Start() {
            base.Start();
            Log.Debug("UINodeControllerPanel started");

            name = "UINodeControllerPanel";
            Caption = "Node Controller";

            {
                var panel = AddPanel();
                var label = panel.AddUIComponent<UILabel>();
                label.text = "Choose node type";

                var dropdown_ = panel.AddUIComponent<UINodeTypeDropDown>();
                Controls.Add(dropdown_);
            }

            {
                var panel = AddPanel();

                var label = panel.AddUIComponent<UILabel>();
                label.text = "Corner smoothness";
                label.tooltip = "Adjusts Corner offset for smooth junction transition.";

                var slider_ = panel.AddUIComponent<UIOffsetSlider>();
                Controls.Add(slider_);
            }

            {
                var panel = AddPanel();
                var label = panel.AddUIComponent<UILabel>();
                label.text = "Embankment";
                label.tooltip = "twist road sideways (superelevation)";
                var slider_ = panel.AddUIComponent<EmbankmentSlider>();
                Controls.Add(slider_);
            }

            {
                var panel = AddPanel();
                var label = panel.AddUIComponent<UILabel>();
                label.text = "Slope";
                label.tooltip = "+90=>up -90=>down\n";
                var slider_ = panel.AddUIComponent<SlopeSlider>();
                Controls.Add(slider_);
            }

            {
                var panel = AddPanel();
                var label = panel.AddUIComponent<UILabel>();
                label.text = "Stretch";
                label.tooltip = "-100%=>size nullified -90=>down\n";
                var slider_ = panel.AddUIComponent<StretchSlider>();
                Controls.Add(slider_);
            }

            AddPanel().name = "Space";

            {
                var panel = AddPanel();
                var checkBox = panel.AddUIComponent<UIHideMarkingsCheckbox>();
                Controls.Add(checkBox);
            }
            {
                var panel = AddPanel();
                ResetButton = panel.AddUIComponent<UIResetButton>();
                Controls.Add(ResetButton);
            }
            Disable();
        }


        public void ShowNode(ushort nodeID) {
            NodeManager.Instance.UpdateData(NodeID); // refresh previous node data if any.
            UISegmentEndControllerPanel.Instance.Close();
            NodeID = nodeID;
            NodeManager.Instance.UpdateData(NodeID);
            Enable();
            Show();
            Refresh();
        }

        public void Close() {
            NodeManager.Instance.UpdateData(NodeID);
            NodeID = 0;
            Hide();
        }
    }
}
