namespace NodeController.GUI {
    using ColossalFramework.UI;
    using System.Globalization;
    using UnityEngine;
    using static KianCommons.HelpersExtensions;
    using KianCommons;
    using KianCommons.UI;
    using System;

    public class UICornerTextField : UITextField, IDataControllerUI {
        UIResetButton resetButton_;
        UIPanel root_;
        bool started_ = false;
        bool refreshing_ = false;
        NumberStyles numberStyle_ = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign;

        public delegate float GetDataFunc();
        public delegate void SetDataFunc(float data);
        public GetDataFunc GetData;
        public SetDataFunc SetData;
        public UIComponent Container; // that will be set visible or invisible.

        public override void Awake() {
            base.Awake();
            atlas = TextureUtil.GetAtlas("Ingame");
            size = new Vector2(50, 20); // overwrite this to fit desired size.
            padding = new RectOffset(4, 4, 3, 3);
            builtinKeyNavigation = true;
            isInteractive = true;
            readOnly = false;
            horizontalAlignment = UIHorizontalAlignment.Center;
            selectionSprite = "EmptySprite";
            selectionBackgroundColor = new Color32(0, 172, 234, 255);
            normalBgSprite = "TextFieldPanelHovered";
            disabledBgSprite = "TextFieldPanelHovered";
            textColor = new Color32(0, 0, 0, 255);
            disabledTextColor = new Color32(80, 80, 80, 128);
            color = new Color32(255, 255, 255, 255);
            textScale = 0.9f;
            useDropShadow = true;
            text = "0";
        }

        public override void Start() {
            base.Start();
            Container = Container ?? parent;
            root_ = GetRootContainer() as UIPanel;
            resetButton_ = root_.GetComponentInChildren<UIResetButton>();
            started_ = true;
        }

        protected override void OnSizeChanged() {
            base.OnSizeChanged();
            // change width to match parent?
        }

        protected override void OnMouseWheel(UIMouseEventParameter p) {
            base.OnMouseWheel(p);
            Value += p.wheelDelta;
        }

        public bool TryGetValue(out float value) {
            if (text == "") {
                value = 0;
                return true;
            }

            return float.TryParse(text, numberStyle_, CultureInfo.InvariantCulture.NumberFormat, out value);
        }

        public float Value {
            set => text = value.ToString();
            get => float.Parse(text, CultureInfo.InvariantCulture.NumberFormat);
        }

        private string _prevText = "";

        protected override void OnTextChanged() {
            Log.Debug($"UICornerTextField.OnTextChanged() called");
            base.OnTextChanged();
            if (TryGetValue(out _)) {
                _prevText = text;
                Apply();
            } else {
                text = _prevText;
                Unfocus();
            }
        }

        //protected override void OnVisibilityChanged() {
        //    base.OnVisibilityChanged();
        //    if (isVisible)
        //        Refresh();
        //}

        public void Apply() {
            if (refreshing_ || !started_) return;
            Log.Debug($"UICornerTextField.Apply() called");
            if (TryGetValue(out float value)) {
                Log.Debug($"UICornerTextField.Apply : calling SetData()  ");
                SetData(value);
            } else {
                // Value = GetData(); 
                throw new Exception("Unreachable code. this path is handled in OnTextChanged().");
            }

            Refresh();
        }

        public void Refresh() {
            Log.Debug($"UICornerTextField.Refresh()called");
            refreshing_ = true;
            resetButton_?.Refresh();

            if (root_ is UINodeControllerPanel ncpanel)
                RefreshNode();
            else if (root_ is UISegmentEndControllerPanel secpanel)
                RefreshSegmentEnd();

            Value = GetData();
            Container.isVisible = isEnabled;
            Container.Invalidate();
            Invalidate();
            refreshing_ = false;
        }

        public void RefreshNode() {
            throw new NotImplementedException();
        }

        public void RefreshSegmentEnd() {
            SegmentEndData data = (root_ as UISegmentEndControllerPanel).SegmentEndData;
            if (data == null) {
                Disable();
                return;
            }
            data.Refresh();
            isEnabled = data.CanModifyOffset();
        }

    }
}
