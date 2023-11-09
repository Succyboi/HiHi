using System;

namespace HiHi {
    public partial struct HiHiQuaternion {
        public static HiHiQuaternion operator *(HiHiQuaternion lhs, HiHiQuaternion rhs) {
            float x = lhs.W * rhs.X + lhs.X * rhs.W + lhs.Y * rhs.Z - lhs.Z * rhs.Y;
            float y = lhs.W * rhs.Y + lhs.Y * rhs.W + lhs.Z * rhs.X - lhs.X * rhs.Z;
            float z = lhs.W * rhs.Z + lhs.Z * rhs.W + lhs.X * rhs.Y - lhs.Y * rhs.X;
            float w = lhs.W * rhs.W - lhs.X * rhs.X - lhs.Y * rhs.Y - lhs.Z * rhs.Z;

            return new HiHiQuaternion(x, y, z, w);
        }

        public static HiHiQuaternion Inverse(HiHiQuaternion q) => new HiHiQuaternion(-q.X, -q.Y, -q.Z, q.W);

        public static float Dot(HiHiQuaternion q, HiHiQuaternion p) => (q.X * p.X) + (q.Y * p.Y) + (q.Z * p.Z) + (q.W * p.W);

        public static HiHiQuaternion Slerpni(HiHiQuaternion from, HiHiQuaternion to, float t) {
            float dot = Dot(from, to);

            if (MathF.Abs(dot) > 0.9999f) {
                return from;
            }

            float theta = MathF.Acos(dot);
            float sinT = 1.0f / MathF.Sin(theta);
            float newFactor = MathF.Sin(t * theta) * sinT;
            float invFactor = MathF.Sin((1.0f - t) * theta) * sinT;

            return new HiHiQuaternion(invFactor * from.X + newFactor * to.X,
                    invFactor * from.Y + newFactor * to.Y,
                    invFactor * from.Z + newFactor * to.Z,
                    invFactor * from.W + newFactor * to.W);
        }
    }
}