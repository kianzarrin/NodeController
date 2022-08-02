namespace NodeController.GUI {
    using ColossalFramework.UI;
    using System;
    using KianCommons;
    using KianCommons.UI;

    public abstract class ButtonBase : UIButtonExt, IDataControllerUI {
        public abstract string HintHotkeys { get; }

        public abstract string HintDescription { get; }

        public INetworkData Data => root_.Alive()?.GetData();

        UIPanelBase root_;
        public override void Awake() {
            //Log.Debug("ButtonBase.Awake() called",false);
            base.Awake();
            ParentWith = false;
        }

        public override void Start() {
            base.Start();
            root_ = GetRootContainer() as UIPanelBase;
        }

        protected override void OnClick(UIMouseEventParameter p) {
            base.OnClick(p);
            if (refreshing_)
                return;
            Apply();
        }

        public void Apply() {
            if (Log.VERBOSE) Log.Debug("UIResetButton.Apply called()\n" + Environment.StackTrace);
            var data = root_.Alive()?.GetData();
            Action(data);
            data.RefreshAndUpdate();
            Assertion.Assert(!refreshing_, "!refreshing_");
            root_.Refresh();
        }

        // protection against unnecessary apply/refresh/infinite recursion.
        bool refreshing_ = false;

        public void Refresh() {
            if (Log.VERBOSE) Log.Debug("Refresh called()\n" + Environment.StackTrace);
            RefreshValues();
            refreshing_ = true;
            parent.isVisible = isVisible = isEnabled;
            Invalidate();
            //Log.Debug("ButtonBase.Refresh(): isVisible set to " + isVisible);
            refreshing_ = false;
        }

        public void RefreshValues() {
            refreshing_ = true;
            INetworkData data = root_.Alive().GetData();
            if (data == null) {
                Disable();
                return;
            }
            isEnabled = ShouldShow;
            refreshing_ = false;
        }

        public abstract bool ShouldShow { get; }
        public abstract void Action(INetworkData data);

        public void Reset() { } //can't be reset.
    }
}


