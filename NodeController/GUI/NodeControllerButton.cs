using ColossalFramework.UI;
using NodeController.Tool;
using NodeController.Util;
using System;
using UnityEngine;
using static NodeController.Util.HelpersExtensions;

/* A lot of copy-pasting from Crossings mod by Spectra and Roundabout Mod by Strad. The sprites are partly copied as well. */

namespace NodeController.GUI {
    public class NodeControllerButton : UIButton {
        public static string AtlasName = "NodeControllerButtonUI_rev" +
            typeof(NodeControllerButton).Assembly.GetName().Version.Revision;
        const int SIZE = 31;
        const string CONTAINING_PANEL_NAME = "RoadsOptionPanel";
        readonly static Vector2 RELATIVE_POSITION = new Vector3(94, 38);

        const string NodeControllerButtonBg = "NodeControllerButtonBg";
        const string NodeControllerButtonBgFocused = "NodeControllerButtonBgFocused";
        const string NodeControllerButtonBgHovered = "NodeControllerButtonBgHovered";
        internal const string NodeControllerIcon = "NodeControllerIcon";
        internal const string NodeControllerIconActive = "NodeControllerIconPressed";

        static UIComponent GetContainingPanel() {
            var ret = GUI.UIUtils.Instance.FindComponent<UIComponent>(CONTAINING_PANEL_NAME, null, GUI.UIUtils.FindOptions.NameContains);
            Log.Debug("GetPanel returns " + ret);
            return ret ?? throw new Exception("Could not find " + CONTAINING_PANEL_NAME);
        }

        public override void Start() {
            Log.Info("NodeControllerButton.Start() is called.");

            name = "NodeControllerButton";
            playAudioEvents = true;
            tooltip = "Node Controller";

            var builtinTabstrip = GUI.UIUtils.Instance.FindComponent<UITabstrip>("ToolMode", GetContainingPanel(), GUI.UIUtils.FindOptions.None);
            AssertNotNull(builtinTabstrip, "builtinTabstrip");

            UIButton tabButton = (UIButton)builtinTabstrip.tabs[0];

            string[] spriteNames = new string[]
            {
                NodeControllerButtonBg,
                NodeControllerButtonBgFocused,
                NodeControllerButtonBgHovered,
                NodeControllerIcon,
                NodeControllerIconActive
            };

            var atlas = TextureUtil.GetAtlas(AtlasName);
            if (atlas == UIView.GetAView().defaultAtlas) {
                atlas = TextureUtil.CreateTextureAtlas("sprites.png", AtlasName, tabButton.atlas.material, SIZE, SIZE, spriteNames);
            }

            Log.Debug("atlas name is: " + atlas.name);
            this.atlas = atlas;

            normalBgSprite = pressedBgSprite = disabledBgSprite = NodeControllerButtonBg;
            hoveredBgSprite  = NodeControllerButtonBgHovered;
            focusedBgSprite = NodeControllerButtonBgFocused;

            normalFgSprite = disabledFgSprite = hoveredFgSprite = pressedFgSprite = NodeControllerIcon;
            focusedFgSprite = NodeControllerIconActive;

            relativePosition = RELATIVE_POSITION;
            size = new Vector2(SIZE, SIZE); 
            Show();
            Log.Info("NodeControllerButton created sucessfully.");
        }

        public static UIButton CreateButton() { 
            Log.Info("NodeControllerButton.CreateButton() called");
            return GetContainingPanel().AddUIComponent<NodeControllerButton>();
        }

        protected override void OnClick(UIMouseEventParameter p) {
            base.OnClick(p);
            NodeControllerTool.Instance.ToggleTool();
        }

        public override void OnDestroy() {
            base.OnDestroy();
        }
    }
}
