namespace NodeController.GUI {
    using ColossalFramework.UI;
    using System;
    using UnityEngine;
    using KianCommons;
    using static KianCommons.HelpersExtensions;


    public class UIUnFlattenJunctionsCheckbox : UICheckBox, IDataControllerUI {
        public string HintHotkeys => "del => reset hovered value to default";
        public string HintDescription =>
            "uncheck give slope to junction/transition. Useful for highway intersections. " +
            "the two bigger segments should turn off flat junction. " +
            "minor roads joining a sloped intersection twist sideways to match the slope of the intersection.";

        public override void Awake() {
            base.Awake();
            name = nameof(UIUnFlattenJunctionsCheckbox);
            height = 20f;
            clipChildren = true;

            UISprite sprite = AddUIComponent<UISprite>();
            sprite.spriteName = "ToggleBase";
            sprite.size = new Vector2(20f, 20f);
            sprite.relativePosition = new Vector2(0, (height-sprite.height)/2 );

            checkedBoxObject = sprite.AddUIComponent<UISprite>();
            ((UISprite)checkedBoxObject).spriteName = "ToggleBaseFocused";
            checkedBoxObject.size = sprite.size;
            checkedBoxObject.relativePosition = Vector3.zero;

            label = AddUIComponent<UILabel>();
            label.text = "flatten segment end";
            label.textScale = 0.9f;
            label.relativePosition = new Vector2(sprite.width+5f, (height- label.height)/2+1);

            eventCheckChanged += OnCheckChanged;
        }

        void OnCheckChanged(UIComponent component, bool value) {
            if (refreshing_)
                return;
            Apply();
        }

        public override void OnDestroy() {
            eventCheckChanged -= OnCheckChanged;
            base.OnDestroy();
        }

        UIPanelBase root_;
        public override void Start() {
            base.Start();
            width = parent.width;
            root_ = GetRootContainer() as UIPanelBase;
        }


        public void Apply() {
            Log.Debug("UIUnFlattenJunctionsCheckbox.Apply called()\n"/* + Environment.StackTrace*/);
            SegmentEndData data = root_?.GetData() as SegmentEndData;
            if (data == null)
                return;
 
            data.FlatJunctions = this.isChecked;
            if (!this.isChecked)
                data.Twist = false;
            else
                data.Twist = data.DefaultTwist;
            data.DeltaSlopeAngleDeg = 0;
            Assert(!refreshing_, "!refreshing_");
            data.RefreshAndUpdate();
            root_.Refresh();
        }

        // protection against unncessary apply/refresh/infinite recursion.
        bool refreshing_ = false;

        public void Refresh() {
            //Log.Debug("Refresh called()\n"/* + Environment.StackTrace*/);
            RefreshValues();
            refreshing_ = true;

            //if (root_?.GetData() is NodeData nodeData)
            //    RefreshNode(nodeData);

            parent.isVisible = isVisible = this.isEnabled;
            parent.Invalidate();
            Invalidate();
            refreshing_ = false;
        }

        public void RefreshValues() {
            refreshing_ = true;
            INetworkData data = root_?.GetData();
            if (data is SegmentEndData segmentEndData) {
                isEnabled = segmentEndData.CanModifyFlatJunctions();
                if (isEnabled) {
                    this.isChecked = segmentEndData.FlatJunctions;
                }
            } else Disable();
            refreshing_ = false;
        }

        public void Reset() {
            if (root_?.GetData() is SegmentEndData segmentEndData) {
                isChecked = segmentEndData.DefaultFlatJunctions;
            }
        }
    }
}


