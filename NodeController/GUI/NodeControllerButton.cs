using ColossalFramework.UI;
using NodeController.Tool;
using NodeController.Util;
using System;
using System.Linq;
using UnityEngine;
using static NodeController.Util.HelpersExtensions;

/* A lot of copy-pasting from Crossings mod by Spectra and Roundabout Mod by Strad. The sprites are partly copied as well. */

namespace NodeController.GUI {
    public class NodeControllerButton : UIButton {
        public static NodeControllerButton Instace { get; private set;}

        public static string AtlasName = "NodeControllerButtonUI_rev" +
            typeof(NodeControllerButton).Assembly.GetName().Version.Revision;
        const int SIZE = 31;
        const string CONTAINING_PANEL_NAME = "RoadsOptionPanel";
        readonly static Vector2 RELATIVE_POSITION = new Vector3(94, 38);

        const string NodeControllerButtonBg = "NodeControllerButtonBg";
        const string NodeControllerButtonBgActive = "NodeControllerButtonBgFocused";
        const string NodeControllerButtonBgHovered = "NodeControllerButtonBgHovered";
        internal const string NodeControllerIcon = "NodeControllerIcon";
        internal const string NodeControllerIconActive = "NodeControllerIconPressed";

        static UIComponent GetContainingPanel() {
            var ret = GUI.UIUtils.Instance.FindComponent<UIComponent>(CONTAINING_PANEL_NAME, null, GUI.UIUtils.FindOptions.NameContains);
            Log.Debug("GetPanel returns " + ret);
            return ret ?? throw new Exception("Could not find " + CONTAINING_PANEL_NAME);
        }

        public override void Awake() {
            base.Awake();
            Log.Debug("NodeControllerButton.Awake() is called." + Environment.StackTrace);
        }

        public override void Start() {
            base.Start();
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
                NodeControllerButtonBgActive,
                NodeControllerButtonBgHovered,
                NodeControllerIcon,
                NodeControllerIconActive
            };

            var atlas = TextureUtil.GetAtlas(AtlasName);
            if (atlas == UIView.GetAView().defaultAtlas) {
                atlas = TextureUtil.CreateTextureAtlas("sprites.png", AtlasName, SIZE, SIZE, spriteNames);
            }

            Log.Debug("atlas name is: " + atlas.name);
            this.atlas = atlas;

            Deactivate();
            hoveredBgSprite = NodeControllerButtonBgHovered;


            relativePosition = RELATIVE_POSITION;
            size = new Vector2(SIZE, SIZE); 
            Show();
            Log.Info("NodeControllerButton created sucessfully.");
            Unfocus();
            Invalidate();
            //if (parent.name == "RoadsOptionPanel(RoadOptions)") {
            //    Destroy(Instace); // destroy old instance after cloning
            //}
            Instace = this;
        }

        public void Activate() {
            focusedFgSprite = normalBgSprite = pressedBgSprite = disabledBgSprite = NodeControllerButtonBgActive;
            normalFgSprite = focusedFgSprite = NodeControllerIconActive;
            Invalidate();
        }

        public void Deactivate() {
            focusedFgSprite = normalBgSprite = pressedBgSprite = disabledBgSprite = NodeControllerButtonBg;
            normalFgSprite = focusedFgSprite = NodeControllerIcon;
            Invalidate();
        }


        public static NodeControllerButton CreateButton() { 
            Log.Info("NodeControllerButton.CreateButton() called");
            return GetContainingPanel().AddUIComponent<NodeControllerButton>();
        }

        protected override void OnClick(UIMouseEventParameter p) {
            Log.Debug("ON CLICK CALLED" + Environment.StackTrace);
            var buttons = UIUtils.GetCompenentsWithName<UIComponent>(name);
            Log.Debug(buttons.ToSTR());

            base.OnClick(p); 
            NodeControllerTool.Instance.ToggleTool();
        }

        public override void OnDestroy() {
            base.OnDestroy();
        }

        public override string ToString() => $"NodeControllerButton:|name={name} parent={parent.name}|";


    }
}
