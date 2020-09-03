namespace NodeController.GUI {
    using ColossalFramework.UI;
    using ColossalFramework;
    using KianCommons;
    using KianCommons.UI;
    using UnityEngine;

    public class HintBox : UILabel {
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

        public float width {
            get => base.width;
            set {
                base.width = value;
                this.minimumSize = new Vector2(value, 0);
                this.maximumSize = new Vector2(value, height);
            }
        }

        public float height {
            get => base.height;
            set {
                base.height = value;
                this.maximumSize = new Vector2(width, value);
            }
        }

        public Vector2 size {
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
            Log.Debug($"size={size} minsize={minimumSize} maxsize={maximumSize}");
            Invalidate();
        }

        public string hint1_, hint2_, hint3_;
        public string Hint1 {
            get => hint1_;
            set {
                if (hint1_ != value) {
                    hint1_ = value;
                    Refresh();
                }
            }
        }
        public string Hint2 {
            get => hint2_;
            set {
                if (hint2_ != value) {
                    hint2_ = value;
                    Refresh();
                }
            }
        }
        public string Hint3 {
            get => hint3_;
            set {
                if (hint3_ != value) {
                    hint3_ = value;
                    Refresh();
                }
            }
        }

        public void Refresh() {
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
            isVisible = h1 || h2 || h3;
        }




    }
}
