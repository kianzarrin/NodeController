namespace NodeController.GUI {
    using ColossalFramework.UI;
    using System;
    using UnityEngine;
    using KianCommons;
    using static KianCommons.HelpersExtensions;

    public class TwistCheckbox : UICheckBox, IDataControllerUI {
        public static TwistCheckbox Instance { get; private set; }
        public UISprite UncheckedSprite, CheckedSprite;

        public override void Awake() {
            base.Awake();
            Instance = this;
            name = nameof(TwistCheckbox);
            height = 30f;
            clipChildren = true;

            UISprite sprite = UncheckedSprite = AddUIComponent<UISprite>();
            sprite.spriteName = "ToggleBase";
            sprite.size = new Vector2(19f, 19f);
            sprite.relativePosition = new Vector2(0, (height - sprite.height) / 2);

            checkedBoxObject = CheckedSprite = sprite.AddUIComponent<UISprite>();
            ((UISprite)checkedBoxObject).spriteName = "ToggleBaseFocused";
            checkedBoxObject.size = sprite.size;
            checkedBoxObject.relativePosition = Vector3.zero;

            label = AddUIComponent<UILabel>();
            label.text = "Twist segment ends";
            label.textScale = 0.9f;
            label.relativePosition = new Vector2(sprite.width + 5f, (height - label.height) / 2 + 1);
            tooltip = "If turned off, junctions could have slope. Useful for highway intersections.\n" +
                "the two bigger segments should have flat junctions turned off. minor roads joining a tilted junction might need some manual sideway twisting.";

            eventCheckChanged += (component, value) => {
                if (refreshing_)
                    return;
                Apply();
            };
        }

        UIPanelBase root_;
        public override void Start() {
            base.Start();
            width = parent.width;
            root_ = GetRootContainer() as UIPanelBase;
        }

        public void Apply() {
            Log.Debug("TwistCheckbox.Apply called()\n"/* + Environment.StackTrace*/);
            var data = (root_ as UISegmentEndControllerPanel).SegmentEndData;
            if (data == null)
                return;
            data.Twist = this.isChecked;
            data.DeltaSlopeAngleDeg = 0;
            Assert(!refreshing_, "!refreshing_");
            data.RefreshAndUpdate();
            root_.Refresh();
        }

        // protection against unncessary apply/refresh/infinite recursion.
        bool refreshing_ = false;

        public void Refresh() {
            Log.Debug("Refresh called()\n" + Environment.StackTrace);
            refreshing_ = true;

            RefreshValues();
            //if (root_?.GetData() is NodeData nodeData)
            //    RefreshNode(nodeData);

            if (isEnabled) {
                label.color = UncheckedSprite.color = CheckedSprite.color  = Color.white;

            } else {
                byte c = 75;
                label.color = UncheckedSprite.color = CheckedSprite.color = new Color32(c, c, c, c);
            }

            parent.Invalidate();
            Invalidate();
            Log.Debug($"TwistChekbox.Refresh() visible={isVisible}");

            refreshing_ = false;
        }

        public void RefreshValues() {
            INetworkData data = root_?.GetData();
            if (data is SegmentEndData segmentEndData) {
                bool b = segmentEndData.CanModifyTwist();
                if (b) {
                    this.isChecked = segmentEndData.Twist;
                    isEnabled = segmentEndData.FlatJunctions;
                }
                isVisible = parent.isVisible = b;
                //Log.Debug($"TwistChekbox.Refresh() isVisible={isVisible} b={b}");
            } else if (data is NodeData nodeData) {
                Hide();
            } else Hide();
        }
    }
}


