namespace NodeController.GUI {
    using ColossalFramework;
    using ColossalFramework.UI;
    using KianCommons.UI;
    using System.Collections;
    using UnityEngine;

    public class CollapsorButton : UIButton {
        private static SavedBool showAdvanced_ = new SavedBool("ShowAdvanced", NCSettings.FileName, false, true);
        private UIPanel targetPanel_;
                
        public override void Awake() {
            base.Awake();
            name = GetType().FullName;
            autoSize = true;
            canFocus = false;

            normalBgSprite = "";
            hoveredBgSprite = "";
            pressedBgSprite = "";
            disabledBgSprite = "";
            atlas = TextureUtil.Ingame;

            text = "                             ▼ ▼ ▼ Advanced ▼ ▼ ▼";
            height = 30f;
            textScale = 0.9f;
            m_TextVerticalAlign = UIVerticalAlignment.Bottom;
            horizontalAlignment = UIHorizontalAlignment.Center;
            textPadding = new RectOffset(5, 5, 5, 5);
            textColor = Color.white;
            hoveredTextColor = Color.Lerp(Color.blue, Color.white, .5f);
            pressedTextColor = Color.Lerp(Color.green, Color.white, 0);
            disabledTextColor = Color.Lerp(Color.black, Color.white, .5f);
        }

        public void SetTarget(UIPanel panel) {
            targetPanel_ = panel;
            if (!showAdvanced_.value) {
                targetPanel_.Hide();
            }
        }
        public void Collapse() {
            targetPanel_.Hide();
            text = text.Replace("▲", "▼");
            GetComponentInParent<UIPanelBase>().Refresh();
            showAdvanced_.value = false;
        }

        public void Open() {
            targetPanel_.Show();
            text = text.Replace("▼", "▲");
            GetComponentInParent<UIPanelBase>().Refresh();
            showAdvanced_.value = true;
        }

        protected override void OnClick(UIMouseEventParameter p) {
            base.OnClick(p);
            if (targetPanel_.isVisible) {
                Collapse();
            } else {
                Open();
            }
        }
    }
}
