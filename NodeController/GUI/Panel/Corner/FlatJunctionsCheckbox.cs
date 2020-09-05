namespace NodeController.GUI {
    using ColossalFramework.UI;
    using System;
    using UnityEngine;
    using KianCommons;
    using static KianCommons.HelpersExtensions;


    public class UIFlatJunctionsCheckbox : UICheckBox, IDataControllerUI {
        public string HintHotkeys => null;
        public string HintDescription =>
            "turn of to give slope to junctions. Useful for highway intersections. " +
            "the two bigger segments should turn off flat junction. " +
            "minor roads joining a sloped intersection twist sideways to matcht the slope of the intersection.";

        public override void Awake() {
            base.Awake();
            name = nameof(UIFlatJunctionsCheckbox);
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
            label.text = "Flatten junction";
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
            Log.Debug("UIFlatJunctionsCheckbox.Apply called()\n"/* + Environment.StackTrace*/);
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
            refreshing_ = true;

            RefreshValues();
            //if (root_?.GetData() is NodeData nodeData)
            //    RefreshNode(nodeData);

            parent.isVisible = isVisible = this.isEnabled;
            parent.Invalidate();
            Invalidate();
            refreshing_ = false;
        }

        public void RefreshValues() {
            INetworkData data = root_?.GetData();
            if (data is SegmentEndData segmentEndData) {
                this.isChecked = segmentEndData.FlatJunctions;
                isEnabled = segmentEndData.CanModifyFlatJunctions();
            } else if (data is NodeData nodeData) {
                //this.isChecked = nodeData.FlatJunctions; // TODO complete
                isEnabled = nodeData.CanModifyFlatJunctions();
            } else Disable();
        }
    }
}


