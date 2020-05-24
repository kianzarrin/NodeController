using System;
using UnityEngine;

namespace Kian.Math {
    public static class VectorUtil {
        /// <summary>
        /// return value is between 0 to pi. v1 and v2 are interchangable.
        /// </summary>
        public static float UnsignedAngleRad(Vector2 v1, Vector2 v2) {
            //cos(a) = v1.v2 /(|v1||v2|)
            v1.Normalize();
            v2.Normalize();
            //cos(a) = v1.v2
            float dot = Vector2.Dot(v1, v2);
            float angle = Mathf.Acos(dot);
            return angle;
        }

        public static Vector2 RotateRadCCW(this Vector2 v, float angle) =>
            Vector2ByAngleRadCCW(v.magnitude, angle + v.SignedAngleRadCCW());

        /// <param name="angle">angle in rad with Vector.right in CCW direction</param>
        public static Vector2 Vector2ByAngleRadCCW(float magnitude, float angle) {
            return new Vector2(
                x: magnitude * Mathf.Cos(angle),
                y: magnitude * Mathf.Sin(angle)
                );
        }

        /// result is between -pi to +pi. angle is CCW with respect to Vector2.right
        public static float SignedAngleRadCCW(this Vector2 v) {
            v.Normalize();
            return Mathf.Acos(v.x) * Mathf.Sign(v.y);
        }

        public static float Determinent(Vector2 v1, Vector2 v2) =>
            v1.x * v2.y - v1.y * v2.x; // x1*y2 - y1*x2  

        public static Vector2 Vector2ByAgnleRad(float magnitude, float angle) {
            return new Vector2(
                x: magnitude * Mathf.Cos(angle),
                y: magnitude * Mathf.Sin(angle)
                );
        }

        /// <summary>
        /// return value is between -pi to pi. v1 and v2 are not interchangable.
        /// the angle goes CCW from v1 to v2.
        /// eg v1=0,1 v2=1,0 => angle=pi/2
        /// Note: to convert CCW to CW EITHER swap v1 and v2 OR take the negative of the result.
        /// </summary>
        public static float SignedAngleRadCCW(Vector2 v1, Vector2 v2) {
            float dot = Vector2.Dot(v1, v2);
            float det = Determinent(v1, v2);
            float angle = Mathf.Atan2(det, dot);
            return angle;
        }

        public static bool AreApprox180(Vector2 v1, Vector2 v2, float error = MathUtil.Epsilon) {
            float dot = Vector2.Dot(v1, v2);
            return MathUtil.EqualAprox(dot, -1, error);
        }

        public static Vector2 Rotate90CCW(this Vector2 v) => new Vector2(-v.y, +v.x);
        public static Vector2 PerpendicularCCW(this Vector2 v) => v.normalized.Rotate90CCW();
        public static Vector2 Rotate90CW(this Vector2 v) => new Vector2(+v.y, -v.x);
        public static Vector2 PerpendicularCC(this Vector2 v) => v.normalized.Rotate90CW();

        public static Vector2 Extend(this Vector2 v, float magnitude) => NewMagnitude(v, magnitude + v.magnitude);
        public static Vector2 NewMagnitude(this Vector2 v, float magnitude) => magnitude * v.normalized;


        /// returns rotated vector counter clockwise
        ///
        public static Vector3 ToCS3D(this Vector2 v2, float h = 0) => new Vector3(v2.x, h, v2.y);
        public static Vector2 ToCS2D(this Vector3 v3) => new Vector2(v3.x, v3.z);
        public static float Height(this Vector3 v3) => v3.y;



    }
}
