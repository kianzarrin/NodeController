using KianCommons;
using MoveItIntegration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeController.LifeCycle {
    [Serializable]
    public class MoveItSegmentData {
        public SegmentEndData Start;
        public SegmentEndData End;
    }

    public class NodeControllerMoveItIntegration : IMoveItIntegration {
        static NodeManager nodeMan => NodeManager.Instance;
        static SegmentEndManager segEndMan => SegmentEndManager.Instance;

        public object CopyNode(ushort nodeID) {
            return NodeManager.Instance.buffer[nodeID]; 
        }

        public object CopySegment(ushort segmentId) {
            return new MoveItSegmentData {
                Start = segEndMan.GetAt(segmentID: segmentId, startNode: true),
                End = segEndMan.GetAt(segmentID: segmentId, startNode: false)
            };
        }

        public object Decode64(string base64Data) {
            if (base64Data == null || base64Data.Length == 0) return null;
            return Convert.FromBase64String(base64Data);
        }

        public string Encode64(object record) {
            var data = SerializationUtil.Serialize(record);
            if (data == null || data.Length == 0) return null;
            return Convert.ToBase64String(data);
        }

        public void PasteNode(ushort nodeID, object record, Dictionary<InstanceID, InstanceID> map) {
            if (record == null) {
                nodeMan.ResetNodeToDefault(nodeID);
            } else {
                nodeMan.buffer[nodeID] = (NodeData)record;
                nodeMan.buffer[nodeID].NodeID = nodeID;
                nodeMan.RefreshData(nodeID);
            }
        }

        public void PasteSegment(ushort segmentId, object record, Dictionary<InstanceID, InstanceID> map) {
            if (record == null) {
                segEndMan.ResetSegmentEndToDefault(segmentId, true);
                segEndMan.ResetSegmentEndToDefault(segmentId, false);
            } else {
                var data = (MoveItSegmentData)record;
                data.Start.SegmentID = data.End.SegmentID = segmentId;
                segEndMan.SetAt(segmentId, true, data.Start);
                segEndMan.SetAt(segmentId, false, data.End);
                segEndMan.RefreshData(segmentId, true);
                segEndMan.RefreshData(segmentId, false);
            }
        }
    }
}
