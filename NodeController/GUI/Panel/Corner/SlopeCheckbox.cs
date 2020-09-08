namespace NodeController.GUI {
    using KianCommons;
    using static KianCommons.HelpersExtensions;
    using KianCommons.UI;
    using UnityEngine;
    using ColossalFramework.UI;

    public class SlopeJunctionCheckbox :UICheckBoxExt, IDataControllerUI {
        public override void Awake() {
            base.Awake();
            name = nameof(SlopeJunctionCheckbox);
            label.text = "Slope at junction";
        }

        public string HintHotkeys => null;

        public string HintDescription => "gives slope to main road\nembanks side roads if any";

        Color GetColor() {
            if (containsMouse)
                return Color.white;
            return Color.Lerp(Color.grey, Color.white, 0.50f);
        }

        public override void Update() {
            base.Update();
            var c = GetColor();
            label.textColor = Color.Lerp(c, Color.white, 0.70f);
        }

        public override void OnCheckChanged(UIComponent component, bool value) {
            base.OnCheckChanged(component, value);
            if (refreshing_) return;
            Apply();
        }

        UINodeControllerPanel root_;
        public override void Start() {
            base.Start();
            width = parent.width;
            root_ = GetRootContainer() as UINodeControllerPanel;
        }


        public void Apply() {
            Assert(!refreshing_, "!refreshing_");
            Log.Debug("SlopeJunctionCheckbox.Apply called()\n"/* + Environment.StackTrace*/);

            NodeData data = root_?.NodeData;
            if (data == null) return;

            data.SlopedJunction = isChecked;

            data.RefreshAndUpdate();
            root_.Refresh();
        }

        // protection against unncessary apply/refresh/infinite recursion.
        bool refreshing_ = false;

        public void Refresh() {
            //Log.Debug("Refresh called()\n"/* + Environment.StackTrace*/);
            RefreshValues();
            refreshing_ = true;
            if (isEnabled && root_?.GetData() is NodeData nodeData) {
                RefreshNode(nodeData);
            }

            parent.isVisible = isVisible = this.isEnabled;
            parent.Invalidate();
            Invalidate();
            refreshing_ = false;
        }

        public void RefreshNode(NodeData data) {
            if (data.HasUniformSlopedJunction()) {
                checkedBoxObject.color = Color.white;
            } else {
                checkedBoxObject.color = Color.grey;
            }
        }

        public void RefreshValues() {
            refreshing_ = true;
            INetworkData data = root_?.GetData();
            if (data is NodeData nodeData) {
                isEnabled = nodeData.CanModifyFlatJunctions();
                if (isEnabled) {
                    this.isChecked = nodeData.SlopedJunction;
                }
            }
            refreshing_ = false;
        }

        public void Reset() {
            isChecked = false;
        }
    }
}
