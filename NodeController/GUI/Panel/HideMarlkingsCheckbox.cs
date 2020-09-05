namespace NodeController.GUI {
    using ColossalFramework.UI;
    using System;
    using UnityEngine;
    using KianCommons;
    using static KianCommons.HelpersExtensions;


    public class UIHideMarkingsCheckbox : UICheckBox, IDataControllerUI {
        public static UIHideMarkingsCheckbox Instance { get; private set; }

        public string HintHotkeys => throw new NotImplementedException();

        public string HintDescription =>
            "Removes crossing texture for all segment ends. " +
            "does not influence whether or not pedestrians can cross";


        UIPanelBase root_;
        public override void Awake() {
            base.Awake();
            Instance = this;
            name = nameof(UIHideMarkingsCheckbox);
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
            label.text = "No junction markings";
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
            if (VERBOSE) Log.Debug("UIHideMarkingsCheckbox.Apply called()\n" + Environment.StackTrace);
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
            data.ClearMarkings = this.isChecked;
            Assert(!refreshing_, "!refreshing_");
            data.Update();
            (root_ as IDataControllerUI).Refresh();

        }

        public void ApplySegmentEnd() {
            SegmentEndData data = (root_ as UISegmentEndControllerPanel).SegmentEndData;
            if (data == null)
                return;
            data.NoMarkings = this.isChecked;
            Log.Debug($"UIHideMarkingsCheckbox.ApplySegmentEnd(): {data}" +
                $"isChecked={isChecked} " +
                $"data.NoMarkings is set to {data.NoMarkings}");
            data.Update();
        }

        public void Refresh() {
            if (VERBOSE) Log.Debug("Refresh called()\n" + Environment.StackTrace);
            refreshing_ = true;

            RefreshValues();
            if (root_?.GetData() is NodeData nodeData)
                RefreshNode(nodeData);

            parent.isVisible = isVisible = this.isEnabled;
            Invalidate();
            refreshing_ = false;
        }

        public void RefreshNode(NodeData data) {
            if (data.HasUniformNoMarkings()) {
                checkedBoxObject.color = Color.white;
            } else {
                checkedBoxObject.color = Color.grey;
            }
        }

        public void RefreshValues() {
            INetworkData data = root_?.GetData();
            if (data is SegmentEndData segmentEndData) {
                isChecked = segmentEndData.NoMarkings;
                isEnabled = segmentEndData.ShowClearMarkingsToggle();
            } else if (data is NodeData nodeData) {
                this.isChecked = nodeData.ClearMarkings;
                isEnabled = nodeData.ShowClearMarkingsToggle();
            } else Disable();
        }
    }
}


