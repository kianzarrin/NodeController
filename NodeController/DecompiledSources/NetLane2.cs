using System;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

public partial struct NetLane2 {
    float m_length;
    NetLane.Flags m_flags;
    Bezier3 m_bezier;

    public void RenderInstance(
        RenderManager.CameraInfo cameraInfo,
        ushort segmentID, uint laneID, NetInfo.Lane laneInfo, NetNode.Flags startFlags,
        NetNode.Flags endFlags, Color startColor, Color endColor, float startAngle, float endAngle, bool invert, int layerMask,
        Vector4 objectIndex1, Vector4 objectIndex2, ref RenderManager.Instance data, ref int propIndex) {
        NetLaneProps laneProps = laneInfo.m_laneProps;

        if (laneProps != null && laneProps.m_props != null) {
            bool backward = (byte)(laneInfo.m_finalDirection & NetInfo.Direction.Both) == 2 || (byte)(laneInfo.m_finalDirection & NetInfo.Direction.AvoidBoth) == 11;
            bool reverse = backward != invert;
            if (backward) { //swap
                NetNode.Flags flags = startFlags;
                startFlags = endFlags;
                endFlags = flags;
            }
            Texture texture = null;
            Vector4 zero = Vector4.zero;
            Vector4 zero2 = Vector4.zero;
            Texture texture2 = null;
            Vector4 zero3 = Vector4.zero;
            Vector4 zero4 = Vector4.zero;
            int nProps = laneProps.m_props.Length;
            for (int i = 0; i < nProps; i++) {
                NetLaneProps.Prop prop = laneProps.m_props[i];
                if (this.m_length >= prop.m_minLength) {
                    int repeatCountTimes2 = 2;
                    if (prop.m_repeatDistance > 1f) {
                        repeatCountTimes2 *= Mathf.Max(1, Mathf.RoundToInt(this.m_length / prop.m_repeatDistance));
                    }
                    int currentPropIndex = propIndex;
                    if (propIndex != -1) {
                        propIndex = currentPropIndex + (repeatCountTimes2 + 1) >> 1; // div 2
                    }
                    if (prop.CheckFlags((NetLane.Flags)this.m_flags, startFlags, endFlags)) {
                        float halfSegmentOffset = prop.m_segmentOffset * 0.5f;
                        if (this.m_length != 0f) {
                            halfSegmentOffset = Mathf.Clamp(halfSegmentOffset + prop.m_position.z / this.m_length, -0.5f, 0.5f);
                        }
                        if (reverse) {
                            halfSegmentOffset = -halfSegmentOffset;
                        }
                        PropInfo finalProp = prop.m_finalProp;
                        if (finalProp != null && (layerMask & 1 << finalProp.m_prefabDataLayer) != 0) {
                            Color color = (prop.m_colorMode != NetLaneProps.ColorMode.EndState) ? startColor : endColor;
                            Randomizer randomizer = new Randomizer((int)(laneID + (uint)i));
                            for (int j = 1; j <= repeatCountTimes2; j += 2) {
                                if (randomizer.Int32(100u) < prop.m_probability) {
                                    float t = halfSegmentOffset + (float)j / (float)repeatCountTimes2;
                                    PropInfo variation = finalProp.GetVariation(ref randomizer);
                                    float scale = variation.m_minScale + (float)randomizer.Int32(10000u) * (variation.m_maxScale - variation.m_minScale) * 0.0001f;
                                    if (prop.m_colorMode == NetLaneProps.ColorMode.Default) {
                                        color = variation.GetColor(ref randomizer);
                                    }
                                    Vector3 pos = this.m_bezier.Position(t);
                                    if (propIndex != -1) {
                                        pos.y = (float)data.m_extraData.GetUShort(currentPropIndex++) * 0.015625f;
                                    }
                                    pos.y += prop.m_position.y;
                                    if (cameraInfo.CheckRenderDistance(pos, variation.m_maxRenderDistance)) {
                                        Vector3 tan = this.m_bezier.Tangent(t);
                                        if (tan != Vector3.zero) {
                                            if (reverse) {
                                                tan = -tan;
                                            }
                                            Vector3 normalXZ = new Vector3 { x = tan.z, z = -tan.x };
                                            if (prop.m_position.x != 0f) {
                                                tan.Normalize();
                                                normalXZ.Normalize();
                                                pos += normalXZ * prop.m_position.x;
                                            }
                                            float normalAngle = Mathf.Atan2(normalXZ.z, normalXZ.x);
                                            if (prop.m_cornerAngle != 0f || prop.m_position.x != 0f) {
                                                float angleDiff = endAngle - startAngle;
                                                if (angleDiff > 3.14159274f) {
                                                    angleDiff -= 6.28318548f;
                                                }
                                                if (angleDiff < -3.14159274f) {
                                                    angleDiff += 6.28318548f;
                                                }
                                                float currentAngle = startAngle + angleDiff * t;
                                                float angle2 = currentAngle - normalAngle;
                                                if (angle2 > 3.14159274f) {
                                                    angle2 -= 6.28318548f;
                                                }
                                                if (angle2 < -3.14159274f) {
                                                    angle2 += 6.28318548f;
                                                }
                                                normalAngle += angle2 * prop.m_cornerAngle;
                                                if (angle2 != 0f && prop.m_position.x != 0f) {
                                                    float d = Mathf.Tan(angle2);
                                                    pos.x += tan.x * d * prop.m_position.x;
                                                    pos.z += tan.z * d * prop.m_position.x;
                                                }
                                            }
                                            Vector4 objectIndex3 = (t <= 0.5f) ? objectIndex1 : objectIndex2;
                                            normalAngle += prop.m_angle * 0.0174532924f;
                                            InstanceID id = default(InstanceID);
                                            id.NetSegment = segmentID;
                                            if (variation.m_requireWaterMap) {
                                                if (texture == null) {
                                                    Singleton<TerrainManager>.instance.GetHeightMapping(Singleton<NetManager>.instance.m_segments.m_buffer[(int)segmentID].m_middlePosition, out texture, out zero, out zero2);
                                                }
                                                if (texture2 == null) {
                                                    Singleton<TerrainManager>.instance.GetWaterMapping(Singleton<NetManager>.instance.m_segments.m_buffer[(int)segmentID].m_middlePosition, out texture2, out zero3, out zero4);
                                                }
                                                PropInstance.RenderInstance(cameraInfo, variation, id, pos, scale, normalAngle, color, objectIndex3, true, texture, zero, zero2, texture2, zero3, zero4);
                                            } else if (!variation.m_requireHeightMap) {
                                                PropInstance.RenderInstance(cameraInfo, variation, id, pos, scale, normalAngle, color, objectIndex3, true);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        TreeInfo finalTree = prop.m_finalTree;
                        if (finalTree != null && (layerMask & 1 << finalTree.m_prefabDataLayer) != 0) {
                            Randomizer randomizer2 = new Randomizer((int)(laneID + (uint)i));
                            for (int k = 1; k <= repeatCountTimes2; k += 2) {
                                if (randomizer2.Int32(100u) < prop.m_probability) {
                                    float t = halfSegmentOffset + (float)k / (float)repeatCountTimes2;
                                    TreeInfo variation2 = finalTree.GetVariation(ref randomizer2);
                                    float scale2 = variation2.m_minScale + (float)randomizer2.Int32(10000u) * (variation2.m_maxScale - variation2.m_minScale) * 0.0001f;
                                    float brightness = variation2.m_minBrightness + (float)randomizer2.Int32(10000u) * (variation2.m_maxBrightness - variation2.m_minBrightness) * 0.0001f;
                                    Vector3 position = this.m_bezier.Position(t);
                                    if (propIndex != -1) {
                                        position.y = (float)data.m_extraData.GetUShort(currentPropIndex++) * 0.015625f;
                                    }
                                    position.y += prop.m_position.y;
                                    if (prop.m_position.x != 0f) {
                                        Vector3 vector3 = this.m_bezier.Tangent(t);
                                        if (reverse) {
                                            vector3 = -vector3;
                                        }
                                        vector3.y = 0f;
                                        vector3 = Vector3.Normalize(vector3);
                                        position.x += vector3.z * prop.m_position.x;
                                        position.z -= vector3.x * prop.m_position.x;
                                    }
                                    global::TreeInstance.RenderInstance(cameraInfo, variation2, position, scale2, brightness, RenderManager.DefaultColorLocation);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
