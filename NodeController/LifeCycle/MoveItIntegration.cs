using KianCommons;
using MoveItIntegration;
using System;
using System.Collections.Generic;
using KianCommons.Serialization;

namespace NodeController.LifeCycle {
    [Serializable]
    public class MoveItSegmentData {
        public MoveItSegmentData Clone() => new MoveItSegmentData { Start = Start, End = End };
        public SegmentEndData Start;
        public SegmentEndData End;
        public override string ToString() => $"MoveItSegmentData(Start={Start} End={End})";
    }

    public class MoveItIntegrationFactory : IMoveItIntegrationFactory {
        public MoveItIntegrationBase GetInstance() => new MoveItIntegration();
    }

    public class MoveItIntegration : MoveItIntegrationBase {
        static NodeManager nodeMan => NodeManager.Instance;
        static SegmentEndManager segEndMan => SegmentEndManager.Instance;

        public override string ID => "CS.Kian.NodeController";

        public override Version DataVersion => new Version(2, 1, 1);

        public override object Decode64(string base64Data, Version dataVersion) {
            Log.Debug($"MoveItIntegration.Decode64({base64Data},{dataVersion}) was called");
            if (base64Data == null || base64Data.Length == 0) return null;
            byte [] data = Convert.FromBase64String(base64Data);
            return SerializationUtil.Deserialize(data, dataVersion).LogRet("MoveItIntegration.Decode64 ->");
        }

        public override string Encode64(object record) {
            Log.Debug($"MoveItIntegration.Encode64({record}) was called");
            var data = SerializationUtil.Serialize(record);
            if (data == null || data.Length == 0) return null;
            return Convert.ToBase64String(data).LogRet("MoveItIntegration.Encode64 ->");
        }

        public override object Copy(InstanceID sourceInstanceID) {
            Log.Debug($"MoveItIntegration.Copy({sourceInstanceID.ToSTR()}) called");
            switch (sourceInstanceID.Type) {
                case InstanceType.NetNode:
                    return CopyNode(sourceInstanceID.NetNode);
                case InstanceType.NetSegment:
                    return CopySegment(sourceInstanceID.NetSegment);
                default:
                    Log.Debug("Unsupported integration");
                    return null;
            }
        }

        public override void Paste(InstanceID targetrInstanceID, object record, Dictionary<InstanceID, InstanceID> map) {
            string strRecord = record == null ? "null" : record.ToString();
            string strInstanceID = targetrInstanceID.ToSTR();
            Log.Debug($"MoveItIntegration.Paste({strInstanceID}, record:{strRecord}, map) was called");
            switch (targetrInstanceID.Type) {
                case InstanceType.NetNode:
                    PasteNode(targetrInstanceID.NetNode, (NodeData)record, map);
                    break;
                case InstanceType.NetSegment:
                    PasteSegment(targetrInstanceID.NetSegment, (MoveItSegmentData)record, map);
                    break;
                default:
                    Log.Debug("Unsupported integration");
                    break;
            }
        }

        public static NodeData CopyNode(ushort sourceNodeID) {
            return NodeManager.Instance.buffer[sourceNodeID]?.Clone()
                .LogRet($"MoveItIntegration.CopyNode({sourceNodeID}) -> ");
        }

        public static MoveItSegmentData CopySegment(ushort sourceSegmentID) {
            var ret = new MoveItSegmentData {
                Start = segEndMan.GetAt(segmentID: sourceSegmentID, startNode: true)?.Clone(),
                End = segEndMan.GetAt(segmentID: sourceSegmentID, startNode: false)?.Clone()
            };
            if (ret.Start == null && ret.End == null)
                return null;

            return ret.LogRet($"MoveItIntegration.CopySegment({sourceSegmentID}) -> ");
        }

