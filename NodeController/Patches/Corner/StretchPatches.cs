namespace NodeController.Patches.Corner;
using HarmonyLib;
using KianCommons;
using KianCommons.Patches;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

// vehicle/pedestrian paths take into consideration the stretched lane/segment.
[HarmonyPatch]
static class WidthPatch1 {
    static IEnumerable<MethodBase> TargetMethods() {
        // stretch the area ppl wait at bus stop:
        // private Vector4 HumanAI.GetTransportWaitPosition(ushort instanceID, ref CitizenInstance citizenData, ref CitizenInstance.Frame frameData, float minSqrDistance)
        yield return typeof(HumanAI).GetMethod("GetTransportWaitPosition", throwOnError: true);

        // stretch the walkable pedestrian area:
        // protected Vector4 CitizenAI.GetPathTargetPosition(ushort instanceID, ref CitizenInstance citizenData, ref CitizenInstance.Frame frameData, float minSqrDistance)
        yield return typeof(CitizenAI).GetMethod("GetPathTargetPosition", throwOnError: true);

        // push cars to the side:
        // private static bool PassengerCarAI.FindParkingSpaceRoadSide(ushort ignoreParked, ushort requireSegment, Vector3 refPos, float width, float length, out Vector3 parkPos, out Quaternion parkRot, out float parkOffset)
        yield return typeof(PassengerCarAI).GetMethod("FindParkingSpaceRoadSide", throwOnError: true);
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original) {
        FieldInfo fLaneWidth = ReflectionHelpers.GetField<NetInfo.Lane>(nameof(NetInfo.Lane.m_width));
        FieldInfo fNetHW = ReflectionHelpers.GetField<NetInfo>(nameof(NetInfo.m_halfWidth));
        FieldInfo fNetPW = ReflectionHelpers.GetField<NetInfo>(nameof(NetInfo.m_pavementWidth));
        MethodInfo mStretch = typeof(WidthPatch1).GetMethod(nameof(Stretch), throwOnError: true);
        var codes = instructions.ToList();

        bool LoadsPathPos(CodeInstruction c) => c.IsLdLoc(typeof(PathUnit.Position), original);

        for (int i = 0; i < codes.Count; i++) {
            var code = codes[i];
            if (code.LoadsField(fLaneWidth) || code.LoadsField(fNetHW) || code.LoadsField(fNetPW)) {
                int iLoadPathPos = codes.Search(predicate: LoadsPathPos, startIndex: i, count: -1);
                i += codes.InsertInstructions(
                    i + 1, // insert after
                    new[] {
                        // m_width or m_halfWidth or m_pavementWidth already on stack
                        codes[iLoadPathPos].Clone(),
                        new CodeInstruction(OpCodes.Call, mStretch),
                });
            }
        }

        return codes;
    }

    static float Stretch(float w0, ref PathUnit.Position pathPosition) {
        ushort segmentId = pathPosition.m_segment;
        ref SegmentEndData segStart = ref SegmentEndManager.Instance.GetAt(segmentId, true);
        ref SegmentEndData segEnd = ref SegmentEndManager.Instance.GetAt(segmentId, false);
        float stretchStart = segStart?.Stretch ?? 0;
        float stretchEnd = segEnd?.Stretch ?? 0;
        float stretch = Mathf.Min(stretchStart, stretchEnd);
        float ratio = stretch * 0.01f + 1;
        return w0 * ratio;
    }
}


[HarmonyPatch(typeof(NetLane), nameof(NetLane.CalculateStopPositionAndDirection))]
[HarmonyPriority(Priority.High)]
[HarmonyBefore("me.tmpe")]
static class CalculateStopPositionAndDirectionPatch {
    static void Prefix(ref NetLane __instance, float laneOffset, ref float stopOffset) {
        float sign = Mathf.Sign(stopOffset);
        if (sign != 0) {
            ushort segmentId = __instance.m_segment;
            ref SegmentEndData segStart = ref SegmentEndManager.Instance.GetAt(segmentId, true);
            ref SegmentEndData segEnd = ref SegmentEndManager.Instance.GetAt(segmentId, false);

            float stretchStart = segStart?.Stretch ?? 0;
            float stretchEnd = segEnd?.Stretch ?? 0;
            float stretch = Mathf.Lerp(stretchStart, stretchEnd, laneOffset);
            float ratio = stretch * 0.01f + 1;
            stopOffset *= ratio;

            // take into account that the space also stretches.
            const float BUS_WIDTH = 3f;
            stopOffset += sign * BUS_WIDTH * 0.5f * stretch * 0.01f;
        }
    }
}