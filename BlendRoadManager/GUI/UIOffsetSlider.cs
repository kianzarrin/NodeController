namespace BlendRoadManager.GUI {
    using BlendRoadManager.Util;
    using ColossalFramework;
    using ColossalFramework.UI;
    using System;
    using UnityEngine;
    using static Util.HelpersExtensions;

    public class UIOffsetSlider : UISlider, IDataControllerUI {
        public static UIOffsetSlider Instance { get; private set; }

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
            width = parent.width - 10f;

            maxValue = 100;
            minValue = 0;
            stepSize = 1;
            AlignTo(parent, UIAlignAnchor.TopLeft);

            Log.Debug("parent:" + parent);
            slicedSprite_ = AddUIComponent<UISlicedSprite>();
            slicedSprite_.spriteName = "ScrollbarTrack";
            slicedSprite_.height = 12;
            slicedSprite_.width = width;
            slicedSprite_.relativePosition = new Vector3(0, 2f);

            UISprite thumbSprite = AddUIComponent<UISprite>();
            thumbSprite.spriteName = "ScrollbarThumb";
            thumbSprite.height = 20f;
            thumbSprite.width = 10f;
            thumbObject = thumbSprite;

            value = 0;

            eventSizeChanged += (component, value) => {
                slicedSprite_.width = slicedSprite_.parent.width;
            };
        }

        protected override void OnValueChanged() {
            base.OnValueChanged();
            Apply();
        }

        public void Apply() {
            NodeData data = UINodeControllerPanel.Instance.BlendData;
            if (data == null)
                return;
            data.CornerOffset = value;
            data.Refresh();
            tooltip = value.ToString();
            RefreshTooltip();
        }

        public void Refresh() {
            NodeData data = UINodeControllerPanel.Instance.BlendData;
            if (data == null) {
                Disable();
                return;
            }
            value = data.CornerOffset;

            isVisible = slicedSprite_.isEnabled = thumbObject.isEnabled = isEnabled = data.CanModifyOffset();
            Invalidate();
            thumbObject.Invalidate();
            slicedSprite_.Invalidate();
            //Log.Debug($"slider.Refresh: node:{data.NodeID} isEnabled={isEnabled}\n" + Environment.StackTrace);
        }
    }
}
