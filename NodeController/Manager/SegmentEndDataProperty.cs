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

        /// <summary>
        /// I found no other way of overriding ShouldShow(NodeData)
        /// </summary>
        /// <param name="nodeData"></param>
        /// <returns></returns>
        public static SegmentEndDataProperty<TName, TVal> FirstAt(NodeData nodeData)
            => fieldRef_(nodeData.IterateSegmentEndDatas().First());

        public static void SetValue(NodeData nodeData, TVal newValue) {
            foreach (var segmentEndData in nodeData.IterateSegmentEndDatas()) {
                GetAt(segmentEndData).Value = newValue;
            }
        }

        // tests if all segment ends attached to this nodes have the same value or not
        // node panel dispalys the average value of non-unifrom items with a yellow highlight.
        public static bool IsUniform(NodeData nodeData) =>
            nodeData.IterateSegmentEndDatas().AllEqual(item => GetAt(item).Value);

        // shortchuts:
        public static bool ShouldShow0(SegmentEndData segmentEndData) =>
            GetAt(segmentEndData).ShouldShow(segmentEndData);
        public static bool ShouldShow0(NodeData nodeData) =>
            FirstAt(nodeData).ShouldShow(nodeData);
        public static bool CanModify0(SegmentEndData segmentEndData) =>
            GetAt(segmentEndData).CanModify(segmentEndData);
        #endregion
        public TVal Value;

        // useful when removing or reseting data
        public virtual TVal DefaultValue => default(TVal);

        // should we show this toggle in segmentEnd panel?
        public abstract bool ShouldShow(SegmentEndData segmentEndData);


        // should we show this toggle in node panel?
        public virtual bool ShouldShow(NodeData nodeData) =>
            nodeData.IterateSegmentEndDatas().Any(item=> ShouldShow0(item));

        // can this value be modified? if not it should be reset to DefaultValue
        public abstract bool CanModify(SegmentEndData segmentEndData);
    }

    public abstract class SegmentEndDataPropertyToggle<TName> : SegmentEndDataProperty<TName, bool> {
        // node panel dispalys the average value of non-unifrom items with a yellow highlight.
        public static bool GetValue(NodeData nodeData) {
            return nodeData.IterateSegmentEndDatas().Any(item => GetAt(item).Value);
        }
    }

    public abstract class SegmentEndDataPropertyFloat<TName> : SegmentEndDataProperty<TName, float> {
        // node panel dispalys the average value of non-unifrom items with a yellow highlight.
        public static float GetValue(NodeData nodeData) {
            return nodeData.IterateSegmentEndDatas().Average(item => GetAt(item).Value);
        }
    }
}
