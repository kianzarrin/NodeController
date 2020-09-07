namespace NodeController.GUI {
    using KianCommons.UI;
    using KianCommons;
    using ColossalFramework.UI;
    using static KianCommons.HelpersExtensions;
    using UnityEngine;
    using System;

    public abstract class UISliderBase: UISliderExt, IDataControllerUI {
        //UIResetButton resetButton_ => Root?.ResetButton;
        protected UIPanelBase Root;

        public override void Awake() {
            base.Awake();
            tooltip = null;
            //stepSize = 0f; // no quantization while reading.
        }

        public string HintHotkeys => "mousewheel/keypad arrows => increment/decrement\n" +
            "shift + mousewheel/keypad arrows => fine increment/decrement\n" +
            "del => reset to default";

        public string HintDescription => null;

        public override void Start() {
            base.Start();
            Root = GetRootContainer() as UIPanelBase;
        }

        #region Scroll/Drag step/rounding size

        protected override void OnValueChanged() {
            base.OnValueChanged();
            if (RefreshingValues && Root?.GetData() is NodeData nodeData) {
                bool mixed = MixedValues;
                RefreshNode(nodeData);
                if(mixed != MixedValues)
                    Invalidate();
            }else if (!Refreshing)
                Apply();
            //UpdateTooltip();
        }

        public virtual bool CourseMode => ShiftIsPressed;

        public float LargeScrollStep = 10;
        public float CourseScrollStep = 2;
        public float LargeDragStep = 5;
        public float CourseDragStep = 1;
        // step size is read step.

        float ScrollStep => CourseMode ? CourseScrollStep : LargeScrollStep;
        float DragStep => CourseMode ? CourseDragStep : LargeDragStep;

#if false
        protected override void OnMouseWheel(UIMouseEventParameter p) {
            Log.Debug(GetType().Name + "\n" + Environment.StackTrace);
            scrollWheelAmount = ScrollStep;
            this.value += UISliderExt.Quantize(scrollWheelAmount * p.wheelDelta, scrollWheelAmount);
            p.Use();
        }

        protected override void OnMouseDown(UIMouseEventParameter p) {
            base.OnMouseDown(p);
            QuantizeValue(DragStep);
        }

        protected override void OnMouseMove(UIMouseEventParameter p) {
            base.OnMouseMove(p);
            QuantizeValue(DragStep);
        }

        protected override void OnKeyDown(UIKeyEventParameter p) {
            scrollWheelAmount = ScrollStep;
            base.OnKeyDown(p);
            bool arrowkey = p.keycode == KeyCode.LeftArrow || p.keycode == KeyCode.RightArrow
                || p.keycode == KeyCode.UpArrow || p.keycode == KeyCode.DownArrow;
            if (arrowkey)
                QuantizeValue(ScrollStep);
            if (p.keycode == KeyCode.Delete)
                ResetToDefaultValue();
        }
#endif
        #endregion

        public void Apply() {
            Assert(!Refreshing);
            object data = Root.GetData();
            if (data is NodeData nodeData) {
                ApplyNode(nodeData);
                nodeData.Update();
            } else if (data is SegmentEndData segEndData) {
                ApplySegmentEnd(segEndData);
                segEndData.Update();
            }

            Root?.RefreshValues();
        }

        /// <summary>set data value. data refresh is already taken care of</summary>
        public abstract void ApplyNode(NodeData data);

        /// <summary>set data value. data refresh is already taken care of</summary>
        public abstract void ApplySegmentEnd(SegmentEndData data);


        public virtual string TooltipPostfix => "";

        public virtual void ResetToDefaultValue() => value = 0;

        protected bool Refreshing = false;
        public virtual void Refresh() {
            try {
                //Log.Debug("UISliderBase.Refresh() was called\n" /*+ Environment.StackTrace*/);
                INetworkData data = Root.GetData();
                RefreshValues();

                // RefreshValues sets and then unsets Refreshing. therefore this must be called after RefreshValues.
                Refreshing = true;

                if (isEnabled && data is NodeData nodeData)
                    RefreshNode(nodeData);

                parent.isVisible = isEnabled;
                parent.Invalidate();
                //Invalidate(); // TODO is this redundant?
                //thumbObject.Invalidate();
                //SlicedSprite.Invalidate();
                //Log.Debug($"slider.Refresh: node:{data.NodeID} isEnabled={isEnabled}\n" + Environment.StackTrace);
            }
            finally {
                Refreshing = false;
            }
        }

        /// <summary>
        /// <see cref="RefreshNodeValues()"/> is called before hand.
        /// Precondition: isEnabled = true
        /// setup opactiy, color, ...
        /// </summary>
        public abstract void RefreshNode(NodeData data);

        /// <summary>
        /// read isEnabled and value
        /// </summary>
        public abstract void RefreshNodeValues(NodeData data);

        /// <summary>
        /// read isEnabled and value
        /// </summary>
        public abstract void RefreshSegmentEndValues(SegmentEndData data);

        bool RefreshingValues;
        public virtual void RefreshValues() {
            try {
                RefreshingValues = Refreshing = true;
                INetworkData data = Root?.GetData();
                if (data is SegmentEndData segmentEndData) {
                    RefreshSegmentEndValues(segmentEndData);
                } else if (data is NodeData nodeData) {
                    RefreshNodeValues(nodeData);
                } else Disable();
           }
            finally {
                RefreshingValues = Refreshing = false;
            }
        }

        //public bool TooltipVisible = false;
        //public virtual void UpdateTooltip() {
        //    if (TooltipVisible) {
        //        tooltip = value.ToString() + TooltipPostfix;
        //        RefreshTooltip();
        //    } else {
        //        tooltip = null;
        //        RefreshTooltip();
        //    }
        //}
    }
}
