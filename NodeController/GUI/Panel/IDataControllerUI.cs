namespace NodeController.GUI {
    public enum NetworkTypeT {
        None,
        Node,
        Segment,
        SegmentEnd,
        Lane,
    }

    public interface IDataControllerUI {
        void Apply();
        void Refresh();
        NetworkTypeT NetworkType { get; }
    }
}
