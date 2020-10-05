namespace NodeController.LifeCycle
{
    using ICities;
    using KianCommons;

    public class LoadingExtention : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode) {
            Log.Debug("LoadingExtention.OnLevelLoaded");

            LifeCycle.OnLevelLoaded(mode);
        }

        public override void OnLevelUnloading()
        {
            Log.Debug("LoadingExtention.OnLevelUnloading");
            LifeCycle.OnLevelUnloading();
        }
    }
}
