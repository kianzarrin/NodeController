namespace NodeController.GUI {
    using ColossalFramework.UI;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using ColossalFramework;
    using KianCommons;
    using KianCommons.UI;
    using System.Reflection.Emit;

    public abstract class UIPanelBase : UIAutoSizePanel, IDataControllerUI {
        public static readonly SavedFloat SavedX = new SavedFloat(
            "PanelX", Settings.FileName, 87, true);
        public static readonly SavedFloat SavedY = new SavedFloat(
            "PanelY", Settings.FileName, 58, true);

        public List<IDataControllerUI> Controls;
        public UIResetButton ResetButton;

        public string Caption {
            get => lblCaption_.text;
            set => lblCaption_.text = value;
        }
        UILabel lblCaption_;
        UISprite sprite_;
        UIDragHandle dragHandle_;

        public abstract NetworkTypeT NetworkType { get; }

        /// <summary>
        /// if data id is 0, returns null. otherwise calls *Manager.GetORCreateData(...)
        /// </summary>
        /// <returns></returns>
        public abstract INetworkData GetData();

        public override void Awake() {
            base.Awake();
            Controls = new List<IDataControllerUI>();
        }

        public override void Start() {
            base.Start();
            Log.Debug("UIPanelBase started");

            width = 250;
            name = "UIPanelBase";
            backgroundSprite = "MenuPanel2";
            absolutePosition = new Vector3(SavedX, SavedY);

            isVisible = false;

            {
                dragHandle_ = AddUIComponent<UIDragHandle>();
                dragHandle_.width = width;
                dragHandle_.height = 42;
                dragHandle_.relativePosition = Vector3.zero;
                dragHandle_.target = parent;

                lblCaption_ = dragHandle_.AddUIComponent<UILabel>();
                lblCaption_.text = "network controller";
                lblCaption_.relativePosition = new Vector2(70, 14);

                sprite_ = dragHandle_.AddUIComponent<UISprite>();
                sprite_.size = new Vector2(40, 40);
                sprite_.relativePosition = new Vector2(5, 3);
                sprite_.atlas = TextureUtil.GetAtlas(NodeControllerButton.AtlasName);
                sprite_.spriteName = NodeControllerButton.NodeControllerIconActive;
            }
            Disable();
        }

        protected virtual UIAutoSizePanel AddPanel() {
            int pad_horizontal = 10;
            int pad_vertical = 5;
            UIAutoSizePanel panel = AddUIComponent<UIAutoSizePanel>();
            panel.width = width - pad_horizontal * 2;
            panel.autoLayoutPadding =
                new RectOffset(pad_horizontal, pad_horizontal, pad_vertical, pad_vertical);
            return panel;
        }

        protected override void OnPositionChanged() {
            base.OnPositionChanged();
            Log.Debug("OnPositionChanged called");

            Vector2 resolution = GetUIView().GetScreenResolution();

            absolutePosition = new Vector2(
                Mathf.Clamp(absolutePosition.x, 0, resolution.x - width),
                Mathf.Clamp(absolutePosition.y, 0, resolution.y - height));

            SavedX.value = absolutePosition.x;
            SavedY.value = absolutePosition.y;
            Log.Debug("absolutePosition: " + absolutePosition);
        }

        public void Apply() {
            foreach (IDataControllerUI control in Controls ?? Enumerable.Empty<IDataControllerUI>())
                control.Apply();
        }

        public virtual void Refresh() {
            Unfocus();
            autoLayout = true;
            foreach (IDataControllerUI control in Controls ?? Enumerable.Empty<IDataControllerUI>())
                control.Refresh();
            //Update();
            RefreshSizeRecursive();
            Invalidate();
            autoLayout = false;

            // calcualte captions position:
            float spriteEndX = sprite_.relativePosition.x + sprite_.width;
            float x = spriteEndX + 0.5f * (dragHandle_.width - spriteEndX - lblCaption_.width);
            lblCaption_.relativePosition = new Vector2(x, 14);
        }

        public virtual void RefreshValues() {
            foreach (IDataControllerUI control in Controls ?? Enumerable.Empty<IDataControllerUI>()) {
                control?.RefreshValues();
            }
        }

        public UILabel Hintbox;

        public void MakeHintBox() {
            var panel = AddUIComponent<UIPanel>();
            panel.width = width;
            panel.height = 0;
            Hintbox = panel.AddUIComponent<UILabel>();
            Hintbox.relativePosition = Vector2.zero;
            Hintbox.size = new Vector2(width, 10);
            Hintbox.minimumSize = new Vector2(width, 0);
            Hintbox.maximumSize = new Vector2(width, 100);
            Hintbox.wordWrap = true;
        }

    }
}
