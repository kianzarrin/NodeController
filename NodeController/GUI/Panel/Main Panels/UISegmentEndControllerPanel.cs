namespace NodeController.GUI {
    using ColossalFramework.UI;
    using KianCommons;
    using KianCommons.UI;
    using System.Linq;
    using UnityEngine;

    public class UISegmentEndControllerPanel : UIPanelBase {
        UIResetButton reset_;
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
                var checkBox = panel.AddUIComponent<UIFlatJunctionsCheckbox>();
                Controls.Add(checkBox);
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
                reset_ = panel.AddUIComponent<UIResetButton>();
                Controls.Add(reset_);
            }

            AutoSize2 = true;
        }

        UIAutoSizePanel tableLeft_, tableRight_;
        public void MakeCornerTable(UIPanel container) {
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

                var posx = row2.AddUIComponent<UICornerTextField>();
                posx.GetData = () => SegmentEndData.DeltaRightCornerPos.x;
                posx.SetData = val => SegmentEndData.DeltaRightCornerPos.x = val;
                Controls.Add(posx);

                var posz = row2.AddUIComponent<UICornerTextField>();
                posz.GetData = () => SegmentEndData.DeltaRightCornerPos.z;
                posz.SetData = val => SegmentEndData.DeltaRightCornerPos.z = val;
                Controls.Add(posz);

                var posy = row2.AddUIComponent<UICornerTextField>();
                posy.GetData = () => SegmentEndData.DeltaRightCornerPos.y;
                posy.SetData = val => SegmentEndData.DeltaRightCornerPos.y = val;
                Controls.Add(posy);

                var row3 = AddTableRow(table);
                AddTableLable(row3, "dir:");
                var dirx = row3.AddUIComponent<UICornerTextField>();
                dirx.GetData = () => SegmentEndData.DeltaRightCornerDir.x;
                dirx.SetData = val => SegmentEndData.DeltaRightCornerDir.x = val;
                dirx.MouseWheelRatio = 0.1f;
                Controls.Add(dirx);

                var dirz = row3.AddUIComponent<UICornerTextField>();
                dirz.GetData = () => SegmentEndData.DeltaRightCornerDir.z;
                dirz.SetData = val => SegmentEndData.DeltaRightCornerDir.z = val;
                dirz.MouseWheelRatio = 0.1f;
                Controls.Add(dirz);

                var diry = row3.AddUIComponent<UICornerTextField>();
                diry.GetData = () => SegmentEndData.DeltaRightCornerDir.y;
                diry.SetData = val => SegmentEndData.DeltaRightCornerDir.y = val;
                diry.MouseWheelRatio = 0.1f;
                Controls.Add(diry);
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

                var posx = row2.AddUIComponent<UICornerTextField>();
                posx.GetData = () => SegmentEndData.DeltaLeftCornerPos.x;
                posx.SetData = val => SegmentEndData.DeltaLeftCornerPos.x = val;
                Controls.Add(posx);

                var posz = row2.AddUIComponent<UICornerTextField>();
                posz.GetData = () => SegmentEndData.DeltaLeftCornerPos.z;
                posz.SetData = val => SegmentEndData.DeltaLeftCornerPos.z = val;
                Controls.Add(posz);

                var posy = row2.AddUIComponent<UICornerTextField>();
                posy.GetData = () => SegmentEndData.DeltaLeftCornerPos.y;
                posy.SetData = val => SegmentEndData.DeltaLeftCornerPos.y = val;
                Controls.Add(posy);

                var row3 = AddTableRow(table);
                AddTableLable(row3, "dir:");
                var dirx = row3.AddUIComponent<UICornerTextField>();
                dirx.GetData = () => SegmentEndData.DeltaLeftCornerDir.x;
                dirx.SetData = val => SegmentEndData.DeltaLeftCornerDir.x = val;
                dirx.MouseWheelRatio = 0.1f;
                Controls.Add(dirx);

                var dirz = row3.AddUIComponent<UICornerTextField>();
                dirz.GetData = () => SegmentEndData.DeltaLeftCornerDir.z;
                dirz.SetData = val => SegmentEndData.DeltaLeftCornerDir.z = val;
                dirz.MouseWheelRatio = 0.1f;
                Controls.Add(dirz);

                var diry = row3.AddUIComponent<UICornerTextField>();
                diry.GetData = () => SegmentEndData.DeltaLeftCornerDir.y;
                diry.SetData = val => SegmentEndData.DeltaLeftCornerDir.y = val;
                diry.MouseWheelRatio = 0.1f;
                Controls.Add(diry);
            }
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
            if (Instance != this)
                Log.Error("Assertion Failed: Instance == this");
            UINodeControllerPanel.Instance.Close();
            SegmentID = segmentID;
            NodeID = nodeID;
            SegmentEndManager.Instance.RefreshData(SegmentID, StartNode);
            Show();
            Refresh();
        }

        public void Close() {
            SegmentEndManager.Instance.RefreshData(SegmentID, StartNode);
            SegmentID = NodeID = 0;
            Hide();
        }

        public void RefreshTableValuesOnly() {
            foreach (IDataControllerUI control in Controls ?? Enumerable.Empty<IDataControllerUI>()) {
                if (control is UICornerTextField cornerTextField)
                    cornerTextField?.RefreshUIValueOnly();
            }
            reset_?.Refresh();
        }

        public override void Refresh() {
            tableLeft_.isVisible = tableRight_.isVisible =
                SegmentEndData?.CanModifyCorners() ?? false;
            base.Refresh();
        }
    }
}
