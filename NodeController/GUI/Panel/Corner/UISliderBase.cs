namespace NodeController.GUI {
    using KianCommons.UI;
    using KianCommons;
    using ColossalFramework.UI;
    using static KianCommons.HelpersExtensions;
    using UnityEngine;

    public abstract class UISliderBase: UISliderExt, IDataControllerUI {
        //UIResetButton resetButton_ => Root?.ResetButton;
        protected UIPanelBase Root;

        public float ScrollWheelAmount;

        public override void Start() {
            base.Start();
            Root = GetRootContainer() as UIPanelBase;

            stepSize = 0.5f;
            ScrollWheelAmount = 0.5f;
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

        protected override void OnMouseWheel(UIMouseEventParameter p) {
            scrollWheelAmount = ScrollWheelAmount;
            if (ShiftIsPressed) scrollWheelAmount *= 10;
            base.OnMouseWheel(p);
        }

        public void Apply() {
            if (Refreshing) return;
            object data = Root.GetData();
            if (data is NodeData nodeData) {
                ApplyNode(nodeData);
                nodeData.Update();
            } else if (data is SegmentEndData segEndData) {
                ApplySegmentEnd(segEndData);
                segEndData.Update();
            }

            Root?.RefreshValues();
            //resetButton_?.Refresh();
            Refresh();
        }

        /// <summary>set data value. data refresh is already taken care of</summary>
        public abstract void ApplyNode(NodeData data);

        /// <summary>set data value. data refresh is already taken care of</summary>
        public abstract void ApplySegmentEnd(SegmentEndData data);


        public virtual string TooltipPostfix => "";

        protected bool Refreshing = false;
        public virtual void Refresh() {
            Log.Debug("UISliderBase.Refresh() was called");
            INetworkData data = Root.GetData();
            RefreshValues();

            // RefreshValues sets and then unsets Refreshing. therefore this must be called after RefreshValues.
            Refreshing = true; 

            if (isEnabled && data is NodeData nodeData)
                RefreshNode(nodeData);

            tooltip = value.ToString() +  TooltipPostfix;
            RefreshTooltip();

            parent.isVisible = isEnabled;
            parent.Invalidate();
            Invalidate();
            thumbObject.Invalidate();
            SlicedSprite.Invalidate();
            //Log.Debug($"slider.Refresh: node:{data.NodeID} isEnabled={isEnabled}\n" + Environment.StackTrace);
            Refreshing = false;
        }

        /// <summary>
        /// <see cref="RefreshNodeValues()"/> is called before hand.
        /// Precondition: isEnabled = true
        /// setup opactiy, color, ...
        /// </summary>
        public abstract void RefreshNode(NodeData data);

        /// <summary>
        /// read isEnabled and value
        /// </summary>
        public abstract void RefreshNodeValues(NodeData data);

        /// <summary>
        /// read isEnabled and value
        /// </summary>
        public abstract void RefreshSegmentEndValues(SegmentEndData data);

        public virtual void RefreshValues() {
            Refreshing = true;
            INetworkData data = Root?.GetData();
            if (data is SegmentEndData segmentEndData) {
                RefreshSegmentEndValues(segmentEndData);
            } else if (data is NodeData nodeData) {
                RefreshNodeValues(nodeData);
            } else Disable();
            Refreshing = false;
        }
    }
}
