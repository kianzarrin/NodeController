namespace NodeController.Patches {
    using HarmonyLib;
    using System;
    using System.Reflection;
    using UnityEngine;
    using KianCommons;
    using KianCommons.Math;
    using ColossalFramework;

    // Vehicle
    //public static void RenderInstance(RenderManager.CameraInfo cameraInfo, VehicleInfo info,
    // Vector3 position, Quaternion rotation, Vector3 swayPosition, Vector4 lightState, Vector4 tyrePosition,
    // Vector3 velocity, float acceleration, Color color,
    // Vehicle.Flags flags, int variationMask, InstanceID id, bool underground, bool overground)
    //[HarmonyPatch()]
    public static class Vehicle_RenderInstancePatch {
        static MethodBase TargetMethod() =>
            typeof(Vehicle)
            .GetMethod(nameof(Vehicle.RenderInstance), BindingFlags.Public | BindingFlags.Static)
            ?? throw new Exception("could nto find Vehicle.RenderInstance");


        static Vehicle[] buff = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;

        static void Prefix(InstanceID id, ref Vector3 swayPosition, ref Quaternion rotation) {
            //ref Vehicle v = ref buff[id.Vehicle];
            float a1 = 30; // total rotation
            float a2 = 30; // extra body rotation
            var rot = Quaternion.Euler(0, 0f, -a1); // minus to rotate to right
            rotation = rotation * rot;
            swayPosition.x += a2 * Mathf.Deg2Rad; // rotates to the right.
        }
    }
}

