namespace NodeController.GUI {
    using ColossalFramework.UI;
    using System.Globalization;
    using UnityEngine;
    using static Util.HelpersExtensions;

    public class UIOffsetTextField : UITextField, IDataControllerUI {
        public static UIOffsetTextField Instance { get; private set; }

        public override void Awake() {
            base.Awake();
            Instance = this;
            atlas = TextureUtil.GetAtlas("Ingame");
            size = new Vector2(50, 20);
            padding = new RectOffset(4, 4, 3, 3);
            builtinKeyNavigation = true;
            isInteractive = true;
            readOnly = false;
            horizontalAlignment = UIHorizontalAlignment.Center;
            selectionSprite = "EmptySprite";
            selectionBackgroundColor = new Color32(0, 172, 234, 255);
            normalBgSprite = "TextFieldPanelHovered";
            disabledBgSprite = "TextFieldPanelHovered";
            textColor = new Color32(0, 0, 0, 255);
            disabledTextColor = new Color32(80, 80, 80, 128);
            color = new Color32(255, 255, 255, 255);
            textScale = 0.9f;
            useDropShadow = true;
            text = "0";
        }

        public override void Start() {
            base.Start();
        }

        public bool TryGetValue(out float value) {
            return float.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture.NumberFormat, out value);
        }

        public float Value {
            set => text = value.ToString();
            get => float.Parse(text, CultureInfo.InvariantCulture.NumberFormat);
        }

        private string _prevText = "";

        protected override void OnTextChanged() {
            base.OnTextChanged();
            if (TryGetValue(out _)) {
                _prevText = text;
                Apply();
            } else {
                text = _prevText;
                Unfocus();
            }
        }

        //protected override void OnVisibilityChanged() {
        //    base.OnVisibilityChanged();
        //    if (isVisible)
        //        Refresh();
        //}

        public void Apply() {
            ushort nodeID = UINodeControllerPanel.Instance.NodeID;
            NodeData data = UINodeControllerPanel.Instance.NodeData;
            if (data == null)
                return;
            if (TryGetValue(out float value)) {
                data.CornerOffset = value;
                NetManager.instance.UpdateNode(nodeID);
            } else {
                Value = data.CornerOffset;
            }
        }

        public void Refresh() {
            NodeData data = UINodeControllerPanel.Instance.NodeData;
            if (data == null) {
                Disable();
                return;
            }
            Value = data.CornerOffset;
            isEnabled = data.CanModifyOffset();
        }

    }
}
