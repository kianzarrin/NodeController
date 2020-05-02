using ColossalFramework.UI;
using RoadTransitionManager.Tool;
using RoadTransitionManager.Util;
using System;
using UnityEngine;
using static RoadTransitionManager.Util.HelpersExtensions;

/* A lot of copy-pasting from Crossings mod by Spectra and Roundabout Mod by Strad. The sprites are partly copied as well. */

namespace RoadTransitionManager.GUI {
    public class RoadTransitionButton : UIButton {
        public static string AtlasName = "RoadTransitionButtonUI_rev" +
            typeof(RoadTransitionButton).Assembly.GetName().Version.Revision;
        const int SIZE = 40;
        const string CONTAINING_PANEL_NAME = "RoadsOptionPanel";
        readonly static Vector2 RELATIVE_POSITION = new Vector3(94, 38);


        const string RoadTransitionButtonBg = "RoadTransitionButtonBg";
        const string RoadTransitionButtonBgFocused = "RoadTransitionButtonBgFocused";
        const string RoadTransitionButtonBgHovered = "RoadTransitionButtonBgHovered";
        internal const string RoadTransitionIcon = "RoadTransitionIcon";
        internal const string RoadTransitionIconActive = "RoadTransitionIconPressed";

        static UIComponent GetContainingPanel() {
            var ret = GUI.UIUtils.Instance.FindComponent<UIComponent>(CONTAINING_PANEL_NAME, null, GUI.UIUtils.FindOptions.NameContains);
            Log.Debug("GetPanel returns " + ret);
            return ret ?? throw new Exception("Could not find " + CONTAINING_PANEL_NAME);
        }

        public override void Start() {
            Log.Info("RoadTransitionButton.Start() is called.");

            name = "RoadTransitionButton";
            playAudioEvents = true;
            tooltip = "Road Transition Manager";

            var builtinTabstrip = GUI.UIUtils.Instance.FindComponent<UITabstrip>("ToolMode", GetContainingPanel(), GUI.UIUtils.FindOptions.None);
            AssertNotNull(builtinTabstrip, "builtinTabstrip");

            UIButton tabButton = (UIButton)builtinTabstrip.tabs[0];

            string[] spriteNames = new string[]
            {
                RoadTransitionButtonBg,
                RoadTransitionButtonBgFocused,
                RoadTransitionButtonBgHovered,
                RoadTransitionIcon,
                RoadTransitionIconActive
            };

            var atlas = TextureUtil.GetAtlas(AtlasName);
            if (atlas == UIView.GetAView().defaultAtlas) {
                atlas = TextureUtil.CreateTextureAtlas("sprites.png", AtlasName, tabButton.atlas.material, SIZE, SIZE, spriteNames);
            }

            Log.Debug("atlas name is: " + atlas.name);
            this.atlas = atlas;

            normalBgSprite = pressedBgSprite = disabledBgSprite = RoadTransitionButtonBg;
            hoveredBgSprite  = RoadTransitionButtonBgHovered;
            focusedBgSprite = RoadTransitionButtonBgFocused;

            normalFgSprite = disabledFgSprite = hoveredFgSprite = pressedFgSprite = RoadTransitionIcon;
            focusedFgSprite = RoadTransitionIconActive;

            relativePosition = RELATIVE_POSITION;
            size = new Vector2(SIZE, SIZE); 
            Show();
            Log.Info("RoadTransitionButton created sucessfully.");
        }

        public static UIButton CreateButton() { 
            Log.Info("RoadTransitionButton.CreateButton() called");
            return GetContainingPanel().AddUIComponent<RoadTransitionButton>();
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
