namespace NodeController.GUI {
    using ColossalFramework.UI;
    using KianCommons;

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

        public ushort NodeID { get; private set; }

        public NodeData NodeData {
            get {
                if (NodeID == 0) return null;
                return NodeManager.Instance.GetOrCreate(NodeID);
            }
        }

        public override NetworkTypeT NetworkType => NetworkTypeT.Node;

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

            //{
            //    var panel = AddPanel();

            //    var label = panel.AddUIComponent<UILabel>();
            //    label.text = "Corner smoothness";
            //    label.tooltip = "Adjusts Corner offset for smooth junction transition.";

            //    var slider_ = panel.AddUIComponent<UIOffsetSlider>();
            //    Controls.Add(slider_);
            //}

            AddPanel().name = "Space";

            //{
            //    var panel = AddPanel();
            //    var checkBox = panel.AddUIComponent<UIHideMarkingsCheckbox>();
            //    Controls.Add(checkBox);
            //}
            //{
            //    var panel = AddPanel();
            //    var checkBox = panel.AddUIComponent<UIFlatJunctionsCheckbox>();
            //    Controls.Add(checkBox);
            //}

            {
                var panel = AddPanel();
                var button = panel.AddUIComponent<UIResetButton>();
                Controls.Add(button);
            }
        }


        public void ShowNode(ushort nodeID) {
            // TODO hide the other panel
            NodeManager.Instance.RefreshData(NodeID);
            NodeID = nodeID;
            Show();
            Refresh();
        }

        public void Close() {
            NodeManager.Instance.RefreshData(NodeID);
            NodeID = 0;
            Hide();
        }
    }
}
