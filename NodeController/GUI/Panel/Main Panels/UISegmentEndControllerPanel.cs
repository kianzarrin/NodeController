namespace NodeController.GUI {
    using System.Linq;
    using UnityEngine;
    using ColossalFramework.UI;
    using KianCommons;
    using KianCommons.UI;
    using System;

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
        public static Vector2 CELL_SIZE2 = new Vector2(60, 20); // corner table

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
                offsetPanel_ = AddUIComponent<UIAutoSizePanel>();
                offsetPanel_.autoLayoutDirection = LayoutDirection.Horizontal;
                offsetPanel_.AutoSize2 = true;
                offsetPanel_.padding = new RectOffset(5, 5, 5, 5);
                offsetPanel_.autoLayoutPadding = new RectOffset(0, 4, 0, 0);
                {
                    var panel = offsetPanel_.AddUIComponent<UIAutoSizePanel>();
                    panel.AutoSize2 = false;
                    panel.width = 290; // set slider width
                    var label = panel.AddUIComponent<UILabel>();
                    label.text = "Corner smoothness";
                    label.tooltip = "Adjusts Corner offset for smooth junction transition.";
                    var slider_ = panel.AddUIComponent<UIOffsetSlider>();
                    Controls.Add(slider_);
                }
                {
                    var table = offsetPanel_.AddUIComponent<UIAutoSizePanel>();
                    table.name = "offset_table";
                    table.autoLayoutDirection = LayoutDirection.Vertical;
                    table.AutoSize2 = true;
                    var row1 = AddTableRow(table);
                    var row2 = AddTableRow(table);
                    row1.width = row2.width = CELL_SIZE2.x * 2;

                    AddTableLable(row1, "Left").size = CELL_SIZE2;
                    AddTableLable(row1, "Right").size = CELL_SIZE2;
                    
                    var lcorner = row2.AddUIComponent<UICornerTextField>();
                    lcorner.allowNegative = false;
                    lcorner.size = CELL_SIZE2;
                    lcorner.GetData = () => SegmentEndData.CorneroffsetRight;
                    lcorner.SetData = val => SegmentEndData.CorneroffsetRight = val > 0 ? val : 0;
                    Controls.Add(lcorner);

                    var rcorner = row2.AddUIComponent<UICornerTextField>();
                    rcorner.allowNegative = false;
                    rcorner.size = CELL_SIZE2;
                    rcorner.GetData = () => SegmentEndData.CornerOffsetLeft;
                    rcorner.SetData = val => SegmentEndData.CornerOffsetLeft = val > 0 ? val : 0;
                    Controls.Add(rcorner);

                    lcorner.Mirror = rcorner;
                    rcorner.Mirror = lcorner;
                }
            }

            {
                var panel = AddPanel();
                {
                    var panel0 = panel.AddUIComponent<UIAutoSizePanel>();
                    panel0.width = panel.width;
                    var label = panel0.AddUIComponent<UILabel>();
                    label.text = "Embankment";
                    label.tooltip = "twist road sideways (superelevation)";
                    var slider_ = panel0.AddUIComponent<EmbankmentSlider>();
                    Controls.Add(slider_);
                }
                {
                    var row2 = panel.AddUIComponent<UIAutoSizePanel>();
                    row2.autoSize = row2.AutoSize2 = true;
                    row2.autoLayoutDirection = LayoutDirection.Horizontal;
                    var col1 = row2.AddUIComponent<UIAutoSizePanel>();
                    col1.AutoSize2 = true;
                    var col2 = row2.AddUIComponent<UIAutoSizePanel>();
                    col2.AutoSize2 = true;
                    var col3 = row2.AddUIComponent<UIAutoSizePanel>();
                    col3.AutoSize2 = true;
                    var col4 = row2.AddUIComponent<UIAutoSizePanel>();
                    col4.AutoSize2 = true;

                    var fieldAngle = col1.AddUIComponent<UICornerTextField>();
                    fieldAngle.GetData = () => SegmentEndData.EmbankmentAngleDeg;
                    fieldAngle.SetData = val => SegmentEndData.EmbankmentAngleDeg = Mathf.Clamp(val, -180, +180);
                    Controls.Add(fieldAngle);

                    var lbl1 = col2.AddUIComponent<UILabel>();
                    lbl1.autoSize = true;
                    lbl1.text = "degrees  ";
                    lbl1.padding = new RectOffset(0, 0, 4, 0);


                    var fieldPercent = col3.AddUIComponent<UICornerTextField>();
                    fieldPercent.GetData = () => SegmentEndData.EmbankmentPercent;
                    fieldPercent.SetData = val => SegmentEndData.EmbankmentPercent = val;
                    Controls.Add(fieldPercent);

                    var lbl2 = col4.AddUIComponent<UILabel>();
                    lbl2.autoSize = true;
                    lbl2.text = "%";
                    lbl2.padding = new RectOffset(0, 0, 4, 0);
                }
            }

            {
                var panel = AddPanel();
                {
                    var panel0 = panel.AddUIComponent<UIAutoSizePanel>();
                    panel0.width = panel.width;
                    var label = panel0.AddUIComponent<UILabel>();
                    label.text = "Slope";
                    label.tooltip = "+90=>up -90=>down\n";
                    var slider_ = panel0.AddUIComponent<SlopeSlider>();
                    Controls.Add(slider_);
                }
                {
                    var row2 = panel.AddUIComponent<UIAutoSizePanel>();
                    row2.autoSize = row2.AutoSize2 = true;
                    row2.autoLayoutDirection = LayoutDirection.Horizontal;
                    var col1 = row2.AddUIComponent<UIAutoSizePanel>();
                    col1.AutoSize2 = true;
                    var col2 = row2.AddUIComponent<UIAutoSizePanel>();
                    col2.AutoSize2 = true;

                    var fieldAngle = col1.AddUIComponent<UICornerTextField>();
                    fieldAngle.GetData = () => SegmentEndData.SlopeAngleDeg;
                    fieldAngle.SetData = val => SegmentEndData.SlopeAngleDeg = Mathf.Clamp(val, -180, +180);
                    Controls.Add(fieldAngle);

                    var lbl1 = col2.AddUIComponent<UILabel>();
                    lbl1.autoSize = true;
                    lbl1.text = "degrees";
                    lbl1.padding = new RectOffset(0, 0, 4, 0);
                }
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



        UIAutoSizePanel offsetPanel_;
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
                AddTableLable(row1, "axis:", center: false);
                AddTableLable(row1, "outward");
                AddTableLable(row1, "backward");
                AddTableLable(row1, "vertical");

                var row2 = AddTableRow(table);
                AddTableLable(row2, "pos:", center: false);

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
                AddTableLable(row3, "dir:", center: false);

                ldirx = row3.AddUIComponent<UICornerTextField>();
                ldirx.GetData = () => SegmentEndData.RightCornerDir.x;
                ldirx.SetData = val => SegmentEndData.SetRightCornerDirI(val, 0);
                ldirx.MouseWheelRatio = 0.1f;
                Controls.Add(ldirx);

                ldirz = row3.AddUIComponent<UICornerTextField>();
                ldirz.GetData = () => SegmentEndData.RightCornerDir.z;
                ldirz.SetData = val => SegmentEndData.SetRightCornerDirI(val, 2);
                ldirz.MouseWheelRatio = 0.1f;
                Controls.Add(ldirz);

                ldiry = row3.AddUIComponent<UICornerTextField>();
                ldiry.GetData = () => SegmentEndData.RightCornerDir.y;
                ldiry.SetData = val => SegmentEndData.SetRightCornerDirI(val, 1);
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
                AddTableLable(row1, "axis:", center: false);
                AddTableLable(row1, "outward");
                AddTableLable(row1, "backward");
                AddTableLable(row1, "vertical");

                var row2 = AddTableRow(table);
                AddTableLable(row2, "pos:", center: false);

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
                AddTableLable(row3, "dir:", center: false);

                rdirx = row3.AddUIComponent<UICornerTextField>();
                rdirx.GetData = () => SegmentEndData.LeftCornerDir.x;
                rdirx.SetData = val => SegmentEndData.SetLeftCornerDirI(val, 0);
                rdirx.MouseWheelRatio = 0.1f;
                Controls.Add(rdirx);

                rdirz = row3.AddUIComponent<UICornerTextField>();
                rdirz.GetData = () => SegmentEndData.LeftCornerDir.z;
                rdirz.SetData = val => SegmentEndData.SetLeftCornerDirI(val, 2);
                rdirz.MouseWheelRatio = 0.1f;
                Controls.Add(rdirz);

                rdiry = row3.AddUIComponent<UICornerTextField>();
                rdiry.GetData = () => SegmentEndData.LeftCornerDir.y;
                rdiry.SetData = val => SegmentEndData.SetLeftCornerDirI(val, 1);
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

        static public UIPanel AddTableRow(UIPanel container, int nColumns=4) {
            var panel = container.AddUIComponent<UIPanel>();
            panel.autoLayout = true;
            panel.autoLayoutDirection = LayoutDirection.Horizontal;
            panel.size = new Vector2(CELL_SIZE.x * nColumns, CELL_SIZE.y);
            return panel;
        }

        static public UILabel AddTableLable(UIPanel container, string text, bool center=true) {
            var lbl = container.AddUIComponent<UILabel>();
            lbl.text = text;
            if(center)
                lbl.textAlignment = UIHorizontalAlignment.Center;
            lbl.autoSize = false;
            lbl.size = CELL_SIZE;
            return lbl;
        }

        public void ShowSegmentEnd(ushort segmentID, ushort nodeID) {
            if (Instance != this) Log.Error("Assertion Failed: Instance == this");
            SegmentEndManager.Instance.UpdateData(SegmentID, StartNode); // update previous segment data if any.
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
            Log.Debug("SegmentEndController.Refresh() called\n" + Environment.StackTrace);
            tableLeft_.isVisible = tableRight_.isVisible =
                SegmentEndData?.CanModifyCorners() ?? false;
            offsetPanel_.isVisible = SegmentEndData?.CanModifyOffset() ?? false;
            base.Refresh();
        }
    }
}
 