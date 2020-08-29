namespace NodeController {
    public interface INetworkData {
        /// <summary>
        /// marks the network for update in the next simulation step.
        /// once this node is updated, its recalculated.
        /// if the network is default/unsupported/invalid, manager will remove customisation.
        /// </summary>
        void Update();

        /// <summary>
        /// Respond to external changes:
        ///  - calculate new default values. (required by <see cref="IsDefault()"/> and <see cref="ResetToDefault()"/>)
        ///  - refresh node type, values.
        ///  external changes includes:
        ///  - segment added/remvoed
        ///  - MoveIT moves segment/node.
        ///
        /// Call this:
        /// - after initialization
        /// - after CS has calcualted node but before custom modifications has been made
        ///
        /// Note: this does not mark network for update but rather responds to network update.
        /// </summary>
        void Calculate();

        bool IsDefault();

        void ResetToDefault();
    }

    public interface INetworkData<T> {
        T Clone();
    }
}
