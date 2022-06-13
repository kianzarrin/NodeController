namespace NodeController.Manager {
    using KianCommons;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using TrafficManager.API.Notifier;

    public class LaneCache {
        public static LaneCache Instance { get; private set;}
        private struct LaneData {
            public bool HideArrows;
        }
        private LaneData[] buffer = new LaneData[NetManager.MAX_LANE_COUNT];
        static INotifier Notifier => TrafficManager.API.Implementations.Notifier;

        public static void Create() => Instance = new LaneCache();
        public static void Ensure() => Instance ??= new LaneCache();
        public void Release() {
            Notifier.EventModified -= Notifier_EventModified;
            Instance = null;
        }
        public bool ShouldHideArrows(uint laneId) => buffer[laneId].HideArrows;

        private void Notifier_EventModified(OnModifiedEventArgs obj) {
            SimulationManager.instance.m_ThreadingWrapper.QueueSimulationThread(delegate () {
                Log.Called(obj.InstanceID);
                if (obj.InstanceID.Type == InstanceType.NetSegment) {
                    UpdateLanes(obj.InstanceID.NetSegment);
                } else if (obj.InstanceID.Type == InstanceType.NetLane) {
                    UpdateLanes(obj.InstanceID.NetLane.ToLane().m_segment);
                } else if(obj.InstanceID.Type == InstanceType.NetNode) {
                    foreach(ushort segmentId in obj.InstanceID.NetNode.ToNode().IterateSegments()) {
                        UpdateLanes(segmentId);
                    }
                }
            });
        }

        public void OnTMPELoaded() {
            SimulationManager.instance.m_ThreadingWrapper.QueueSimulationThread(delegate () {
                Log.Called();
                for (ushort segmentId = 1; segmentId < NetManager.MAX_SEGMENT_COUNT; ++segmentId) {
                    UpdateLanes(segmentId);
                }
                Notifier.EventModified -= Notifier_EventModified;
                Notifier.EventModified += Notifier_EventModified;
                Log.Succeeded();
            });
        }

        public void UpdateLanes(ushort segmentID) {
            if (!NetUtil.IsSegmentValid(segmentID)) {
                return;
            }
            UpdateLanes(segmentID, false);
            UpdateLanes(segmentID, true);
        }
        private void UpdateLanes(ushort segmentID, bool startNode) {
            var lanes = NetUtil.IterateLanes(segmentID, startNode: startNode).ToArray();
            bool twoSegments = TwoSegments(segmentID.ToSegment().GetNode(startNode));
            bool hide;

            if (twoSegments) {
                NetLane.Flags flags = 0;
                foreach (var lane in lanes) {
                    flags |= lane.Flags;
                }
                bool allForward = (flags & NetLane.Flags.LeftForwardRight) == NetLane.Flags.Forward;
                hide = allForward;
            } else {
                hide = false;
            }
            foreach(var lane in lanes) {
                buffer[lane.LaneID].HideArrows = hide;
            }
        }
        private static bool TwoSegments(ushort nodeId) {
            ref NetNode node = ref nodeId.ToNode();
            int nSegments = 0;
            for (int segmentIndex = 0; segmentIndex < 8; ++segmentIndex) {
                ushort segmentId2 = node.GetSegment(segmentIndex);
                if (segmentId2 > 0)
                    nSegments++;
                if (nSegments >= 3)
                    return false;
            }
            return nSegments == 2;
        }

    }
}
