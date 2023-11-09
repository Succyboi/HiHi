using System;

/*
 * ANTI-CAPITALIST SOFTWARE LICENSE (v 1.4)
 *
 * Copyright © 2023 Pelle Bruinsma
 * 
 * This is anti-capitalist software, released for free use by individuals and organizations that do not operate by capitalist principles.
 *
 * Permission is hereby granted, free of charge, to any person or organization (the "User") obtaining a copy of this software and associated documentation files (the "Software"), to use, copy, modify, merge, distribute, and/or sell copies of the Software, subject to the following conditions:
 * 
 * 1. The above copyright notice and this permission notice shall be included in all copies or modified versions of the Software.
 * 
 * 2. The User is one of the following:
 *    a. An individual person, laboring for themselves
 *    b. A non-profit organization
 *    c. An educational institution
 *    d. An organization that seeks shared profit for all of its members, and allows non-members to set the cost of their labor
 *    
 * 3. If the User is an organization with owners, then all owners are workers and all workers are owners with equal equity and/or equal vote.
 * 
 * 4. If the User is an organization, then the User is not law enforcement or military, or working for or under either.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT EXPRESS OR IMPLIED WARRANTY OF ANY KIND, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
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