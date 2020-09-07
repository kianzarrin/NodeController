namespace NodeController.GUI {
    using System;
    using ColossalFramework.UI;
    using UnityEngine;
    using static KianCommons.HelpersExtensions;
    using KianCommons;
    using KianCommons.UI;
    using System.Reflection;
    using HarmonyLib;

    public class NodeTypeDropDown : UIDropDown, IDataControllerUI {
        bool awakened_ = false;

        public override void Awake() {
            base.Awake();
            atlas = TextureUtil.GetAtlas("Ingame");
            size = new Vector2(120f, 30);
            listBackground = "GenericPanelLight";
            itemHeight = 25;
            itemHover = "ListItemHover";
            itemHighlight = "ListItemHighlight";
            normalBgSprite = "ButtonMenu";
            disabledBgSprite = "ButtonMenuDisabled";
            hoveredBgSprite = "ButtonMenuHovered";
            focusedBgSprite = "ButtonMenu";
            listWidth = 120;
            listHeight = 700;
            listPosition = UIDropDown.PopupListPosition.Below;
            clampListToScreen = true;
            builtinKeyNavigation = true;
            foregroundSpriteMode = UIForegroundSpriteMode.Stretch;
            popupColor = new Color32(45, 52, 61, 255);
            popupTextColor = new Color32(170, 170, 170, 255);
            zOrder = 1;
            textScale = 0.8f;
            verticalAlignment = UIVerticalAlignment.Middle;
            horizontalAlignment = UIHorizontalAlignment.Left;
            selectedIndex = 0;
            textFieldPadding = new RectOffset(8, 0, 8, 0);
            itemPadding = new RectOffset(14, 0, 8, 0);
            //AlignTo(parent, UIAlignAnchor.TopLeft);

            var button = AddUIComponent<UIButton>();
            triggerButton = button;
            button.atlas = TextureUtil.GetAtlas("Ingame");
            button.text = "";
            button.size = size;
            button.relativePosition = new Vector3(0f, 0f);
            button.textVerticalAlignment = UIVerticalAlignment.Middle;
            button.textHorizontalAlignment = UIHorizontalAlignment.Left;
            button.normalFgSprite = "IconDownArrow";
            button.hoveredFgSprite = "IconDownArrowHovered";
            button.pressedFgSprite = "IconDownArrowPressed";
            button.focusedFgSprite = "IconDownArrowFocused";
            button.disabledFgSprite = "IconDownArrowDisabled";
            button.foregroundSpriteMode = UIForegroundSpriteMode.Fill;
            button.horizontalAlignment = UIHorizontalAlignment.Right;
            button.verticalAlignment = UIVerticalAlignment.Middle;
            button.zOrder = 0;
            button.textScale = 0.8f;

            foreach (var item in Enum.GetNames(typeof(NodeTypeT))) {
                AddItem(item);
            }
            listWidth = (int)width;
            awakened_ = true;
        }

        protected override void OnSizeChanged() {
            base.OnSizeChanged();
            if (!awakened_) return;
            triggerButton.size = size;
            listWidth = (int)width;
        }

        UINodeControllerPanel root_;
        public override void Start() {
            base.Start();
            root_ = GetRootContainer() as UINodeControllerPanel;

        }

        public NodeTypeT SelectedItem {
            get => (NodeTypeT)String2Enum<NodeTypeT>(selectedValue);
            set => selectedValue = value.ToString();
        }

        FieldInfo fPopop = AccessTools.DeclaredField(typeof(UIDropDown), "m_Popup")
            ?? throw new Exception("m_Popup not found");

        public UIListBox Popup => fPopop.GetValue(this) as UIListBox;

        FieldInfo fHoverIndex = AccessTools.DeclaredField(typeof(UIListBox), "m_HoverIndex")
            ?? throw new Exception("m_HoverIndex not found");

        int GetHoverIndex() {
            Log.Debug("GetHoverIndex() popup=" + Popup);
            if (!Popup.isVisible)
                return -1;
            return (int)fHoverIndex.GetValue(Popup);
        }

        public NodeTypeT GetHoveredItem() {
            Log.Debug("GetHoveredItem() called" );
            int index = GetHoverIndex();
            Log.Debug("index = " + index);
            if (index == -1)
                return SelectedItem;
            string item = items[index];
            return (NodeTypeT)String2Enum<NodeTypeT>(item);
        }


        public string HintHotkeys => null;

        public string HintDescription {
            get {
                var data = root_?.GetData() as NodeData;
                var nodeType = GetHoveredItem();
                return data?.ToolTip(nodeType);
            }
        }

        protected override void OnSelectedIndexChanged() {
            base.OnSelectedIndexChanged();
            if (refreshing_)
                return;
            Apply();
        }

        public void Apply() {
            if (VERBOSE) Log.Debug("NodeTypeDropDown.Apply called()\n" + Environment.StackTrace);
            NodeData data = root_.NodeData;
            if (data == null) return;
            data.NodeType = SelectedItem;
            foreach (var segmentEndData in data.IterateSegmentEndDatas())
                segmentEndData.DeltaSlopeAngleDeg = 0;
            Assert(!refreshing_, "!refreshing_");
            data.RefreshAndUpdate();
            root_.Refresh();
        }

        // protection against unncessary apply/refresh/infinite recursion.
        bool refreshing_ = false;

        public void Repopulate() {
            if (VERBOSE) Log.Debug("NodeTypeDropDown.Repopulate called()" + Environment.StackTrace);
            NodeData data = root_.NodeData;
            items = null;
            foreach (NodeTypeT nodeType in Enum.GetValues(typeof(NodeTypeT))) {
                if (data.CanChangeTo(nodeType)/*.LogRet("CanChangeTo()->")*/) {
                    AddItem(nodeType.ToString());
                }
            }
        }

        public void Refresh() {
            if (VERBOSE) Log.Debug("Refresh called()\n" + Environment.StackTrace);
            refreshing_ = true;
            NodeData data = root_?.NodeData;
            if (data == null) {
                Disable();
                return;
            }

            Repopulate();
            SelectedItem = data.NodeType;

            parent.isVisible = isVisible = triggerButton.isEnabled = this.isEnabled = items.Length > 1;
            parent.Invalidate();
            Invalidate();
            triggerButton.Invalidate();
            refreshing_ = false;
        }

        public void RefreshValues() {
            refreshing_ = true;
            NodeData data = root_?.NodeData;
            if (data != null)
                SelectedItem = data.NodeType;
            refreshing_ = false;
        }
    }
}
