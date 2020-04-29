
namespace RoadTransitionManager.LifeCycle {
    using System;
    using ICities;
    using UnityEngine;
    using static RoadTransitionManager.Util.HelpersExtensions;
    using RoadTransitionManager.Tool;
    using RoadTransitionManager.Util;

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
