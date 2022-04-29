namespace NodeController.GUI {
    using ColossalFramework.UI;
    using System;
    using UnityEngine;
    using KianCommons;
    using static KianCommons.Assertion;
    using KianCommons.UI;

    public class SharpCornersCheckbox : UICheckBox, IDataControllerUI {
        public string HintHotkeys => "del => reset hovered value to default";
        public string HintDescription =>
            "make corners sharp for straight segments (overrides corner offset)";

        public override void Awake() {
            base.Awake();
            name = nameof(SharpCornersCheckbox);
            height = 20f;
            clipChildren = true;

            UISprite sprite = AddUIComponent<UISprite>();
            sprite.spriteName = "ToggleBase";
            sprite.size = new Vector2(20f, 20f);
            sprite.relativePosition = new Vector2(0, (height-sprite.height)/2 );
            sprite.atlas = TextureUtil.Ingame;

            checkedBoxObject = sprite.AddUIComponent<UISprite>();
            ((UISprite)checkedBoxObject).spriteName = "ToggleBaseFocused";
            ((UISprite)checkedBoxObject).atlas = TextureUtil.Ingame;
            checkedBoxObject.size = sprite.size;
            checkedBoxObject.relativePosition = Vector3.zero;

            label = AddUIComponent<UILabel>();
            label.text = "sharpen corners";
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
            Log.Debug("SharpCornersCheckbox.Apply called()\n"/* + Environment.StackTrace*/);
            NodeData data = root_?.GetData() as NodeData;
            if (data == null)
                return;
 
            data.SharpCorners = this.isChecked;
            Assert(!refreshing_, "!refreshing_");
            data.RefreshAndUpdate();
            root_.Refresh();
        }

        // protection against unnecessary apply/refresh/infinite recursion.
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
            if (data is NodeData nodeData) {
                isEnabled = nodeData.CanModifySharpCorners();
                if (isEnabled) {
                    this.isChecked = nodeData.SharpCorners;
                }
            } else Disable();
            refreshing_ = false;
        }

        public void Reset() {
            if (root_?.GetData() is NodeData nodeData) {
                isChecked = nodeData.DefaultSharpCorners;
            }
        }
    }
}


