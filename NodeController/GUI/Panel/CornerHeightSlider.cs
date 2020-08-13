namespace NodeController.GUI {
    using ColossalFramework.UI;
    using KianCommons;
    using System;
    using UnityEngine;

    public class ConrnerHeightSlider : UISlider, IDataControllerUI {
        public static ConrnerHeightSlider Instance { get; private set; }

        public override void Awake() {
            base.Awake();
            Instance = this;
        }

        UISlicedSprite slicedSprite_;
        UIResetButton resetButton_;
        UIPanel root_;

        public bool bLeftSide; // left side comming toward the segment end.

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
            if (root_ == UINodeControllerPanel.Instance) 
                ApplyNode();
            else if( root_ == UISegmentEndControllerPanel.Instance)
                ApplySegmentEnd();

            tooltip = (value-50).ToString();
            RefreshTooltip();
            resetButton_?.Refresh();
            Refresh();
        }

        public void ApplyNode() {
            throw new NotImplementedException();
        }

        public void ApplySegmentEnd() {
            SegmentEndData data = UISegmentEndControllerPanel.Instance.SegmentEndData;
            if (data == null)
                return;
            if (bLeftSide) data.HLeft = value-50;
            else data.HRight = value-50;
            data.Refresh();
        }

        bool refreshing_ = false;
        public void Refresh() {
            refreshing_ = true;
            if (root_ == UINodeControllerPanel.Instance)
                RefreshNode();
            else if (root_ == UISegmentEndControllerPanel.Instance)
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
            throw new NotImplementedException();
        }

        public void RefreshSegmentEnd() {
            SegmentEndData data = UISegmentEndControllerPanel.Instance.SegmentEndData;
            if (data == null) {
                Disable();
                return;
            }
            if (bLeftSide) value = data.HLeft+50;
            else value = data.HRight+50;
            isEnabled = true;
        }
    }
}
