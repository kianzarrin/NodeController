namespace NodeController.GUI {
    using ColossalFramework.UI;
    using KianCommons;
    using KianCommons.UI;
    using System;
    using UnityEngine;
    using static KianCommons.HelpersExtensions;
    using static KianCommons.ReflectionHelpers;

    public class NodelessCheckbox : UICheckBox, IDataControllerUI {
        public static NodelessCheckbox Instance { get; private set; }

        public string HintHotkeys => "del => reset hovered value to default";

        public string HintDescription => "make this segment end node-less.";

        UIPanelBase root_;
        public override void Awake() {
            base.Awake();
            Instance = this;
            name = nameof(NodelessCheckbox);
            height = 30f;
            clipChildren = true;

            UISprite sprite = AddUIComponent<UISprite>();
            sprite.spriteName = "ToggleBase";
            sprite.size = new Vector2(19f, 19f);
            sprite.relativePosition = new Vector2(0, (height - sprite.height) / 2);
            sprite.atlas = KianCommons.UI.TextureUtil.GetAtlas("InGame");
            sprite.atlas = TextureUtil.Ingame;

            checkedBoxObject = sprite.AddUIComponent<UISprite>();
            ((UISprite)checkedBoxObject).spriteName = "ToggleBaseFocused";
            ((UISprite)checkedBoxObject).atlas = TextureUtil.Ingame;
            checkedBoxObject.size = sprite.size;
            checkedBoxObject.relativePosition = Vector3.zero;

            label = AddUIComponent<UILabel>();
            label.text = "node-less";
            label.textScale = 0.9f;
            label.relativePosition = new Vector2(sprite.width + 5f, (height - label.height) / 2 + 1);

            eventCheckChanged += OnCheckChanged;
        }

        void OnCheckChanged(UIComponent component, bool value) {
            if(refreshing_)
                return;
            Apply();
        }

        public override void OnDestroy() {
            eventCheckChanged -= OnCheckChanged;
            base.OnDestroy();
        }

        public override void Start() {
            base.Start();
            width = parent.width;
            root_ = GetRootContainer() as UIPanelBase;
        }

        // protection against unnecessary apply/refresh/infinite recursion.
        bool refreshing_ = false;

        public void Apply() {
            if(VERBOSE) Log.Debug(ThisMethod + " called\n" + Environment.StackTrace);
            if(root_ is UINodeControllerPanel)
                throw new NotImplementedException();
            else if(root_ is UISegmentEndControllerPanel segPanel) {
                ApplySegmentEnd();
                Assertion.Assert(!refreshing_, "!refreshing_");
                SegmentEndData data = segPanel.SegmentEndData;
                data?.RefreshAndUpdate();
                segPanel.Refresh();
            } else {
                throw new Exception("Unreachable code. root_=" + root_);
            }
        }

        public void ApplySegmentEnd() {
            SegmentEndData data = (root_ as UISegmentEndControllerPanel).SegmentEndData;
            if(data == null)
                return;
            data.Nodeless = this.isChecked;
            Log.Debug($"{ThisMethod}: {data}" +
                $"isChecked={isChecked} " +
                $"data.Nodeless is set to {data.Nodeless}");
            data.Update();
        }

        public void Refresh() {
            if(VERBOSE) Log.Debug("Refresh called()\n" + Environment.StackTrace);
            RefreshValues();
            parent.isVisible = isVisible = this.isEnabled;
            Invalidate();
        }

        public void RefreshValues() {
            refreshing_ = true;
            INetworkData data = root_?.GetData();
            if(data is SegmentEndData segmentEndData) {
                isChecked = segmentEndData.Nodeless;
                isEnabled = segmentEndData.CanModifyNodeless();
            } else if(data is NodeData nodeData) {
                throw new NotImplementedException();
            } else Disable();
            refreshing_ = false;
        }

        public void Reset() {
            isChecked = false;
        }
    }
}


