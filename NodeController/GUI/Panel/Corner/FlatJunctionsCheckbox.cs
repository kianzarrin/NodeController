namespace NodeController.GUI {
    using ColossalFramework.UI;
    using System;
    using UnityEngine;
    using KianCommons;
    using static KianCommons.HelpersExtensions;


    public class UIFlatJunctionsCheckbox : UICheckBox, IDataControllerUI {
        public static UIFlatJunctionsCheckbox Instance { get; private set; }

        public override void Awake() {
            base.Awake();
            Instance = this;
            name = nameof(UIFlatJunctionsCheckbox);
            height = 30f;
            clipChildren = true;

            UISprite sprite = AddUIComponent<UISprite>();
            sprite.spriteName = "ToggleBase";
            sprite.size = new Vector2(19f, 19f);
            sprite.relativePosition = new Vector2(0, (height-sprite.height)/2 );

            checkedBoxObject = sprite.AddUIComponent<UISprite>();
            ((UISprite)checkedBoxObject).spriteName = "ToggleBaseFocused";
            checkedBoxObject.size = sprite.size;
            checkedBoxObject.relativePosition = Vector3.zero;

            label = AddUIComponent<UILabel>();
            label.text = "Flat junctions";
            label.textScale = 0.9f;
            label.relativePosition = new Vector2(sprite.width+5f, (height- label.height)/2+1);
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
            Log.Debug("UIFlatJunctionsCheckbox.Apply called()\n"/* + Environment.StackTrace*/);
            SegmentEndData data = root_?.GetData() as SegmentEndData;
            if (data == null)
                return;
 
            data.FlatJunctions = this.isChecked;
            if (!this.isChecked)
                data.Twist = false;
            else
                data.Twist = data.DefaultTwist;
            data.UpdateSlopeAngleDeg = true; // reset slope angle.
            Assert(!refreshing_, "!refreshing_");
            data.Update();
            root_.Refresh();
        }

        // protection against unncessary apply/refresh/infinite recursion.
        bool refreshing_ = false;

        public void Refresh() {
            Log.Debug("Refresh called()\n"/* + Environment.StackTrace*/);
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


