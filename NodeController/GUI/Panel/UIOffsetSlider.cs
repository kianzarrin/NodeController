namespace NodeController.GUI {
    using ColossalFramework.UI;
    using System;
    using UnityEngine;

    public class UIOffsetSlider : UISlider, IDataControllerUI {
        public static UIOffsetSlider Instance { get; private set; }

        public NetworkTypeT NetworkType { get; private set; }

        public override void Awake() {
            base.Awake();
            Instance = this;
        }

        UISlicedSprite slicedSprite_;
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
        }

        protected override void OnValueChanged() {
            base.OnValueChanged();
            Apply();
        }

        public void Apply() {
            switch (NetworkType) {
                case NetworkTypeT.Node:
                    ApplyNode();
                    break;
                case NetworkTypeT.SegmentEnd:
                    throw new NotImplementedException();
            }

            tooltip = value.ToString();
            RefreshTooltip();
            UIResetButton.Instance.Refresh();
        }

        public void ApplyNode() {
            NodeData data = UINodeControllerPanel.Instance.NodeData;
            if (data == null)
                return;
            data.CornerOffset = value;
            data.Refresh();
        }


        public void Refresh() {
            NodeData data = UINodeControllerPanel.Instance.NodeData;
            if (data == null) {
                Disable();
                return;
            }
            value = data.CornerOffset;

            parent.isVisible = isVisible = slicedSprite_.isEnabled = thumbObject.isEnabled = isEnabled = data.CanModifyOffset();
            parent.Invalidate();
            Invalidate();
            thumbObject.Invalidate();
            slicedSprite_.Invalidate();
            //Log.Debug($"slider.Refresh: node:{data.NodeID} isEnabled={isEnabled}\n" + Environment.StackTrace);
        }
    }
}
