namespace NodeController.GUI {
    using ColossalFramework.UI;
    using ColossalFramework;
    using KianCommons;
    using UnityEngine;
    using System.Collections.Generic;
    using System;
    using NodeController.Tool;

    public class HintBox : UILabel {
        UIPanelBase root_;
        IEnumerable<IDataControllerUI> controlls_ => root_?.Controls;
        NodeControllerTool tool_;
        NodeControllerTool Tool => tool_ = tool_ ?? NodeControllerTool.Instance;

        public override void Awake() {
            base.Awake();
            this.relativePosition = Vector2.zero;
            this.wordWrap = true;
            byte intensity = 32;
            this.color = new Color32(intensity, intensity, intensity, 190);

            this.textColor = Color.white;
            textScale = 0.8f;
            padding = new RectOffset(5, 5, 5, 5);
        }

        public new float width {
            get => base.width;
            set {
                base.width = value;
                this.minimumSize = new Vector2(value, 0);
                this.maximumSize = new Vector2(value, height);
            }
        }

        public new float height {
            get => base.height;
            set {
                base.height = value;
                this.maximumSize = new Vector2(width, value);
            }
        }

        public new Vector2 size {
            get => base.size;
            set {
                base.size = value;
                this.minimumSize = new Vector2(width, 0);
                this.maximumSize = size;
            }
        }

        public override void Start() {
            base.Start();
            this.relativePosition = new Vector2(0, 1f);
            this.backgroundSprite = "GenericPanel";
            this.size = new Vector2(parent.width, 200);
            this.autoSize = true;

            root_ = GetRootContainer() as UIPanelBase;
            Log.Debug($"size={size} minsize={minimumSize} maxsize={maximumSize}");
            Invalidate();
        }

        public string hint1_, hint2_, hint3_;

        /// <summary>
        /// Controller hotkeys
        /// </summary>
        public string Hint1;

        // Controller description
        public string Hint2;

        // tool 
        public string Hint3;


        public override void Update() {
            base.Update();
            try {
                string rootname = root_?.GetType()?.Name ?? "null";
                //var version = this?.VersionOf()?.ToString() ?? "null";
                //string id = $"{rootname} V{this.VersionOf()}";
                //Log.DebugWait($"HintBox.Update() called", id);

                if (root_ == null || !root_.isVisible)
                    return;
                if (containsMouse)
                    return; // prevent flickering on mouse hover

                string h1 = null, h2 = null;
                foreach (IDataControllerUI c in controlls_) {
                    bool hovered = (c as UIComponent).containsMouse;
                    if (hovered) {
                        string m = $"{(c as UIComponent).name}-{c}@{rootname}";
                        Log.DebugWait(m);
                        h1 = c.HintHotkeys;
                        h2 = c.HintDescription;
                    }
                }
                // TODO get h3 from tool.
                var prev_h1 = Hint1;
                var prev_h2 = Hint2;
                var prev_h3 = Hint3;

                Hint1 = h1;
                Hint2 = h2;
                Hint3 = Tool?.Hint;

                if (Hint1 != prev_h1 || Hint2 != prev_h2 || Hint3 != prev_h3) {
                    RefreshValues();
                }
            }
            catch(Exception e) {
                Hint1 = e.ToString();
            }
        }

        public void RefreshValues() {
            //Log.Debug($"Refresh called" + Environment.StackTrace);
            bool h1 = !Hint1.IsNullOrWhiteSpace();
            bool h2 = !Hint2.IsNullOrWhiteSpace();
            bool h3 = !Hint3.IsNullOrWhiteSpace();
            const string nl = "\n";
            string t = "";

            if (h1) t += Hint1;
            if (h1 && h2) t += nl;
            if (h2) t += Hint2;
            if (h2 && h3) t += nl;
            if (h3) t += Hint3;

            text = t;
            isVisible = t != "";
        }
    }
}