        public static void PasteNode(ushort targetNodeID, NodeData record, Dictionary<InstanceID, InstanceID> map) {
            Log.Info($"MoveItIntegration.PasteNode({targetNodeID}) called with record = " + record, true);
            if (record == null) {
                //nodeMan.ResetNodeToDefault(nodeID); // doing this is not backward comaptible
            } else {
                record = record.Clone();
                nodeMan.buffer[targetNodeID] = record;
                nodeMan.buffer[targetNodeID].NodeID = targetNodeID;

                // Do not call refresh here as it might restart node to 0 even though corner offsets from
                // segments may come in later.
                // after cloning is complete, everything will be updated.
                //nodeMan.RefreshData(targetNodeID);
            }
        }

        public static void PasteSegment(
            ushort targetSegmentID, MoveItSegmentData data, Dictionary<InstanceID, InstanceID> map) {
            Log.Debug($"MoveItIntegration.PasteSegment({targetSegmentID}) called with record = " + data);
            if (data == null) {
                // doing this is not backward comatible:
                //segEndMan.ResetSegmentEndToDefault(segmentId, true); 
                //segEndMan.ResetSegmentEndToDefault(segmentId, false);
            } else {
                PasteSegmentEnd(segmentEndData: data.Start, targetSegmentID: targetSegmentID, map: map);
                PasteSegmentEnd(segmentEndData: data.End, targetSegmentID: targetSegmentID, map: map);
            }
        }

        public static void Paste(object record, Dictionary<InstanceID, InstanceID> map) {
            if (record is NodeData nodeData) {
                ushort mappedNodeID = MappedNodeID(map, nodeData.NodeID);
                PasteNode(mappedNodeID, nodeData, map);
            } else if (record is MoveItSegmentData moveItSegmentData) {
                ushort segmentID;
                if (moveItSegmentData.Start != null) {
                    segmentID = moveItSegmentData.Start.SegmentID;
                } else if (moveItSegmentData.End != null) {
                    segmentID = moveItSegmentData.End.SegmentID;
                } else {
                    return;
                }
                ushort mappedSegmentID = MappedSegmentID(map, segmentID);
                PasteSegment(mappedSegmentID, moveItSegmentData, map);
            }
        }

        public static ushort MappedNodeID(Dictionary<InstanceID, InstanceID> map, ushort nodeID) {
            InstanceID instanceID = new InstanceID { NetNode = nodeID };
            if(map.TryGetValue(instanceID, out InstanceID mappedInstanceID)) {
                return mappedInstanceID.NetNode;
            } else {
                throw new Exception($"map does not contian node:{nodeID} map = {map.ToSTR()}");
            }
        }
        public static ushort MappedSegmentID(Dictionary<InstanceID, InstanceID> map, ushort segmentID) {
            InstanceID instanceID = new InstanceID { NetSegment = segmentID };
            if (map.TryGetValue(instanceID, out InstanceID mappedInstanceID)) {
                return mappedInstanceID.NetSegment;
            } else {
                throw new Exception($"map does not contian segment:{segmentID} map = {map.ToSTR()}");
            }
        }

        public static void PasteSegmentEnd(
            SegmentEndData segmentEndData, ushort targetSegmentID, Dictionary<InstanceID, InstanceID> map) {
            if (segmentEndData != null) {
                ushort nodeID = MappedNodeID(map, segmentEndData.NodeID);
                PasteSegmentEnd(
                    segmentEndData: segmentEndData,
                    targetNodeID: nodeID,
                    targetSegmentID: targetSegmentID);
            }
        }

        public static void PasteSegmentEnd(SegmentEndData segmentEndData, ushort targetNodeID, ushort targetSegmentID) {
            Log.Info($"PasteSegmentEnd({segmentEndData}, targetNodeID:{targetNodeID}, targetSegmentID:{targetSegmentID})",true);
            if (segmentEndData != null) {
                segmentEndData = segmentEndData.Clone();
                segmentEndData.SegmentID = targetSegmentID;
                segmentEndData.NodeID = targetNodeID;
            }
            segEndMan.SetAt(
                segmentID: targetSegmentID,
                nodeID: targetNodeID,
                value: segmentEndData);
        }

    }
}
