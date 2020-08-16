namespace NodeController.GUI {
    using ColossalFramework.UI;
    using System;
    using UnityEngine;
    using static KianCommons.HelpersExtensions;
    using KianCommons;

    public class UIResetButton : UIButton, IDataControllerUI {
        public static UIResetButton Instance { get; private set; }

        UIPanelBase root_;
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
            root_ = GetRootContainer() as UIPanelBase;
            if (root_ is UINodeControllerPanel ncpanel)
                tooltip += "including segment ends.";

        }

        protected override void OnClick(UIMouseEventParameter p) {
            base.OnClick(p);
            if (refreshing_)
                return;
            Apply();
        }

        public void Apply() {
            if (VERBOSE) Log.Debug("UIResetButton.Apply called()\n" + Environment.StackTrace);
            if (root_ is UINodeControllerPanel ncpanel)
                ncpanel.NodeData?.ResetToDefault(); 
            else if (root_ is UISegmentEndControllerPanel secpanel)
                secpanel.SegmentEndData?.ResetToDefault();

            Assert(!refreshing_, "!refreshing_");
            root_.Refresh();
        }

        // protection against unncessary apply/refresh/infinite recursion.
        bool refreshing_ = false;

        public void Refresh() {
            if (VERBOSE) Log.Debug("Refresh called()\n" + Environment.StackTrace);
            refreshing_ = true;
            if (root_ is UINodeControllerPanel ncpanel)
                RefreshNode();
            else if (root_ is UISegmentEndControllerPanel secpanel)
                RefreshSegmentEnd();

            parent.isVisible = isVisible = true;
            Invalidate();
            refreshing_ = false;
        }
        public void RefreshNode() {
            NodeData data = (root_ as UINodeControllerPanel).NodeData;
            if (data == null)
                Disable();
            else
                isEnabled = !data.IsDefault();
        }
        public void RefreshSegmentEnd() {
            SegmentEndData data = (root_ as UISegmentEndControllerPanel).SegmentEndData;
            if (data == null)
                Disable();
            else
                isEnabled = !data.IsDefault();
        }

    }
}


