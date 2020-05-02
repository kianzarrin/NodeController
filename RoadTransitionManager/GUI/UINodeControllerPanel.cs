namespace RoadTransitionManager.GUI {
    using UnityEngine;
    using ColossalFramework.UI;
    using System.Collections.Generic;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Runtime.Serialization.Formatters;
    using System.IO;
    using System.Linq;
    using Util;
    public class UINodeControllerPanel : UIAutoSizePanel, IDataControllerUI {
        #region Instanciation
        public static UINodeControllerPanel Instance { get; private set; }
        static float savedX_ = 87;
        static float savedY_ = 58;

        static BinaryFormatter GetBinaryFormatter =>
            new BinaryFormatter { AssemblyFormat = FormatterAssemblyStyle.Simple };

        public static void Deserialize(byte[] data) {
            if (data == null) {
                Instance = new UINodeControllerPanel();
                Log.Debug($"UINodeControllerPanel.Deserialize(data=null)");
                return;
            }
            Log.Debug($"UINodeControllerPanel.Deserialize (data): data.Length={data?.Length}");

            var memoryStream = new MemoryStream();
            memoryStream.Write(data, 0, data.Length);
            memoryStream.Position = 0;
            var formatter = GetBinaryFormatter;
            savedX_ = (float)formatter.Deserialize(memoryStream);
            savedY_ = (float)formatter.Deserialize(memoryStream);
        }

        public static byte[] Serialize() {
            var memoryStream = new MemoryStream();
            var formatter = GetBinaryFormatter;
            formatter.Serialize(memoryStream, savedX_);
            formatter.Serialize(memoryStream, savedY_);
            memoryStream.Position = 0; // redundant
            return memoryStream.ToArray();
        }

        public static UINodeControllerPanel Create() {
            var uiView = UIView.GetAView();
            UINodeControllerPanel panel = uiView.AddUIComponent(typeof(UINodeControllerPanel)) as UINodeControllerPanel;
            return panel;
        }

        public static void Release() {
            var uiView = UIView.GetAView();
            var panel = (UINodeControllerPanel)uiView.FindUIComponent<UINodeControllerPanel>("UINodeControllerPanel");
            Destroy(panel);
        }
        #endregion Instanciation

        public ushort NodeID { get; private set; }

        public NodeData NodeData {
            get {
                if (NodeID == 0) return null;
                return NodeManager.Instance.GetOrCreate(NodeID);
            }
        }

        public List<IDataControllerUI> Controls;

        public override void Awake() {
            base.Awake();
            Instance = this;
            Controls = new List<IDataControllerUI>();
        }

        public override void Start() {
            base.Start();
            Log.Debug("UINodeControllerPanel started");

            width = 250;
            name = "UINodeControllerPanel";
            backgroundSprite = "MenuPanel2";
            absolutePosition = new Vector3(savedX_, savedY_);

            isVisible = false;

            {
                var dragHandle_ = AddUIComponent<UIDragHandle>();
                dragHandle_.width = width;
                dragHandle_.height = 42;
                dragHandle_.relativePosition = Vector3.zero;
                dragHandle_.target = parent;

                var lblCaption = dragHandle_.AddUIComponent<UILabel>();
                lblCaption.text = "Node controler";
                lblCaption.relativePosition = new Vector3(70, 14, 0);

                var sprite = dragHandle_.AddUIComponent<UISprite>();
                sprite.size = new Vector2(40, 40);
                sprite.relativePosition = new Vector3(5, 3, 0);
                sprite.atlas = TextureUtil.GetAtlas(RoadTransitionButton.AtlasName);
                sprite.spriteName = RoadTransitionButton.RoadTransitionIconActive;


            }

            {
                var panel = AddPanel();
                var label = panel.AddUIComponent<UILabel>();
                label.text = "Choose transition type";

                var dropdown_ = panel.AddUIComponent<UINodeTypeDropDown>();
                Controls.Add(dropdown_);
            }

            {
                var panel = AddPanel();

                var label = panel.AddUIComponent<UILabel>();
                label.text = "Corner smoothness";
                label.tooltip = "Adjusts Corner offset for smooth junction transition.";

                var slider_ = panel.AddUIComponent<UIOffsetSlider>();
                Controls.Add(slider_);
            }

            AddPanel().name = "Space";

            {
                var panel = AddPanel();
                var checkBox = panel.AddUIComponent<UIHideMarkingsCheckbox>();
                Controls.Add(checkBox);
            }

            {
                var panel = AddPanel();
                var button = panel.AddUIComponent<UIResetButton>();
                Controls.Add(button);
            }
        }

        UIAutoSizePanel AddPanel() {
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

            savedX_ = absolutePosition.x;
            savedY_ = absolutePosition.y;
            Log.Debug("absolutePosition: " + absolutePosition);
        }

        public void ShowNode(ushort nodeID) {
            NodeManager.Instance.RefreshData(NodeID);
            NodeID = nodeID;
            Show();
            Refresh();
        }

        public void Close() {
            NodeManager.Instance.RefreshData(NodeID);
            NodeID = 0;
            Hide();
        }

        public void Apply() {
            foreach(IDataControllerUI control in Controls ?? Enumerable.Empty<IDataControllerUI>())
                control.Apply();
        }

        public void Refresh() {
            foreach (IDataControllerUI control in Controls ?? Enumerable.Empty<IDataControllerUI>())
                control.Refresh();
            //Update();
            RefreshSizeRecursive();
        }
    }
}
