namespace NodeController.Manager {
    using System;
    using System.Linq;
    using KianCommons;
    using HarmonyLib;

    [Serializable]
    public abstract class SegmentEndDataProperty<TName, TVal> where TVal : struct {
        #region static
        [NonSerialized]
        private static AccessTools.FieldRef<SegmentEndData, SegmentEndDataProperty<TName, TVal>> fieldRef_;
        static SegmentEndDataProperty() {
            fieldRef_ = AccessTools.FieldRefAccess<SegmentEndData, SegmentEndDataProperty<TName, TVal>>(nameof(TName));
        }
        public static SegmentEndDataProperty<TName, TVal> GetAt(SegmentEndData segmentEndData)
            => fieldRef_(segmentEndData);
        public static SegmentEndDataProperty<TName, TVal> FirstAt(NodeData nodeData)
            => fieldRef_(nodeData.IterateSegmentEndDatas().First());

        public static void SetValue(NodeData nodeData, TVal newValue) {
            foreach (var segmentEndData in nodeData.IterateSegmentEndDatas()) {
                GetAt(segmentEndData).Value = newValue;
            }
        }

        public static bool IsUniform(NodeData nodeData) =>
            nodeData.IterateSegmentEndDatas().AllEqual(item => GetAt(item).Value);

        public static bool ShouldShow0(SegmentEndData segmentEndData) =>
            GetAt(segmentEndData).ShouldShow(segmentEndData);
        public static bool ShouldShow0(NodeData nodeData) =>
            FirstAt(nodeData).ShouldShow(nodeData);
        public static bool CanModify0(SegmentEndData segmentEndData) =>
            GetAt(segmentEndData).CanModify(segmentEndData);
        #endregion
        public TVal Value;

        public virtual TVal DefaultValue => default(TVal);

        public abstract bool ShouldShow(SegmentEndData segmentEndData);
        public abstract bool ShouldShow(NodeData nodeData);
        public abstract bool CanModify(SegmentEndData segmentEndData);
    }

    public abstract class SegmentEndDataPropertyToggle<TName> : SegmentEndDataProperty<TName, bool> {
        public static bool GetValue(NodeData nodeData) {
            return nodeData.IterateSegmentEndDatas().Any(item => GetAt(item).Value);
        }
    }

    public abstract class SegmentEndDataPropertyFloat<TName> : SegmentEndDataProperty<TName, float> {
        public static float GetValue(NodeData nodeData) {
            return nodeData.IterateSegmentEndDatas().Average(item => GetAt(item).Value);
        }
    }
}
