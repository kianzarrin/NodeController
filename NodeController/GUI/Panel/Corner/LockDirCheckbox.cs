namespace NodeController.GUI {
    using KianCommons;
    using static KianCommons.HelpersExtensions;
    using KianCommons.UI;
    using UnityEngine;

    public class LockDirCheckbox :UICheckBoxExt, IDataControllerUI {
        public override void Awake() {
            base.Awake();
            name = nameof(LockDirCheckbox);
            label.text = "lock";
            tooltip = "Control : lock left and right";
        }

        static bool LockMode => ControlIsPressed && !AltIsPressed;
        Color GetColor() {
            if (containsMouse && LockMode) 
                return Color.green;
            
            if (Mirror != null && Mirror.containsMouse && LockMode) 
                return Color.green;

            if (containsMouse)
                return Color.white;

            return Color.Lerp(Color.grey, Color.white, 0.50f);
        }

        public override void Update() {
            base.Update();
            var c = GetColor();
            label.textColor = Color.Lerp(c, Color.white, 0.70f);
        }

        public override void OnCheckedChanged() {
            if (refreshing_) return;
            Apply();
        }

        UIPanelBase root_;
        public override void Start() {
            base.Start();
            width = parent.width;
            root_ = GetRootContainer() as UIPanelBase;
        }

        public LockDirCheckbox Mirror;
        public bool Left; // going away from the junction.

        public void Apply() {
            Assert(!refreshing_, "!refreshing_");
            Log.Debug("LockDirCheckbox.Apply called()\n"/* + Environment.StackTrace*/);

            SegmentEndData data = root_?.GetData() as SegmentEndData;
            if (data == null) return;

            data.Corner(Left).LockLength = this.isChecked;
            if (ControlIsPressed &&  Mirror != null)
                Mirror.isChecked = isChecked;

            data.RefreshAndUpdate();
            root_.Refresh();
        }

        // protection against unncessary apply/refresh/infinite recursion.
        bool refreshing_ = false;

        public void Refresh() {
            //Log.Debug("Refresh called()\n"/* + Environment.StackTrace*/);
            refreshing_ = true;

            RefreshValues();
            //if (root_?.GetData() is NodeData nodeData)
            //    RefreshNode(nodeData);

            parent.isVisible = isVisible = this.isEnabled;
            parent.Invalidate();
            Invalidate();
            refreshing_ = false;
        }

        public void RefreshValues() {
            INetworkData data = root_?.GetData();
            if (data is SegmentEndData segmentEndData) {
                this.isChecked = segmentEndData.Corner(Left).LockLength;
                isEnabled = segmentEndData.CanModifyCorners();
            } else if (data is NodeData nodeData) {
                //this.isChecked = nodeData.FlatJunctions; // TODO complete
                //isEnabled = nodeData.CanMassEditNodeCorners();
            } else Disable();
        }
    }
}
