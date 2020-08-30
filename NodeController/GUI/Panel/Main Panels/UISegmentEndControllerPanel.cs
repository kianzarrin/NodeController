namespace NodeController.GUI {
    using System.Linq;
    using UnityEngine;
    using ColossalFramework.UI;
    using KianCommons;
    using KianCommons.UI;

    public class UISegmentEndControllerPanel : UIPanelBase {
        #region Instanciation
        public static UISegmentEndControllerPanel Instance { get; private set; }

        public static UISegmentEndControllerPanel Create() {
            var uiView = UIView.GetAView();
            UISegmentEndControllerPanel panel = uiView.AddUIComponent(typeof(UISegmentEndControllerPanel)) as UISegmentEndControllerPanel;
            return panel;
        }

        public static void Release() {
            var uiView = UIView.GetAView();
            var panel = uiView.FindUIComponent<UISegmentEndControllerPanel>("UISegmentEndControllerPanel");
            Destroy(panel);
        }
        #endregion Instanciation

        public ushort SegmentID { get; private set; }
        public ushort NodeID { get; private set; }
        public bool StartNode => NetUtil.IsStartNode(segmentId: SegmentID, nodeId: NodeID);
        public static Vector2 CELL_SIZE = new Vector2(100, 20);

        public SegmentEndData SegmentEndData {
            get {
                if (NodeID == 0) return null;
                NodeManager.Instance.GetOrCreate(NodeID);
                return SegmentEndManager.Instance.GetOrCreate(SegmentID, StartNode);
            }
        }

        public override NetworkTypeT NetworkType => NetworkTypeT.Node;
        public override INetworkData GetData() => SegmentEndData;

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

            {
                var panel = AddPanel();
                var checkBox = panel.AddUIComponent<UIFlatJunctionsCheckbox>();
                var padding = panel.autoLayoutPadding;
                padding.bottom = 0;
                panel.autoLayoutPadding = padding;
                Controls.Add(checkBox);

                var panel2 = panel.AddUIComponent<UIAutoSizePanel>();
                panel2.padding = new RectOffset(50, 5, 0, 5);
                var checkBox2 = panel2.AddUIComponent<TwistCheckbox>();
                Controls.Add(checkBox2);

            }

            {
                var panel = AddPanel();
                panel.padding = new RectOffset(10, 10, 5, 5);
                panel.AutoSize2 = true;
                MakeCornerTable(panel);
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

            MakeHintBox();

            AutoSize2 = true;
            Disable();
        }




        UIAutoSizePanel tableLeft_, tableRight_;
        public void MakeCornerTable(UIPanel container) {
            UICornerTextField lposx, lposy, lposz,
                              rposx, rposy, rposz,
                              ldirx, ldiry, ldirz,
                              rdirx, rdiry, rdirz;

            {
                UIAutoSizePanel table = tableLeft_ = container.AddUIComponent<UIAutoSizePanel>();
                table.name = "table_left";
                table.autoLayoutDirection = LayoutDirection.Vertical;
                table.AutoSize2 = true;

                var label = table.AddUIComponent<UILabel>();
                label.text = "Left corner";

                // header :  axis: outward, vertical, backward
                var row1 = AddTableRow(table);
                AddTableLable(row1, "axis:");
                AddTableLable(row1, "outward");
                AddTableLable(row1, "backward");
                AddTableLable(row1, "vertical");

                var row2 = AddTableRow(table);
                AddTableLable(row2, "pos:");

                lposx = row2.AddUIComponent<UICornerTextField>();
                lposx.GetData = () => SegmentEndData.DeltaRightCornerPos.x;
                lposx.SetData = val => SegmentEndData.DeltaRightCornerPos.x = val;
                Controls.Add(lposx);

                lposz = row2.AddUIComponent<UICornerTextField>();
                lposz.GetData = () => SegmentEndData.DeltaRightCornerPos.z;
                lposz.SetData = val => SegmentEndData.DeltaRightCornerPos.z = val;
                Controls.Add(lposz);

                lposy = row2.AddUIComponent<UICornerTextField>();
                lposy.GetData = () => SegmentEndData.DeltaRightCornerPos.y;
                lposy.SetData = val => SegmentEndData.DeltaRightCornerPos.y = val;
                Controls.Add(lposy);

                var row3 = AddTableRow(table);
                AddTableLable(row3, "dir:");

                ldirx = row3.AddUIComponent<UICornerTextField>();
                ldirx.GetData = () => SegmentEndData.DeltaRightCornerDir.x;
                ldirx.SetData = val => SegmentEndData.DeltaRightCornerDir.x = val;
                ldirx.MouseWheelRatio = 0.1f;
                Controls.Add(ldirx);

                ldirz = row3.AddUIComponent<UICornerTextField>();
                ldirz.GetData = () => SegmentEndData.DeltaRightCornerDir.z;
                ldirz.SetData = val => SegmentEndData.DeltaRightCornerDir.z = val;
                ldirz.MouseWheelRatio = 0.1f;
                Controls.Add(ldirz);

                ldiry = row3.AddUIComponent<UICornerTextField>();
                ldiry.GetData = () => SegmentEndData.DeltaRightCornerDir.y;
                ldiry.SetData = val => SegmentEndData.DeltaRightCornerDir.y = val;
                ldiry.MouseWheelRatio = 0.1f;
                Controls.Add(ldiry);
            }

            {
                UIAutoSizePanel table = tableRight_ = container.AddUIComponent<UIAutoSizePanel>();
                table.name = "table_right";
                table.autoLayoutDirection = LayoutDirection.Vertical;
                table.AutoSize2 = true;

                var label = table.AddUIComponent<UILabel>();
                label.text = "Right corner";

                // header :  axis: outward, vertical, backward
                var row1 = AddTableRow(table);
                AddTableLable(row1, "axis:");
                AddTableLable(row1, "outward");
                AddTableLable(row1, "backward");
                AddTableLable(row1, "vertical");

                var row2 = AddTableRow(table);
                AddTableLable(row2, "pos:");

                rposx = row2.AddUIComponent<UICornerTextField>();
                rposx.GetData = () => SegmentEndData.DeltaLeftCornerPos.x;
                rposx.SetData = val => SegmentEndData.DeltaLeftCornerPos.x = val;
                Controls.Add(rposx);

                rposz = row2.AddUIComponent<UICornerTextField>();
                rposz.GetData = () => SegmentEndData.DeltaLeftCornerPos.z;
                rposz.SetData = val => SegmentEndData.DeltaLeftCornerPos.z = val;
                Controls.Add(rposz);

                rposy = row2.AddUIComponent<UICornerTextField>();
                rposy.GetData = () => SegmentEndData.DeltaLeftCornerPos.y;
                rposy.SetData = val => SegmentEndData.DeltaLeftCornerPos.y = val;
                Controls.Add(rposy);

                var row3 = AddTableRow(table);
                AddTableLable(row3, "dir:");

                rdirx = row3.AddUIComponent<UICornerTextField>();
                rdirx.GetData = () => SegmentEndData.DeltaLeftCornerDir.x;
                rdirx.SetData = val => SegmentEndData.DeltaLeftCornerDir.x = val;
                rdirx.MouseWheelRatio = 0.1f;
                Controls.Add(rdirx);

                rdirz = row3.AddUIComponent<UICornerTextField>();
                rdirz.GetData = () => SegmentEndData.DeltaLeftCornerDir.z;
                rdirz.SetData = val => SegmentEndData.DeltaLeftCornerDir.z = val;
                rdirz.MouseWheelRatio = 0.1f;
                Controls.Add(rdirz);

                rdiry = row3.AddUIComponent<UICornerTextField>();
                rdiry.GetData = () => SegmentEndData.DeltaLeftCornerDir.y;
                rdiry.SetData = val => SegmentEndData.DeltaLeftCornerDir.y = val;
                rdiry.MouseWheelRatio = 0.1f;
                Controls.Add(rdiry);
            }

            lposx.Mirror = rposx; rposx.Mirror = lposx;
            ldirx.Mirror = rdirx; rdirx.Mirror = ldirx;
            lposz.Mirror = rposz; rposz.Mirror = lposz;
            ldirz.Mirror = rdirz; rdirz.Mirror = ldirz;
            lposy.Mirror = rposy; rposy.Mirror = lposy;
            ldiry.Mirror = rdiry; rdiry.Mirror = ldiry;
        }

        static public UIPanel AddTableRow(UIPanel container) {
            var panel = container.AddUIComponent<UIPanel>();
            panel.autoLayout = true;
            panel.autoLayoutDirection = LayoutDirection.Horizontal;
            panel.size = new Vector2(CELL_SIZE.x * 4, CELL_SIZE.y);
            return panel;
        }

        static public UILabel AddTableLable(UIPanel container, string text) {
            var lbl = container.AddUIComponent<UILabel>();
            lbl.text = text;
            lbl.autoSize = false;
            lbl.size = CELL_SIZE;
            return lbl;
        }

        public void ShowSegmentEnd(ushort segmentID, ushort nodeID) {
            if (Instance != this) Log.Error("Assertion Failed: Instance == this");
            SegmentEndManager.Instance.UpdateData(SegmentID, StartNode); // refresh previous segment data if any.
            SegmentID = segmentID;
            NodeID = nodeID;
            SegmentEndManager.Instance.UpdateData(SegmentID, StartNode);
            base.Open();
        }

        public override void Close() {
            SegmentEndManager.Instance.UpdateData(SegmentID, StartNode);
            SegmentID = NodeID = 0;
            base.Close();
        }

        public override void Refresh() {
            tableLeft_.isVisible = tableRight_.isVisible =
                SegmentEndData?.CanModifyCorners() ?? false;
            base.Refresh();
            Hintbox.Refresh();
        }
    }
}
 