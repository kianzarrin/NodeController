namespace NodeController.GUI {
    using ColossalFramework.UI;
    using System;
    using UnityEngine;
    using Util;
    using static Util.HelpersExtensions;

    public class UIResetButton : UIButton, IDataControllerUI {
        public static UIResetButton Instance { get; private set; }

        public override void Awake() {
            base.Awake();
            Instance = this;
            name = nameof(UIResetButton);

            height = 30f;
            textScale = 0.9f;
            normalBgSprite = "ButtonMenu";
            hoveredBgSprite = "ButtonMenuHovered";
            pressedBgSprite = "ButtonMenuPressed";
            disabledBgSprite = "ButtonMenuDisabled";
            disabledTextColor = new Color32(128, 128, 128, 255);
            canFocus = false;

            text = "Reset to default";
            tooltip = "Clears all customization.";
        }

        public override void Start() {
            base.Start();
            width = parent.width;
        }

        protected override void OnClick(UIMouseEventParameter p) {
            base.OnClick(p);
            if (refreshing_)
                return;
            Apply();
        }

        public void Apply() {
            if (VERBOSE) Log.Debug("UIResetButton.Apply called()\n" + Environment.StackTrace);
            UINodeControllerPanel.Instance.NodeData?.ResetToDefault(); ;
            Assert(!refreshing_, "!refreshing_");
            UINodeControllerPanel.Instance.Refresh();
        }

        // protection against unncessary apply/refresh/infinite recursion.
        bool refreshing_ = false;

        public void Refresh() {
            if (VERBOSE) Log.Debug("Refresh called()\n" + Environment.StackTrace);
            refreshing_ = true;
            NodeData data = UINodeControllerPanel.Instance.NodeData;
            if (data == null) {
                Disable();
                return;
            }

            parent.isVisible = isVisible = true;
            isEnabled = !data.IsDefault();
            parent.Invalidate();
            Invalidate();
            refreshing_ = false;


        }
    }
}


