namespace NodeController.GUI {
    using KianCommons.UI;
    using KianCommons;
    using ColossalFramework.UI;
    using static KianCommons.HelpersExtensions;
    using UnityEngine;
    using System;

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
            if (!Refreshing)
                Apply();
            UpdateTooltip();
        }

        protected override void OnMouseWheel(UIMouseEventParameter p) {
            scrollWheelAmount = ScrollWheelAmount;
            if (ShiftIsPressed) scrollWheelAmount *= 10;
            base.OnMouseWheel(p);
        }

        public void Apply() {
            Assert(!Refreshing);
            object data = Root.GetData();
            if (data is NodeData nodeData) {
                ApplyNode(nodeData);
                nodeData.Update();
            } else if (data is SegmentEndData segEndData) {
                ApplySegmentEnd(segEndData);
                segEndData.Update();
            }

            Root?.RefreshValues();
        }

        /// <summary>set data value. data refresh is already taken care of</summary>
        public abstract void ApplyNode(NodeData data);

        /// <summary>set data value. data refresh is already taken care of</summary>
        public abstract void ApplySegmentEnd(SegmentEndData data);


        public virtual string TooltipPostfix => "";
        public bool TooltipVisible=true;

        protected bool Refreshing = false;
        public virtual void Refresh() {
            try {
                //Log.Debug("UISliderBase.Refresh() was called\n" /*+ Environment.StackTrace*/);
                INetworkData data = Root.GetData();
                RefreshValues();

                // RefreshValues sets and then unsets Refreshing. therefore this must be called after RefreshValues.
                Refreshing = true;

                if (isEnabled && data is NodeData nodeData)
                    RefreshNode(nodeData);

                parent.isVisible = isEnabled;
                parent.Invalidate();
                //Invalidate(); // TODO is this redundant?
                //thumbObject.Invalidate();
                //SlicedSprite.Invalidate();
                //Log.Debug($"slider.Refresh: node:{data.NodeID} isEnabled={isEnabled}\n" + Environment.StackTrace);
            }
            finally {
                Refreshing = false;
            }
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
            try {
                Refreshing = true;
                INetworkData data = Root?.GetData();
                if (data is SegmentEndData segmentEndData) {
                    RefreshSegmentEndValues(segmentEndData);
                } else if (data is NodeData nodeData) {
                    RefreshNodeValues(nodeData);
                } else Disable();
           }
            finally {
                Refreshing = false;
            }
        }

        public virtual void UpdateTooltip() {
            if (TooltipVisible) {
                tooltip = value.ToString() + TooltipPostfix;
                RefreshTooltip();
            } else {
                tooltip = null;
                RefreshTooltip();
            }
        }
    }
}
