namespace NodeController.GUI {
    using KianCommons.UI;
    using KianCommons;

    public abstract class UISliderBase: UISliderExt, IDataControllerUI {
        UIResetButton resetButton_;
        UIPanelBase root_;

        public override void Start() {
            base.Start();
            root_ = GetRootContainer() as UIPanelBase;
            resetButton_ = root_.GetComponentInChildren<UIResetButton>();

            //Log.Debug($"UISliderBase.Start() was called " +
            //    $"this.version={this.VersionOf()} " +
            //    $"root.version={root_.VersionOf()} " +
            //    $"ncpanel.instance.version={UINodeControllerPanel.Instance.VersionOf()} " +
            //    $"UINodeControllerPanel.version={typeof(UINodeControllerPanel).VersionOf()} "+
            //    $"UISliderBase.version={typeof(UISliderBase).VersionOf()} ");
        }

        protected override void OnValueChanged() {
            base.OnValueChanged();
            Apply();
        }

        public void Apply() {
            if (Refreshing) return;
            object data = root_.GetData();
            if (data is NodeData nodeData) {
                ApplyNode(nodeData);
                nodeData.Refresh();
            } else if (data is SegmentEndData segEndData) {
                ApplySegmentEnd(segEndData);
                segEndData.Refresh();
            }

            resetButton_?.Refresh();
            Refresh();
        }

        /// <summary>set data value. data refresh is already taken care of</summary>
        public abstract void ApplyNode(NodeData data);

        /// <summary>set data value. data refresh is already taken care of</summary>
        public abstract void ApplySegmentEnd(SegmentEndData data);

        protected bool Refreshing = false;
        public void Refresh() {
            Log.Debug("UISliderBase.Refresh() was called");

            Refreshing = true;

            tooltip = value.ToString();
            RefreshTooltip();

            var data = root_.GetData();
            if (data is NodeData nodeData) {
                RefreshNode(nodeData);
            } else if (data is SegmentEndData segEndData) {
                RefreshSegmentEnd(segEndData);
            } else if (data == null) {
                Log.Debug("data is null. disabling ...");
                Disable();
            } else {
                throw new System.Exception($"UISliderBase.Refresh() Unreachable code: " +
                    $"this.version:{this.VersionOf()} " +
                    $"data={data.GetType().Name}.{data.VersionOf()} " +
                    $"NodeData.version={typeof(SegmentEndData).VersionOf()}");
            }

            parent.isVisible = isEnabled;
            parent.Invalidate();
            Invalidate();
            thumbObject.Invalidate();
            SlicedSprite.Invalidate();
            //Log.Debug($"slider.Refresh: node:{data.NodeID} isEnabled={isEnabled}\n" + Environment.StackTrace);
            Refreshing = false;
        }

        /// <summary>
        /// read value from data.
        /// setup is enabled (which in turn sets up is visible), opactiy, color, ...
        /// </summary>
        public abstract void RefreshNode(NodeData data);

        /// <summary>
        /// read value from data.
        /// setup is enabled (which in turn sets up is visible), opactiy, color, ...
        /// </summary>
        public abstract void RefreshSegmentEnd(SegmentEndData data);
    }
}
