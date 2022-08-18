namespace NodeController.GUI {
    using ColossalFramework;
    using ColossalFramework.UI;
    using KianCommons;
    using KianCommons.UI;
    using System;
    using UnityEngine;
    using static KianCommons.Helpers;
    using NodeController.Tool;

    public class UICornerTextField : UITextField, IDataControllerUI, IToolUpdate {
        //UIResetButton resetButton_;
        UIPanelBase root_;
        bool started_ = false;
        bool refreshing_ = false, refreshingValues_ = false;

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

        public string HintHotkeys {
            get {
                if (containsFocus)
                    return null;
                string ret = "mouse-wheel => increment/decrement.\n" +
                    "shift + mouse-wheel => large increment/decrement.\n" +
                    "del => reset hovered value to default" +
                    (MixedValues ? "\nyellow color = mixed values" : "");


                if (Mirror != null) {
                    ret += "\n";
                    ret += "control + mouse-wheel => link corresponding text field\n";
                    ret += "control + alt + mouse-wheel => invert link corresponding text field";
                }
                return ret;
            }
        }

        public string HintDescription { get; set; } = null;


        public delegate float GetDataFunc();
        public delegate bool IsMixedFunc();
        public delegate void SetDataFunc(float data);
        public delegate void ResetFunc();

        public IsMixedFunc IsMixed;
        public GetDataFunc GetData;
        public GetDataFunc GetDefault;
        public SetDataFunc SetData;
        public UIComponent Container; // that will be set visible or invisible.
        public ResetFunc ResetToDefault;


        public UICornerTextField Mirror;
        public static bool LockMode => ControlIsPressed && !AltIsPressed;
        public static bool InvertLockMode => ControlIsPressed && AltIsPressed;

        public override void Awake() {
            base.Awake();
            atlas = TextureUtil.Ingame;
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

            submitOnFocusLost = true;
            selectOnFocus = true;
            numericalOnly = true;
            allowFloats = true;
            allowNegative = true;
        }


        Color GetColor() {
            if (containsMouse) {
                if (Mirror != null && LockMode || InvertLockMode)
                    return Color.green;
                else
                    return Color.white;
            }

            if (Mirror != null && Mirror.containsMouse) {
                if (LockMode)
                    return Color.green;
                else if (InvertLockMode)
                    return Color.Lerp(Color.blue, Color.cyan, 0.3f);
            }

            if (containsFocus)
                return Color.white;

            return Color.Lerp(Color.grey, Color.white, 0.60f);
        }

        public void OnToolUpdate() {
            var c = GetColor();
            color = Color.Lerp(c, Color.white, 0.70f);
            if (MixedValues)
                color = Color.Lerp(c, Color.yellow, 0.2f);
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

        public float MinStep => MouseWheelRatio * 0.01f; // round to this.
        private float mouseWheelRatio_ = 1;
        private string format_ = "0.##";
        public float MouseWheelRatio {
            get => mouseWheelRatio_;
            set {
                mouseWheelRatio_ = value;
                int n = Mathf.CeilToInt(-Mathf.Log10(value));
                format_ = "0." + "#" + new String('#', n);
            }
        }
        public bool CourseMode => ShiftIsPressed;
        float ScrollStep => (CourseMode ? 0.2f : 1f) * MouseWheelRatio;

        protected override void OnMouseWheel(UIMouseEventParameter p) {
            base.OnMouseWheel(p);
            AddDelta(-p.wheelDelta * ScrollStep, ScrollStep);
        }

        public void Reset() {
            if (ResetToDefault != null)
                ResetToDefault();
            else
                SetData(0f);

            if (LockMode) {
                if (Mirror?.ResetToDefault != null)
                    Mirror.ResetToDefault();
                else
                    Mirror.SetData(0f);
            }
        }

        //protected override void OnKeyDown(UIKeyEventParameter p) {
        //    Log.Debug(name + ": OnKeyDown");
        //    if (!containsFocus && base.builtinKeyNavigation) {
        //        if (p.keycode == KeyCode.LeftArrow || p.keycode == KeyCode.DownArrow) {
        //            AddDelta(-ScrollStep, ScrollStep);
        //            p.Use();
        //            return;
        //        } else if (p.keycode == KeyCode.RightArrow || p.keycode == KeyCode.UpArrow) {
        //            AddDelta(+ScrollStep, ScrollStep);
        //            p.Use();
        //            return;
        //        } else if (p.keycode == KeyCode.Delete) {
        //            ResetToDefault();
        //            if (Mirror != null && LockMode)
        //                Mirror.ResetToDefault();
        //            p.Use();
        //            return;
        //        }
        //    }
        //    base.OnKeyDown(p);
        //}

        /// <summary>
        /// adds delta to Value rounding to step.
        /// also modifies the mirror.
        /// </summary>
        /// <returns>final delta in Value after rounding</returns>
        public float AddDelta(float delta, float step) {
            Log.Debug(Environment.StackTrace);
            delta = Value - (Value + delta).RoundToNearest(step); // we need final delta for Mirror values.
            Value += delta;

            if (Mirror == null) {
                // nothing
            } else if (LockMode) {
                Mirror.Value += delta;
            } else if (InvertLockMode) {
                Mirror.Value -= delta;
            }
            return delta;
        }


        public string StrippedText => PostFix != "" ? text.Replace(PostFix, "") : text;

        public bool TryGetValue(out float value) {
            string text2 = StrippedText;
            if (text2 == "") {
                value = 0;
                return true;
            }

            var ret = float.TryParse(text2, out value);
            value = value.RoundToNearest(MinStep);
            return ret;
        }

        public float Value {
            set => text = value.RoundToNearest(MinStep).ToString(format_) + PostFix;
            get => float.Parse(StrippedText).RoundToNearest(MinStep);
        }

        protected override void OnTextChanged() {
            //Log.Debug($"UICornerTextField.OnTextChanged() called");
            base.OnTextChanged();
            if (refreshing_ || !started_) return;
            if (TryGetValue(out float value)) {
                // fast apply-refresh.
                // don't update text ... let user type the whole number.
                // deep refresh is for OnSubmit()
                SetData(value);
                root_?.GetData()?.Update();
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
            if (!isEnabled || refreshing_ || !started_) return;
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
            if (Log.VERBOSE) Log.Debug($"UICornerTextField.Refresh()called");
            try {
                refreshing_ = true;

                var data = root_?.GetData();
                //data?.Update();
                if (data is NodeData nodeData) {
                    if (name == "NodeCornerOffset")
                        isEnabled = nodeData.CanModifyOffset();
                    else
                        isEnabled = nodeData.CanMassEditNodeCorners();
                } else if (data is SegmentEndData segEndData)
                    isEnabled = segEndData.CanModifyCorners();
                else
                    Disable();

                if (isEnabled) {
                    Value = GetData();
                    if (IsMixed != null)
                        MixedValues = IsMixed();
                }

                Container.isVisible = isEnabled;
                Container.Invalidate();
                Invalidate();
            }
            finally {
                refreshing_ = false;
            }
        }

        public void RefreshValues() {
            if (containsFocus)
                return;
            try {
                refreshingValues_ = refreshing_ = true;
                if (isEnabled && isVisible) {
                    Value = GetData();
                    if (IsMixed != null) {
                        bool mixed = IsMixed();
                        if (mixed != MixedValues) {
                            MixedValues = IsMixed();
                            Invalidate();
                        }
                    }
                }
            }
            finally {
                refreshingValues_ = refreshing_ = false;
            }
        }

        public bool MixedValues = false;
    }
}
