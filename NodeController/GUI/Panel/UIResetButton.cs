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
            if (root_.NetworkType == NetworkTypeT.Node)
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
            var data = root_?.GetData();
            data?.ResetToDefault(); // also calls RefreshAndUpdate()
            Assert(!refreshing_, "!refreshing_");
            root_.Refresh();
        }

        // protection against unncessary apply/refresh/infinite recursion.
        bool refreshing_ = false;

        public void Refresh() {
            if (VERBOSE) Log.Debug("Refresh called()\n" + Environment.StackTrace);
            refreshing_ = true;
            RefreshValues();

            parent.isVisible = isVisible = true;
            Invalidate();
            refreshing_ = false;
        }

        public void RefreshValues() {
            INetworkData data = root_.GetData();
            if (data == null) {
                Disable();
                return;
            }
            isEnabled = !data.IsDefault();
        }
    }
}


