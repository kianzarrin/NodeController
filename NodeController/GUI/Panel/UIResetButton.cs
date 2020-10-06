namespace NodeController.GUI {
    using ColossalFramework.UI;
    using System;
    using UnityEngine;
    using static KianCommons.HelpersExtensions;
    using static KianCommons.Assertion;
    using KianCommons;
    using KianCommons.UI;

    public class UIResetButton : UIButton, IDataControllerUI {
        public static UIResetButton Instance { get; private set; }
        public string HintHotkeys => "del => reset all to default";

        public string HintDescription {
            get {
                string ret = "Clears all customization. ";
                if (root_.NetworkType == NetworkTypeT.Node)
                    ret += "including segment ends.";
                return ret;
            }
        }

        UIPanelBase root_;
        public override void Awake() {
            base.Awake();
            Instance = this;
            name = nameof(UIResetButton);

            height = 30f;
            width = 200;

            textScale = 0.9f;
            normalBgSprite = "ButtonMenu";
            hoveredBgSprite = "ButtonMenuHovered";
            pressedBgSprite = "ButtonMenuPressed";
            disabledBgSprite = "ButtonMenuDisabled";
            disabledTextColor = new Color32(128, 128, 128, 255);
            canFocus = false;
            atlas = TextureUtil.Ingame;

            text = "Reset to default";
        }

        public override void Start() {
            base.Start();
            root_ = GetRootContainer() as UIPanelBase;
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
            RefreshValues();
            refreshing_ = true;

            parent.isVisible = isVisible = true;
            Invalidate();
            refreshing_ = false;
        }

        public void RefreshValues() {
            refreshing_ = true;
            INetworkData data = root_.GetData();
            if (data == null) {
                Disable();
                return;
            }
            isEnabled = !data.IsDefault();
            refreshing_ = false;
        }

        public void Reset() {
            Apply();
        }
    }
}


