namespace NodeController.GUI {
    using ColossalFramework.UI;
    using KianCommons;
    using KianCommons.UI;
    using UnityEngine;

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
        static Vector2 CELL_SIZE = new Vector2(50, 20);

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

            MakeCornerTable(this);

            AddPanel().name = "Space";

            {
                var panel = AddPanel();
                var checkBox = panel.AddUIComponent<UIHideMarkingsCheckbox>();
                Controls.Add(checkBox);
            }


            {
                var panel = AddPanel();
                var button = panel.AddUIComponent<UIResetButton>();
                Controls.Add(button);
            }
        }

        public void MakeCornerTable(UIPanel container) {
            {
                var label = container.AddUIComponent<UILabel>();
                label.text = "Right corner";

                UIAutoSizePanel table = AddUIComponent<UIAutoSizePanel>();
                table.name = "table_right";
                table.autoLayoutDirection = LayoutDirection.Vertical;
                table.AutoSize2 = true;

                // header :  axis: outward, vertical, backward
                var row1 = AddTableRow(table);
                AddTableLable(row1, "axis:");
                AddTableLable(row1, "outward");
                AddTableLable(row1, "vertical");
                AddTableLable(row1, "backward");

                var row2 = AddTableRow(table);
                AddTableLable(row2, "pos:");

                var posx = row2.AddUIComponent<UICornerTextField>();
                posx.GetData = () => SegmentEndData.RightCornerPos.x;
                posx.SetData = val => SegmentEndData.RightCornerPos.x = val;
                Controls.Add(posx);

                var posy = row2.AddUIComponent<UICornerTextField>();
                posy.GetData = () => SegmentEndData.RightCornerPos.y;
                posy.SetData = val => SegmentEndData.RightCornerPos.y = val;
                Controls.Add(posy);

                var posz = row2.AddUIComponent<UICornerTextField>();
                posz.GetData = () => SegmentEndData.RightCornerPos.z;
                posz.SetData = val => SegmentEndData.RightCornerPos.z = val;
                Controls.Add(posz);

                var row3 = AddTableRow(table);
                AddTableLable(row3, "dir:");
                var dirx = row3.AddUIComponent<UICornerTextField>();
                dirx.GetData = () => SegmentEndData.RightCornerDir.x;
                dirx.SetData = val => SegmentEndData.RightCornerDir.x = val;
                Controls.Add(dirx);

                var diry = row3.AddUIComponent<UICornerTextField>();
                diry.GetData = () => SegmentEndData.RightCornerDir.y;
                diry.SetData = val => SegmentEndData.RightCornerDir.y = val;
                Controls.Add(diry);

                var dirz = row3.AddUIComponent<UICornerTextField>();
                dirz.GetData = () => SegmentEndData.RightCornerDir.z;
                dirz.SetData = val => SegmentEndData.RightCornerDir.z = val;
                Controls.Add(dirz);
            }

            {
                var label = container.AddUIComponent<UILabel>();
                label.text = "Left corner";

                UIAutoSizePanel table = AddUIComponent<UIAutoSizePanel>();
                table.name = "table_left";
                table.autoLayoutDirection = LayoutDirection.Vertical;
                table.AutoSize2 = true;

                // header :  axis: outward, vertical, backward
                var row1 = AddTableRow(table);
                AddTableLable(row1, "axis:");
                AddTableLable(row1, "outward");
                AddTableLable(row1, "vertical");
                AddTableLable(row1, "backward");

                var row2 = AddTableRow(table);
                AddTableLable(row2, "pos:");

                var posx = row2.AddUIComponent<UICornerTextField>();
                posx.GetData = () => SegmentEndData.LeftCornerPos.x;
                posx.SetData = val => SegmentEndData.LeftCornerPos.x = val;
                Controls.Add(posx);

                var posy = row2.AddUIComponent<UICornerTextField>();
                posy.GetData = () => SegmentEndData.LeftCornerPos.y;
                posy.SetData = val => SegmentEndData.LeftCornerPos.y = val;
                Controls.Add(posy);

                var posz = row2.AddUIComponent<UICornerTextField>();
                posz.GetData = () => SegmentEndData.LeftCornerPos.z;
                posz.SetData = val => SegmentEndData.LeftCornerPos.z = val;
                Controls.Add(posz);

                var row3 = AddTableRow(table);
                AddTableLable(row3, "dir:");
                var dirx = row3.AddUIComponent<UICornerTextField>();
                dirx.GetData = () => SegmentEndData.LeftCornerDir.x;
                dirx.SetData = val => SegmentEndData.LeftCornerDir.x = val;
                Controls.Add(dirx);

                var diry = row3.AddUIComponent<UICornerTextField>();
                diry.GetData = () => SegmentEndData.LeftCornerDir.y;
                diry.SetData = val => SegmentEndData.LeftCornerDir.y = val;
                Controls.Add(diry);

                var dirz = row3.AddUIComponent<UICornerTextField>();
                dirz.GetData = () => SegmentEndData.LeftCornerDir.z;
                dirz.SetData = val => SegmentEndData.LeftCornerDir.z = val;
                Controls.Add(dirz);
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

    }
}
