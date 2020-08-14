namespace NodeController.GUI {
    using ColossalFramework.UI;
    using KianCommons;
    using System;
    using UnityEngine;

    public class UIOffsetSlider : UISlider, IDataControllerUI {
        public static UIOffsetSlider Instance { get; private set; }

        public override void Awake() {
            base.Awake();
            Instance = this;
        }

        UISlicedSprite slicedSprite_;
        UIResetButton resetButton_;
        UIPanel root_;
        public override void Start() {
            base.Start();

            builtinKeyNavigation = true;
            isInteractive = true;
            color = Color.grey;
            name = name;
            height = 15f;
            float padding = 0; // contianer has padding
            width = parent.width - 2 * padding;

            maxValue = 100;
            minValue = 0;
            stepSize = 1;
            AlignTo(parent, UIAlignAnchor.TopLeft);

            KianCommons.Log.Debug("parent:" + parent);
            slicedSprite_ = AddUIComponent<UISlicedSprite>();
            slicedSprite_.spriteName = "ScrollbarTrack";
            slicedSprite_.height = 12;
            slicedSprite_.width = width;
            slicedSprite_.relativePosition = new Vector3(padding, 2f);

            UISprite thumbSprite = AddUIComponent<UISprite>();
            thumbSprite.spriteName = "ScrollbarThumb";
            thumbSprite.height = 20f;
            thumbSprite.width = 10f;
            thumbObject = thumbSprite;
            thumbOffset = new Vector2(padding, 0);

            value = 0;

            eventSizeChanged += (component, value) => {
                // TODO [clean up] is this necessary? move it to override.
                slicedSprite_.width = slicedSprite_.parent.width - 2*padding;
            };

            root_ = GetRootContainer() as UIPanel;
            resetButton_ = root_.GetComponentInChildren<UIResetButton>();
        }

        protected override void OnValueChanged() {
            base.OnValueChanged();
            Apply();
        }

        public void Apply() {
            if (refreshing_) return;
            if (root_ is UINodeControllerPanel ncpanel) 
                ApplyNode();
            else if( root_ is UISegmentEndControllerPanel secpanel)
                ApplySegmentEnd();

            tooltip = value.ToString();
            RefreshTooltip();
            resetButton_?.Refresh();
            Refresh();
        }

        public void ApplyNode() {
            NodeData data = (root_ as UINodeControllerPanel).NodeData;
            if (data == null)
                return;
            data.CornerOffset = value;
            data.Refresh();
        }
        public void ApplySegmentEnd() {
            SegmentEndData data = (root_ as UISegmentEndControllerPanel).SegmentEndData;
            if (data == null)
                return;
            data.CornerOffset = value;
            data.Refresh();
        }

        bool refreshing_ = false;
        public void Refresh() {
            refreshing_ = true;
            if (root_ is UINodeControllerPanel ncpanel)
                RefreshNode();
            else if (root_ is UISegmentEndControllerPanel secpanel)
                RefreshSegmentEnd();

            parent.isVisible = isEnabled;
            parent.Invalidate();
            Invalidate();
            thumbObject.Invalidate();
            slicedSprite_.Invalidate();
            //Log.Debug($"slider.Refresh: node:{data.NodeID} isEnabled={isEnabled}\n" + Environment.StackTrace);
            refreshing_ = false;
        }

        public void RefreshNode() {
            NodeData data = (root_ as UINodeControllerPanel).NodeData;
            if (data == null) {
                Disable();
                return;
            }
            value = data.CornerOffset;
            isEnabled = data.CanModifyOffset();

            if (data.HasUniformCornerOffset()) {
                thumbObject.color = Color.white;
                thumbObject.opacity = 1;
            } else {
                thumbObject.color = Color.grey;
                thumbObject.opacity = 0.2f;
            }
        }

        public void RefreshSegmentEnd() {
            SegmentEndData data = (root_ as UISegmentEndControllerPanel).SegmentEndData;
            if (data == null) {
                Disable();
                return;
            }
            value = data.CornerOffset;
            isEnabled = data.CanModifyOffset();
        }
    }
}
