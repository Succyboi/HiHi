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
    public partial struct HiHiVector3 {
        // Adds two vectors.
        public static HiHiVector3 operator +(HiHiVector3 a, HiHiVector3 b) { return new HiHiVector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z); }
        // Subtracts one vector from another.
        public static HiHiVector3 operator -(HiHiVector3 a, HiHiVector3 b) { return new HiHiVector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }
        // Negates a vector.
        public static HiHiVector3 operator -(HiHiVector3 a) { return new HiHiVector3(-a.X, -a.Y, -a.Z); }
        // Multiplies a vector by a number.
        public static HiHiVector3 operator *(HiHiVector3 a, float d) { return new HiHiVector3(a.X * d, a.Y * d, a.Z * d); }
        // Multiplies a vector by a number.
        public static HiHiVector3 operator *(float d, HiHiVector3 a) { return new HiHiVector3(a.X * d, a.Y * d, a.Z * d); }
        // Divides a vector by a number.
        public static HiHiVector3 operator /(HiHiVector3 a, float d) { return new HiHiVector3(a.X / d, a.Y / d, a.Z / d); }

        public static HiHiVector3 Lerp(HiHiVector3 a, HiHiVector3 b, float t) {
            t = Math.Clamp(t, 0f, 1f);
            return new HiHiVector3(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t
            );
        }

        // Linearly interpolates between two vectors without clamping the interpolant
        public static HiHiVector3 LerpUnclamped(HiHiVector3 a, HiHiVector3 b, float t) {
            return new HiHiVector3(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t
            );
        }

        // Returns the distance between /a/ and /b/.
        public static float Distance(HiHiVector3 a, HiHiVector3 b) {
            float diffX = a.X - b.X;
            float diffY = a.Y - b.Y;
            float diffZ = a.Z - b.Z;
            return (float)Math.Sqrt(diffX * diffX + diffY * diffY + diffZ * diffZ);
        }

        // Gradually changes a vector towards a desired goal over time.
        public static HiHiVector3 SmoothDamp(HiHiVector3 current, HiHiVector3 target, ref HiHiVector3 currentVelocity, float smoothTime, float deltaTime, float maxSpeed = float.PositiveInfinity) {
            float output_x = 0f;
            float output_y = 0f;
            float output_z = 0f;

            // Based on Game Programming Gems 4 Chapter 1.10
            smoothTime = Math.Max(0.0001F, smoothTime);
            float omega = 2F / smoothTime;

            float x = omega * deltaTime;
            float exp = 1F / (1F + x + 0.48F * x * x + 0.235F * x * x * x);

            float change_x = current.X - target.X;
            float change_y = current.Y - target.Y;
            float change_z = current.Z - target.Z;
            HiHiVector3 originalTo = target;

            // Clamp maximum speed
            float maxChange = maxSpeed * smoothTime;

            float maxChangeSq = maxChange * maxChange;
            float sqrmag = change_x * change_x + change_y * change_y + change_z * change_z;
            if (sqrmag > maxChangeSq) {
                var mag = (float)Math.Sqrt(sqrmag);
                change_x = change_x / mag * maxChange;
                change_y = change_y / mag * maxChange;
                change_z = change_z / mag * maxChange;
            }

            target.X = current.X - change_x;
            target.Y = current.Y - change_y;
            target.Z = current.Z - change_z;

            float temp_x = (currentVelocity.X + omega * change_x) * deltaTime;
            float temp_y = (currentVelocity.Y + omega * change_y) * deltaTime;
            float temp_z = (currentVelocity.Z + omega * change_z) * deltaTime;

            currentVelocity.X = (currentVelocity.X - omega * temp_x) * exp;
            currentVelocity.Y = (currentVelocity.Y - omega * temp_y) * exp;
            currentVelocity.Z = (currentVelocity.Z - omega * temp_z) * exp;

            output_x = target.X + (change_x + temp_x) * exp;
            output_y = target.Y + (change_y + temp_y) * exp;
            output_z = target.Z + (change_z + temp_z) * exp;

            // Prevent overshooting
            float origMinusCurrent_x = originalTo.X - current.X;
            float origMinusCurrent_y = originalTo.Y - current.Y;
            float origMinusCurrent_z = originalTo.Z - current.Z;
            float outMinusOrig_x = output_x - originalTo.X;
            float outMinusOrig_y = output_y - originalTo.Y;
            float outMinusOrig_z = output_z - originalTo.Z;

            if (origMinusCurrent_x * outMinusOrig_x + origMinusCurrent_y * outMinusOrig_y + origMinusCurrent_z * outMinusOrig_z > 0) {
                output_x = originalTo.X;
                output_y = originalTo.Y;
                output_z = originalTo.Z;

                currentVelocity.X = (output_x - originalTo.X) / deltaTime;
                currentVelocity.Y = (output_y - originalTo.Y) / deltaTime;
                currentVelocity.Z = (output_z - originalTo.Z) / deltaTime;
            }

            return new HiHiVector3(output_x, output_y, output_z);
        }
    }
}