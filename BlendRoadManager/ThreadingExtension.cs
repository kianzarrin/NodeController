
namespace BlendRoadManager {
    using System;
    using ICities;
    using UnityEngine;
    using static BlendRoadManager.Util.HelpersExtensions;
    using BlendRoadManager.Tool;

    public class ThreadingExtension : ThreadingExtensionBase{
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta) {
            if (ControlIsPressed && Input.GetKeyDown(KeyCode.T)) {
                SimulationManager.instance.m_ThreadingWrapper.QueueMainThread(
                    ()=> BlendRoadTool.Instance.ToggleTool());
            }   

        }
    }
}
