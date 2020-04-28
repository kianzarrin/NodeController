namespace BlendRoadManager.GUI {
    using UnityEngine;
    using ColossalFramework.UI;
    using System.Collections.Generic;
    using System.Runtime.Serialization.Formatters.Binary;
    using BlendRoadManager.Util;
    using System.Runtime.Serialization.Formatters;
    using System.IO;
    using System.Linq;

    public class UINodeControllerPanel : UIPanel, IDataControllerUI {
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

        public NodeData BlendData {
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

            autoLayout = true;
            autoLayoutDirection = LayoutDirection.Vertical;
            autoLayoutPadding = new RectOffset(5, 5, 5, 5);
            //autoSize = true;

            name = "UINodeControllerPanel";
            atlas = TextureUtil.GetAtlas("Ingame");
            backgroundSprite = "SubcategoriesPanel";
            size = new Vector2(250, 100);
            absolutePosition = new Vector3(savedX_, savedY_);

            isVisible = false;


            //autoLayout = false;
            dragHandle_ = AddUIComponent<UIDragHandle>();
            dragHandle_.width = width;
            dragHandle_.height = 10;
            //dragHandle_.relativePosition = Vector3.zero;
            dragHandle_.target = parent;

            slider_ = AddUIComponent<UIOffsetSlider>();
            Controls.Add(slider_);
            dropdown_ = AddUIComponent<UINodeTypeDropDown>();
            Controls.Add(dropdown_);

        }

        UIOffsetSlider slider_;
        UINodeTypeDropDown dropdown_;
        UIDragHandle dragHandle_;

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

        //protected override void OnVisibilityChanged() {
        //    base.OnVisibilityChanged();
        //    if (isVisible)
        //        Refresh();
        //}

        public void ShowNode(ushort nodeID) {
            NodeManager.Instance.RefreshData(NodeID);
            NodeID = nodeID;
            Show();
            Refresh();
            dropdown_.Repopulate();
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
        }
    }
}
