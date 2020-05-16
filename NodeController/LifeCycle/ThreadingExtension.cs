namespace NodeController.LifeCycle {
    using ICities;
    using NodeController.Tool;

    public class ThreadingExtension : ThreadingExtensionBase{
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta) {
            var tool = ToolsModifierControl.toolController?.CurrentTool;
            bool flag = tool == null || tool is NodeControllerTool ||
                tool.GetType() == typeof(DefaultTool) || tool is NetTool || tool is BuildingTool;
            if (flag && NodeControllerTool.ActivationShortcut.IsKeyUp()) {
                NodeControllerTool.Instance.ToggleTool();
            }
        }
    }
}
