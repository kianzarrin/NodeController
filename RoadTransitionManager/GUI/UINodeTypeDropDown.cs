namespace NodeController.GUI {
    using System;
    using ColossalFramework.UI;
    using UnityEngine;
    using static Util.HelpersExtensions;
    using System.Linq;
    using Util;

    public class UINodeTypeDropDown : UIDropDown, IDataControllerUI{
        public static UINodeTypeDropDown Instance { get; private set; }

        public override void Awake() {
            base.Awake();
            Instance = this;
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

            eventSizeChanged += new PropertyChangedEventHandler<Vector2>((c, t) => {
                button.size = t;
                listWidth = (int)t.x; // TODO put in override.
            });

            foreach (var item in Enum.GetNames(typeof(NodeTypeT))) {
                AddItem(item);
            }
        }

        public override void Start() {
            base.Start();
        }

        public NodeTypeT SelectedItem {
            get => (NodeTypeT)String2Enum<NodeTypeT>(selectedValue);
            set => selectedValue = value.ToString();
        }

        protected override void OnSelectedIndexChanged() {
            base.OnSelectedIndexChanged();
            if (refreshing_)
                return;
            Apply();
        }

        public void Apply() {
            if(VERBOSE)Log.Debug("UINodeTypeDropDown.Apply called()\n" + Environment.StackTrace);
            NodeData data = UINodeControllerPanel.Instance.NodeData;
            if (data == null) return;
            data.NodeType = SelectedItem;
            Assert(!refreshing_, "!refreshing_");
            data.Refresh();
            UINodeControllerPanel.Instance.Refresh();
        }

        // protection against unncessary apply/refresh/infinite recursion.
        bool refreshing_ = false;

        public void Repopulate() {
            if (VERBOSE) Log.Debug("UINodeTypeDropDown.Repopulate called()" + Environment.StackTrace);
            NodeData data = UINodeControllerPanel.Instance.NodeData;
            items = null;
            foreach (NodeTypeT nodeType in Enum.GetValues(typeof(NodeTypeT))) {
                if (data.CanChangeTo(nodeType)) {
                    AddItem(nodeType.ToString());
                }
            }
        }

        public void Refresh() {
            if (VERBOSE) Log.Debug("Refresh called()\n" + Environment.StackTrace);
            refreshing_ = true;
            NodeData data = UINodeControllerPanel.Instance.NodeData;
            if (data == null) {
                Disable();
                return;
            }

            Repopulate();
            SelectedItem = data.NodeType;
            tooltip = data.ToolTip(data.NodeType);

            parent.isVisible = isVisible = triggerButton.isEnabled = this.isEnabled = items.Length > 1;
            parent.Invalidate();
            Invalidate();
            triggerButton.Invalidate();
            refreshing_ = false;
        }
    }
}
