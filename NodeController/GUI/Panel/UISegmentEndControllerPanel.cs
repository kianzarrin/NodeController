namespace NodeController.GUI {
    using ColossalFramework.UI;
    using KianCommons;

    public class UISegmentEndControllerPanel: UIPanelBase {
        #region Instanciation
        public static UISegmentEndControllerPanel Instance { get; private set; }

        public static UISegmentEndControllerPanel Create() {
            var uiView = UIView.GetAView();
            UISegmentEndControllerPanel panel = uiView.AddUIComponent(typeof(UISegmentEndControllerPanel)) as UISegmentEndControllerPanel;
            return panel;
        }

        public static void Release() {
            var uiView = UIView.GetAView();
            var panel = (UISegmentEndControllerPanel)uiView.FindUIComponent<UISegmentEndControllerPanel>("UISegmentEndControllerPanel");
            Destroy(panel);
        }
        #endregion Instanciation

        public ushort SegmentID { get; private set; }
        public ushort NodeID { get; private set; }
        public bool StartNode => NetUtil.IsStartNode(segmentId:SegmentID, nodeId: NodeID);

        public SegmentEndData SegmentEndData {
            get {
                if (NodeID == 0) return null;
                return SegmentEndManager.Instance.GetOrCreate(SegmentID, StartNode);
            }
        }

        public override NetworkTypeT NetworkType => NetworkTypeT.Node;

        public override void Awake() {
            base.Awake();
            Instance = this;
        }

        public override void Start() {
            base.Start();
            Log.Debug("UISegmentEndControllerPanel started");

            name = "UISegmentEndControllerPanel";
            Caption = "Segment End Controller";

            {
                var panel = AddPanel();

                var label = panel.AddUIComponent<UILabel>();
                label.text = "Corner smoothness";
                label.tooltip = "Adjusts Corner offset for smooth junction transition.";

                var slider_ = panel.AddUIComponent<UIOffsetSlider>();
                Controls.Add(slider_);
            }

            AddPanel().name = "Space";

            //{
            //    var panel = AddPanel();
            //    var checkBox = panel.AddUIComponent<UIHideMarkingsCheckbox>();
            //    Controls.Add(checkBox);
            //}

            {
                var panel = AddPanel();
                var button = panel.AddUIComponent<UIResetButton>();
                Controls.Add(button);
            }
        }

        public void ShowNode(ushort segmentID, ushort nodeID) {
            // TODO hide the other panel
            SegmentID = segmentID;
            NodeID = nodeID;
            SegmentEndManager.Instance.RefreshData(SegmentID, StartNode);
            Show();
            Refresh();
        }

        public void Close() {
            SegmentEndManager.Instance.RefreshData(SegmentID, StartNode);
            SegmentID = NodeID =  0;
            Hide();
        }

    }
}
