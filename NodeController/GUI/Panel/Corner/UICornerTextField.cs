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
        UIPanelBase root_;
        bool started_ = false;
        bool refreshing_ = false;

        public delegate float GetDataFunc();
        public delegate void SetDataFunc(float data);
        public GetDataFunc GetData;
        public SetDataFunc SetData;
        public UIComponent Container; // that will be set visible or invisible.

        public override void Awake() {
            base.Awake();
            atlas = TextureUtil.GetAtlas("Ingame");
            size = UISegmentEndControllerPanel.CELL_SIZE;
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
            tooltip = "mousewheal => increment.\n" + "shift + mousewheal => large increment.";

            submitOnFocusLost = true;
            selectOnFocus = true;
            numericalOnly = true;
            allowFloats = true;
            allowNegative = true;
        }

        public override void Start() {
            base.Start();
            Container = Container ?? parent;
            root_ = GetRootContainer() as UIPanelBase;
            resetButton_ = root_.GetComponentInChildren<UIResetButton>();
            started_ = true;
        }

        protected override void OnSizeChanged() {
            base.OnSizeChanged();
            // change width to match parent?
        }

        protected override void OnMouseWheel(UIMouseEventParameter p) {
            base.OnMouseWheel(p);
            float ratio = HelpersExtensions.ShiftIsPressed ? 1f: 0.2f;
            Value += p.wheelDelta*ratio;
        }

        public bool TryGetValue(out float value) {
            if (text == "") {
                value = 0;
                return true;
            }

            return float.TryParse(text, out value);
        }

        public float Value {
            set => text = value.ToString();
            get => float.Parse(text);
        }


        protected override void OnTextChanged() {
            Log.Debug($"UICornerTextField.OnTextChanged() called");
            base.OnTextChanged();
            if (refreshing_ || !started_) return;
            if (TryGetValue(out float value)) {
                // fast apply-refresh.
                // don't update text ... let user type the whole numbrer.
                // deep refresh is for OnSubmit()
                SetData(value);
                SegmentEndData data = (root_ as UISegmentEndControllerPanel).SegmentEndData;
                data?.Refresh();
                resetButton_?.Refresh();
            }
        }

        protected override void OnSubmit() {
            base.OnSubmit(); // called when focus is lost. deep refresh
            if (TryGetValue(out _)) {
                Apply();
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
                throw new Exception("Unreachable code. this path is handled in OnTextChanged().");
            }

            Refresh();
        }

        public void Refresh() {
            if(VERBOSE)Log.Debug($"UICornerTextField.Refresh()called");
            refreshing_ = true;

            if (root_ is UINodeControllerPanel ncpanel)
                RefreshNode();
            else if (root_ is UISegmentEndControllerPanel secpanel)
                RefreshSegmentEnd();

            Value = GetData();
            Container.isVisible = isEnabled;
            Container.Invalidate();
            Invalidate();

            resetButton_?.Refresh();
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
            isEnabled = data.CanModifyCorners();
        }

        public void RefreshUIValueOnly() {
            refreshing_ = true;
            Value = GetData();
            refreshing_ = false;
        }
    }
}
