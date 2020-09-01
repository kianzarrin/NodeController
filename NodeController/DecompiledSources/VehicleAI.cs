using System;
using ColossalFramework;
using ColossalFramework.Math;
using UnityEngine;

public class CarAI2 : CarAI {
    private void SimulationStepBlown(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics) { }
    private void SimulationStepFloating(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics) { }
    private static float CalculateMaxSpeed(float targetDistance, float targetSpeed, float maxBraking) => throw new NotImplementedException();
    private static bool DisableCollisionCheck(ushort vehicleID, ref Vehicle vehicleData) => throw new NotImplementedException();
    NetSegment[] SegmentBuffer;
    NetLane[] LaneBuffer;

    public override void SimulationStep(ushort vehicleID, ref Vehicle vehicleData, ref Vehicle.Frame frameData, ushort leaderID, ref Vehicle leaderData, int lodPhysics) {
        if ((leaderData.m_flags2 & Vehicle.Flags2.Blown) != (Vehicle.Flags2)0) {
            this.SimulationStepBlown(vehicleID, ref vehicleData, ref frameData, leaderID, ref leaderData, lodPhysics);
            return;
        }
        if ((leaderData.m_flags2 & Vehicle.Flags2.Floating) != (Vehicle.Flags2)0) {
            this.SimulationStepFloating(vehicleID, ref vehicleData, ref frameData, leaderID, ref leaderData, lodPhysics);
            return;
        }
        uint currentFrameIndex = Singleton<SimulationManager>.instance.m_currentFrameIndex;
        frameData.m_position += frameData.m_velocity * 0.5f;
        frameData.m_swayPosition += frameData.m_swayVelocity * 0.5f;
        float accelerationAbility = this.m_info.m_acceleration;
        float brakingAbility = this.m_info.m_braking;
        if (vehicleData.m_flags.IsFlagSet(Vehicle.Flags.Emergency2)) {
            accelerationAbility *= 2f;
            brakingAbility *= 2f;
        }
        float velocity = frameData.m_velocity.magnitude;
        Vector3 posdiff0 = (Vector3)vehicleData.m_targetPos0 - frameData.m_position;
        float postdiff0_len_pow2 = posdiff0.sqrMagnitude;
        float carLength = this.m_info.m_generatedInfo.m_size.z;

        float velocity_max_new2 = Mathf.Max(velocity + accelerationAbility, 5f); // maximum new velocity the car is capble of.
        if (lodPhysics >= 2 && (ulong)(currentFrameIndex >> 4 & 3u) == (ulong)(vehicleID & 3)) {
            velocity_max_new2 *= 2f;
        }

        float num3 = (velocity + accelerationAbility) * (0.5f + 0.5f * (velocity + accelerationAbility) / brakingAbility) + carLength * 0.5f;
        float num5 = Mathf.Max((num3 - velocity_max_new2) / 3f, 1f);
        float velocity_max_new2_pow2 = velocity_max_new2 * velocity_max_new2;
        float num7 = num5 * num5;
        int i = 0;
        bool shortTravelDistanceButStillMoving = false;
        if ((postdiff0_len_pow2 < velocity_max_new2_pow2 || vehicleData.m_targetPos3.w < 0.01f) && !leaderData.m_flags.IsFlagSet(Vehicle.Flags.WaitingPath | Vehicle.Flags.Stopped)) {
            if (leaderData.m_path != 0u) {
                base.UpdatePathTargetPositions(vehicleID, ref vehicleData, frameData.m_position, ref i, 4, velocity_max_new2_pow2, num7);
                if (!leaderData.m_flags.IsFlagSet(Vehicle.Flags.Spawned)) {
                    frameData = vehicleData.m_frame0;
                    return;
                }
            }
            if (!leaderData.m_flags.IsFlagSet(Vehicle.Flags.WaitingPath)) {
                while (i < 4) {
                    float minSqrDistance;
                    Vector3 refPos;
                    if (i == 0) {
                        minSqrDistance = velocity_max_new2_pow2;
                        refPos = frameData.m_position;
                        shortTravelDistanceButStillMoving = true;
                    } else {
                        minSqrDistance = num7;
                        refPos = vehicleData.GetTargetPos(i - 1);
                    }
                    int i0 = i;
                    this.UpdateBuildingTargetPositions(vehicleID, ref vehicleData, refPos, leaderID, ref leaderData, ref i, minSqrDistance);
                    if (i == i0) {
                        break;
                    }
                }
                if (i != 0) {
                    Vector4 targetPos = vehicleData.GetTargetPos(i - 1);
                    while (i < 4) {
                        vehicleData.SetTargetPos(i++, targetPos);
                    }
                }
            }
            posdiff0 = (Vector3)vehicleData.m_targetPos0 - frameData.m_position;
            postdiff0_len_pow2 = posdiff0.sqrMagnitude;
        }
        if (leaderData.m_path != 0u && !leaderData.m_flags.IsFlagSet(Vehicle.Flags.WaitingPath)) {
            byte b = leaderData.m_pathPositionIndex;
            byte lastPathOffset = leaderData.m_lastPathOffset;
            if (b == 255) {
                b = 0;
            }
            int noise;
            float num9 = 1f + leaderData.CalculateTotalLength(leaderID, out noise);
            PathManager instance2 = Singleton<PathManager>.instance;
            PathUnit.Position pathPos;
            if (instance2.m_pathUnits.m_buffer[leaderData.m_path].GetPosition(b >> 1, out pathPos)) {
                if (SegmentBuffer[pathPos.m_segment].m_flags.IsFlagSet(NetSegment.Flags.Flooded) &&
                    Singleton<TerrainManager>.instance.HasWater(VectorUtils.XZ(frameData.m_position))) {
                    leaderData.m_flags2 |= Vehicle.Flags2.Floating;
                }
                SegmentBuffer[pathPos.m_segment].AddTraffic(Mathf.RoundToInt(num9 * 2.5f), noise);
                bool flag2 = false;
                if ((b & 1) == 0 || lastPathOffset == 0) {
                    uint laneID = PathManager.GetLaneID(pathPos);
                    if (laneID != 0u) {
                        Vector3 b2 = LaneBuffer[laneID].CalculatePosition(pathPos.m_offset * (1f/255));
                        float num10 = 0.5f * velocity * velocity / brakingAbility + carLength * 0.5f;
                        if (Vector3.Distance(frameData.m_position, b2) >= num10 - 1f) {
                            LaneBuffer[laneID].ReserveSpace(num9);
                            flag2 = true;
                        }
                    }
                }
                if (!flag2 && instance2.m_pathUnits.m_buffer[leaderData.m_path].GetNextPosition(b >> 1, out pathPos)) {
                    uint laneID2 = PathManager.GetLaneID(pathPos);
                    if (laneID2 != 0u) {
                        LaneBuffer[laneID2].ReserveSpace(num9);
                    }
                }
            }
            if ((ulong)(currentFrameIndex >> 4 & 15u) == (ulong)((long)(leaderID & 15))) {
                bool flag3 = false;
                uint path = leaderData.m_path;
                int num11 = b >> 1;
                int j = 0;
                while (j < 5) {
                    bool flag4;
                    if (PathUnit.GetNextPosition(ref path, ref num11, out pathPos, out flag4)) {
                        uint laneID3 = PathManager.GetLaneID(pathPos);
                        if (laneID3 != 0u && !LaneBuffer[laneID3].CheckSpace(num9)) {
                            j++;
                            continue;
                        }
                    }
                    if (flag4) {
                        this.InvalidPath(vehicleID, ref vehicleData, leaderID, ref leaderData);
                    }
                    flag3 = true;
                    break;
                }
                if (!flag3) {
                    leaderData.m_flags |= Vehicle.Flags.Congestion;
                }
            }
        }
        float num12;
        if (leaderData.m_flags.IsFlagSet(Vehicle.Flags.Stopped)) {
            num12 = 0f;
        } else {
            num12 = vehicleData.m_targetPos0.w;
            if (!leaderData.m_flags.IsFlagSet(Vehicle.Flags.DummyTraffic)) {
                VehicleManager instance3 = Singleton<VehicleManager>.instance;
                float f = velocity * 100f / Mathf.Max(1f, vehicleData.m_targetPos0.w);
                instance3.m_totalTrafficFlow += (uint)Mathf.RoundToInt(f);
                instance3.m_maxTrafficFlow += 100u;
            }
        }
        Quaternion rotation = Quaternion.Inverse(frameData.m_rotation);
        posdiff0 = rotation * posdiff0;
        Vector3 vector2 = rotation * frameData.m_velocity;
        Vector3 forwardDir0 = Vector3.forward;
        Vector3 vector3;
        Vector3 zero = Vector3.zero;
        float num13 = 0f;
        float num14 = 0f;
        bool flag5 = false;
        float posdiff0_lenxz = 0f;
        if (postdiff0_len_pow2 > 1f) {
            forwardDir0 = VectorUtils.NormalizeXZ(posdiff0, out posdiff0_lenxz);
            if (posdiff0_lenxz > 1f) {
                Vector3 vector4 = posdiff0;
                velocity_max_new2 = Mathf.Max(velocity, 2f);
                velocity_max_new2_pow2 = velocity_max_new2 * velocity_max_new2;
                if (postdiff0_len_pow2 > velocity_max_new2_pow2) {
                    vector4 *= velocity_max_new2 / Mathf.Sqrt(postdiff0_len_pow2);
                }
                bool flag6 = false;
                if (vector4.z < Mathf.Abs(vector4.x)) {
                    if (vector4.z < 0f) {
                        flag6 = true;
                    }
                    float num16 = Mathf.Abs(vector4.x);
                    if (num16 < 1f) {
                        vector4.x = Mathf.Sign(vector4.x);
                        if (vector4.x == 0f) {
                            vector4.x = 1f;
                        }
                        num16 = 1f;
                    }
                    vector4.z = num16;
                }
                float b3;
                forwardDir0 = VectorUtils.NormalizeXZ(vector4, out b3);
                posdiff0_lenxz = Mathf.Min(posdiff0_lenxz, b3);
                float num17 = 1.57079637f * (1f - forwardDir0.z);
                if (posdiff0_lenxz > 1f) {
                    num17 /= posdiff0_lenxz;
                }
                float num18 = posdiff0_lenxz;
                if (vehicleData.m_targetPos0.w < 0.1f) {
                    num12 = this.CalculateTargetSpeed(vehicleID, ref vehicleData, 1000f, num17);
                    num12 = Mathf.Min(num12, CalculateMaxSpeed(num18, Mathf.Min(vehicleData.m_targetPos0.w, vehicleData.m_targetPos1.w), brakingAbility * 0.9f));
                } else {
                    num12 = Mathf.Min(num12, this.CalculateTargetSpeed(vehicleID, ref vehicleData, 1000f, num17));
                    num12 = Mathf.Min(num12, CalculateMaxSpeed(num18, vehicleData.m_targetPos1.w, brakingAbility * 0.9f));
                }
                num18 += VectorUtils.LengthXZ(vehicleData.m_targetPos1 - vehicleData.m_targetPos0);
                num12 = Mathf.Min(num12, CalculateMaxSpeed(num18, vehicleData.m_targetPos2.w, brakingAbility * 0.9f));
                num18 += VectorUtils.LengthXZ(vehicleData.m_targetPos2 - vehicleData.m_targetPos1);
                num12 = Mathf.Min(num12, CalculateMaxSpeed(num18, vehicleData.m_targetPos3.w, brakingAbility * 0.9f));
                num18 += VectorUtils.LengthXZ(vehicleData.m_targetPos3 - vehicleData.m_targetPos2);
                if (vehicleData.m_targetPos3.w < 0.01f) {
                    num18 = Mathf.Max(0f, num18 - carLength * 0.5f);
                }
                num12 = Mathf.Min(num12, CalculateMaxSpeed(num18, 0f, brakingAbility * 0.9f));
                if (!DisableCollisionCheck(leaderID, ref leaderData)) {
                    CarAI.CheckOtherVehicles(vehicleID, ref vehicleData, ref frameData, ref num12, ref flag5, ref zero, num3, brakingAbility * 0.9f, lodPhysics);
                }
                if (flag6) {
                    num12 = -num12;
                }
                if (num12 < velocity) {
                    float num19 = Mathf.Max(accelerationAbility, Mathf.Min(brakingAbility, velocity));
                    num13 = Mathf.Max(num12, velocity - num19);
                } else {
                    float num20 = Mathf.Max(accelerationAbility, Mathf.Min(brakingAbility, -velocity));
                    num13 = Mathf.Min(num12, velocity + num20);
                }
            }
        } else if (velocity < 0.1f && shortTravelDistanceButStillMoving && this.ArriveAtDestination(leaderID, ref leaderData)) {
            leaderData.Unspawn(leaderID);
            if (leaderID == vehicleID) {
                frameData = leaderData.m_frame0;
            }
            return;
        }
        if (!leaderData.m_flags.IsFlagSet(Vehicle.Flags.Stopped) && num12 < 0.1f) {
            flag5 = true;
        }
        if (flag5) {
            vehicleData.m_blockCounter = (byte)Mathf.Min((vehicleData.m_blockCounter + 1), 255);
        } else {
            vehicleData.m_blockCounter = 0;
        }
        if (posdiff0_lenxz > 1f) {
            num14 = Mathf.Asin(forwardDir0.x) * Mathf.Sign(num13);
            vector3 = forwardDir0 * num13;
        } else {
            num13 = 0f;
            Vector3 b4 = Vector3.ClampMagnitude(posdiff0 * 0.5f - vector2, brakingAbility);
            vector3 = vector2 + b4;
        }
        bool flag7 = (currentFrameIndex + (uint)leaderID & 16u) != 0u;
        Vector3 a2 = vector3 - vector2;
        Vector3 vector5 = frameData.m_rotation * vector3;
        frameData.m_velocity = vector5 + zero;
        frameData.m_position += frameData.m_velocity * 0.5f;
        frameData.m_swayVelocity = frameData.m_swayVelocity * (1f - this.m_info.m_dampers) - a2 * (1f - this.m_info.m_springs) - frameData.m_swayPosition * this.m_info.m_springs;
        frameData.m_swayPosition += frameData.m_swayVelocity * 0.5f;
        frameData.m_steerAngle = num14;
        frameData.m_travelDistance += vector3.z;
        frameData.m_lightIntensity.x = 5f;
        frameData.m_lightIntensity.y = ((a2.z >= -0.1f) ? 0.5f : 5f);
        frameData.m_lightIntensity.z = ((num14 >= -0.1f || !flag7) ? 0f : 5f);
        frameData.m_lightIntensity.w = ((num14 <= 0.1f || !flag7) ? 0f : 5f);
        frameData.m_underground = (vehicleData.m_flags.IsFlagSet(Vehicle.Flags.Underground));
        frameData.m_transition = (vehicleData.m_flags.IsFlagSet(Vehicle.Flags.Transition));
        if (vehicleData.m_flags.IsFlagSet(Vehicle.Flags.Parking) && posdiff0_lenxz <= 1f && shortTravelDistanceButStillMoving) {
            Vector3 forward = vehicleData.m_targetPos1 - vehicleData.m_targetPos0;
            if (forward.sqrMagnitude > 0.01f) {
                frameData.m_rotation = Quaternion.LookRotation(forward);
            }
        } else if (num13 > 0.1f) {
            if (vector5.sqrMagnitude > 0.01f) {
                frameData.m_rotation = Quaternion.LookRotation(vector5);
            }
        } else if (num13 < -0.1f && vector5.sqrMagnitude > 0.01f) {
            frameData.m_rotation = Quaternion.LookRotation(-vector5);
        }
        base.SimulationStep(vehicleID, ref vehicleData, ref frameData, leaderID, ref leaderData, lodPhysics);
    }
}
