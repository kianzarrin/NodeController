namespace Kian.Math {
    using UnityEngine;

    public struct PointDir3 {
        public Vector3 Point;
        public Vector3 Dir;
        public PointDir3(Vector3 point, Vector3 dir) {
            Point = point;
            Dir = dir;
        }
        public PointDir3 Reverse => new PointDir3(Point, -Dir);
    }
}
