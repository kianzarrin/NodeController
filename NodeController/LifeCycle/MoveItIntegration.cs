using KianCommons;
using MoveItIntegration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Networking.Types;

namespace NodeController.LifeCycle {
    [Serializable]
    public class MoveItSegmentData {
        public SegmentEndData Start;
        public SegmentEndData End;
        public override string ToString() => $"MoveItSegmentData(Start={Start} End={End})";
    }

    public class MoveItIntegrationFactory : IMoveItIntegrationFactory {
        public string Name => throw new NotImplementedException();
        public string Description => throw new NotImplementedException();
        public IMoveItIntegration GetInstance() => new MoveItIntegration();
    }

    public class MoveItIntegration : IMoveItIntegration {
        static NodeManager nodeMan => NodeManager.Instance;
        static SegmentEndManager segEndMan => SegmentEndManager.Instance;

        public string ID => "CS.Kian.NodeController";

        public Version DataVersion => new Version(2,0);

        public object Decode64(string base64Data, Version dataVersion) {
            Log.Debug($"MoveItIntegration.Decode64({base64Data},{dataVersion}) was called");
            if (base64Data == null || base64Data.Length == 0) return null;
            byte [] data = Convert.FromBase64String(base64Data);
            return SerializationUtil.Deserialize(data).LogRet("MoveItIntegration.Decode64 ->");
        }

        public string Encode64(object record) {
            Log.Debug($"MoveItIntegration.Encode64({record}) was called");
            var data = SerializationUtil.Serialize(record);
            if (data == null || data.Length == 0) return null;
            return Convert.ToBase64String(data).LogRet("MoveItIntegration.Encode64 ->");
        }

        public object CopyNode(ushort sourceNodeID) {
            return NodeManager.Instance.buffer[sourceNodeID]?.Clone()
                .LogRet($"MoveItIntegration.CopyNode({sourceNodeID}) -> ");
        }

        public object CopySegment(ushort sourceSegmentID) {
            var ret = new MoveItSegmentData {
                Start = segEndMan.GetAt(segmentID: sourceSegmentID, startNode: true)?.Clone(),
                End = segEndMan.GetAt(segmentID: sourceSegmentID, startNode: false)?.Clone()
            };
            if (ret.Start == null && ret.End == null)
                return null;

            return ret.LogRet($"MoveItIntegration.CopySegment({sourceSegmentID}) -> ");
        }

        public void PasteNode(ushort targetNodeID, object record, Dictionary<InstanceID, InstanceID> map) {
            Log.Debug($"MoveItIntegration.PasteNode({targetNodeID}) called with record = " + record);
            if (record == null) {
                //nodeMan.ResetNodeToDefault(nodeID); // doing this is not backward comaptible
            } else {
                nodeMan.buffer[targetNodeID] = (NodeData)record;
                nodeMan.buffer[targetNodeID].NodeID = targetNodeID;

                // Do not call refresh here as it might restart node to 0 even though corner offsets from
                // segments may come in later.
                // after cloning is complete, everything will be updated.
                //nodeMan.RefreshData(targetNodeID);
            }
        }

        public void PasteSegment(ushort targetSegmentID, object record, Dictionary<InstanceID, InstanceID> map) {
            Log.Debug($"MoveItIntegration.PasteSegment({targetSegmentID}) called with record = " + record);
            if (record == null) {
                // doing this is not backward comatible:
                //segEndMan.ResetSegmentEndToDefault(segmentId, true); 
                //segEndMan.ResetSegmentEndToDefault(segmentId, false);
            } else {
                var data = (MoveItSegmentData)record;
                if (data.Start != null) {
                    data.Start.SegmentID = targetSegmentID;
                    data.Start.NodeID = targetSegmentID.ToSegment().m_startNode;
                }
                segEndMan.SetAt(targetSegmentID, true, data.Start);

                if (data.End != null) {
                    data.End.SegmentID = targetSegmentID;
                    data.End.NodeID = targetSegmentID.ToSegment().m_endNode;
                }
                segEndMan.SetAt(targetSegmentID, false, data.End);
            }
        }
    }
}
