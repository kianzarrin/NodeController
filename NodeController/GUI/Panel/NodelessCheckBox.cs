namespace NodeController.GUI {
    using ColossalFramework.UI;
    using System;
    using UnityEngine;
    using KianCommons;
    using static KianCommons.HelpersExtensions;
    using static KianCommons.Assertion;
    using KianCommons.UI;

    public class NodelessCheckBox : UICheckBox, IDataControllerUI {
        public string HintHotkeys => "del => reset hovered value to default";

        public string HintDescription =>
            "Removes crossing texture for all segment ends. " +
            "To ban crossings, untick this and use TMPE.";

        UIPanelBase root_;
        public override void Awake() {
            base.Awake();
            name = nameof(NodelessCheckBox);
            height = 30f;
            clipChildren = true;

            UISprite sprite = AddUIComponent<UISprite>();
            sprite.spriteName = "ToggleBase";
            sprite.size = new Vector2(19f, 19f);
            sprite.relativePosition = new Vector2(0, (height-sprite.height)/2 );
            sprite.atlas = KianCommons.UI.TextureUtil.GetAtlas("InGame");
            sprite.atlas = TextureUtil.Ingame;

            checkedBoxObject = sprite.AddUIComponent<UISprite>();
            ((UISprite)checkedBoxObject).spriteName = "ToggleBaseFocused";
            ((UISprite)checkedBoxObject).atlas = TextureUtil.Ingame;
            checkedBoxObject.size = sprite.size;
            checkedBoxObject.relativePosition = Vector3.zero;

            label = AddUIComponent<UILabel>();
            label.text = "Nodeless";
            tooltip = "modifies clips segment-ends";
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

        public override void Start() {
            base.Start();
            width = parent.width;
            root_ = GetRootContainer() as UIPanelBase;
        }

        // protection against unncessary apply/refresh/infinite recursion.
        bool refreshing_ = false;

        public void Apply() {
            if (VERBOSE) Log.Debug("NodelessCheckBox.Apply called()\n" + Environment.StackTrace);
            if (root_ is UINodeControllerPanel)
                ApplyNode();
            else if (root_ is UISegmentEndControllerPanel)
                ApplySegmentEnd();
            else
                throw new Exception("Unreachable code. root_="+ root_);
        }

        public void ApplyNode() {
            NodeData data = (root_ as UINodeControllerPanel).NodeData;
            if (data == null)
                return;
            data.Nodeless = this.isChecked;
            Assert(!refreshing_, "!refreshing_");
            data.Update();
            (root_ as IDataControllerUI).Refresh();

        }

        public void ApplySegmentEnd() {
            SegmentEndData data = (root_ as UISegmentEndControllerPanel).SegmentEndData;
            if (data == null)
                return;
            data.Nodeless = this.isChecked;
            Log.Debug($"NodelessCheckBox.ApplySegmentEnd(): {data}" +
                $"isChecked={isChecked} " +
                $"data.Nodeless is set to {data.Nodeless}");
            data.Update();
        }

        public void Refresh() {
            if (VERBOSE) Log.Debug("Refresh called()\n" + Environment.StackTrace);
            RefreshValues();
            refreshing_ = true;
            if (root_?.GetData() is NodeData nodeData)
                RefreshNode(nodeData);

            parent.isVisible = isVisible = this.isEnabled;
            Invalidate();
            refreshing_ = false;
        }

        public void RefreshNode(NodeData data) {
            if (data.HasUniformNodeless()) {
                checkedBoxObject.color = Color.white;
            } else {
                checkedBoxObject.color = Color.grey;
            }
        }

        public void RefreshValues() {
            refreshing_ = true;
            INetworkData data = root_?.GetData();
            if (data is SegmentEndData segmentEndData) {
                isChecked = segmentEndData.Nodeless;
                isEnabled = segmentEndData.ShowNodelessToggle();
            } else if (data is NodeData nodeData) {
                this.isChecked = nodeData.Nodeless;
                isEnabled = nodeData.ShowNodelessToggle();
            } else Disable();
            refreshing_ = false;
        }

        public void Reset() {
            isChecked = false;
        }
    }
}


