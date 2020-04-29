
namespace BlendRoadManager.LifeCycle {
    using System;
    using ICities;
    using UnityEngine;
    using static BlendRoadManager.Util.HelpersExtensions;
    using BlendRoadManager.Tool;
    using BlendRoadManager.Util;

    public class ThreadingExtension : ThreadingExtensionBase{
        public override void OnBeforeSimulationTick() {
            if (LifeCycle.bFirstFrame) {
                LifeCycle.FirstFrame();
           }
        }
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta) {
            if (ControlIsPressed && Input.GetKeyDown(KeyCode.T)) {
                SimulationManager.instance.m_ThreadingWrapper.QueueMainThread(
                    ()=> BlendRoadTool.Instance.ToggleTool());
            }   

        }
    }
}
