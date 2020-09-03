namespace NodeController.GUI {
    using ColossalFramework.UI;
    using UnityEngine;
    using static KianCommons.HelpersExtensions;
    using KianCommons;
    using KianCommons.UI;
    using System;
    using ColossalFramework;

    public class UICornerTextField : UITextField, IDataControllerUI {
        //UIResetButton resetButton_;
        UIPanelBase root_;
        bool started_ = false;
        bool refreshing_ = false;

        public override string ToString() => GetType().Name + $"({name})";

        private string _postfix = "";
        public string PostFix {
            get => _postfix;
            set {
                if (value.IsNullOrWhiteSpace())
                    _postfix = "";
                else
                    _postfix = value;
            }
        }

        public delegate float GetDataFunc();
        public delegate void SetDataFunc(float data);
        public GetDataFunc GetData;
        public SetDataFunc SetData;
        public UIComponent Container; // that will be set visible or invisible.

        public UICornerTextField Mirror;
        public static bool LockMode => ControlIsPressed && !AltIsPressed;
        public static bool InvertLockMode => ControlIsPressed && AltIsPressed;

        public override void Awake() {
            base.Awake();
            atlas = TextureUtil.GetAtlas("Ingame");
            size = UIPanelBase.CELL_SIZE;
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


        Color GetColor() {
            if (containsMouse) {
                if (LockMode || InvertLockMode)
                    return Color.green;
                else
                    return Color.white;
            }

            if (Mirror != null && Mirror.containsMouse) {
                if (LockMode)
                    return Color.green;
                else if (InvertLockMode)
                    return Color.Lerp(Color.blue, Color.cyan,0.3f);
            }

            if (containsFocus)
                return Color.white;

            return Color.Lerp(Color.grey, Color.white, 0.60f);
        }

        public override void Update() {
            base.Update();
            var c = GetColor();
            color = Color.Lerp(c, Color.white, 0.70f);
        }

        public override void Start() {
            base.Start();
            Container = Container ?? parent;
            root_ = GetRootContainer() as UIPanelBase;
            //resetButton_ = root_.GetComponentInChildren<UIResetButton>();
            started_ = true;
        }

        protected override void OnSizeChanged() {
            base.OnSizeChanged();
            // change width to match parent?
        }

        public float MouseWheelRatio = 1; // set to 0.1 for dir vectors.
        public float minStep_ => MouseWheelRatio * 0.01f; // round to this.

        protected override void OnMouseWheel(UIMouseEventParameter p) {
            base.OnMouseWheel(p);
            float ratio = HelpersExtensions.ShiftIsPressed ? 1f: 0.2f;
            float delta = p.wheelDelta * ratio * MouseWheelRatio;

            Value += delta;

            if (Mirror == null)
                return;
            else if (LockMode)
                Mirror.Value += delta;
            else if(InvertLockMode)
                Mirror.Value -= delta;
        }

        public string StrippedText => PostFix != "" ? text.Replace(PostFix, "") : text;

        public bool TryGetValue(out float value) {
            string text2 = StrippedText;
            if (text2 == "") {
                value = 0;
                return true;
            }

            var ret = float.TryParse(text2, out value);
            value = value.RoundToNearest(minStep_);
            return ret;
        }

        public float Value {
            set => text = value.RoundToNearest(minStep_).ToString("0.######") + PostFix;
            get => float.Parse(StrippedText).RoundToNearest(minStep_);
        }

        protected override void OnTextChanged() {
            //Log.Debug($"UICornerTextField.OnTextChanged() called");
            base.OnTextChanged();
            if (refreshing_ || !started_) return;
            if (TryGetValue(out float value)) {
                // fast apply-refresh.
                // don't update text ... let user type the whole numbrer.
                // deep refresh is for OnSubmit()
                SetData(value);
                SegmentEndData data = (root_ as UISegmentEndControllerPanel)?.SegmentEndData;
                data?.Update();
                root_?.RefreshValues(); // refresh values early
            }
        }

        protected override void OnSubmit() {
            Log.Debug(this + $".OnSubmit() called");
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
            Log.Debug(this + $".Apply() called");
            if (TryGetValue(out float value)) {
                Log.Debug($"UICornerTextField.Apply : calling SetData()  ");
                SetData(value);
            } else {
                throw new Exception("Unreachable code. this path is handled in OnTextChanged().");
            }
            root_?.RefreshValues();
            Refresh();
        }

        public void Refresh() {
            if(VERBOSE)Log.Debug($"UICornerTextField.Refresh()called");
            try {
                refreshing_ = true;


                if (root_ is UINodeControllerPanel ncpanel)
                    RefreshNode();
                else if (root_ is UISegmentEndControllerPanel secpanel)
                    RefreshSegmentEnd();

                Value = GetData();
                Container.isVisible = isEnabled;
                Container.Invalidate();
                Invalidate();
            }
            finally {
                refreshing_ = false;
            }
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
            data.Update();
            isEnabled = data.CanModifyCorners();
        }

        public void RefreshValues() {
            if (containsFocus)
                return;
            try {
                refreshing_ = true;
                Value = GetData();
            }
            finally {
                refreshing_ = false;
            }
        }
    }
}
