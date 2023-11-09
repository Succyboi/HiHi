using System;

namespace HiHi {
    public partial struct HiHiVector2 {
        public static HiHiVector2 Lerp(HiHiVector2 a, HiHiVector2 b, float t) {
            t = Math.Clamp(t, 0f, 1f);
            return new HiHiVector2(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t
            );
        }

        public static HiHiVector2 SmoothDamp(HiHiVector2 current, HiHiVector2 target, ref HiHiVector2 currentVelocity, float smoothTime, float deltaTime, float maxSpeed = float.PositiveInfinity) {
            // Based on Game Programming Gems 4 Chapter 1.10
            smoothTime = Math.Max(0.0001F, smoothTime);
            float omega = 2F / smoothTime;

            float x = omega * deltaTime;
            float exp = 1F / (1F + x + 0.48F * x * x + 0.235F * x * x * x);

            float change_x = current.X - target.X;
            float change_y = current.Y - target.Y;
            HiHiVector2 originalTo = target;

            // Clamp maximum speed
            float maxChange = maxSpeed * smoothTime;

            float maxChangeSq = maxChange * maxChange;
            float sqDist = change_x * change_x + change_y * change_y;
            if (sqDist > maxChangeSq) {
                var mag = (float)Math.Sqrt(sqDist);
                change_x = change_x / mag * maxChange;
                change_y = change_y / mag * maxChange;
            }

            target.X = current.X - change_x;
            target.Y = current.Y - change_y;

            float temp_x = (currentVelocity.X + omega * change_x) * deltaTime;
            float temp_y = (currentVelocity.Y + omega * change_y) * deltaTime;

            currentVelocity.X = (currentVelocity.X - omega * temp_x) * exp;
            currentVelocity.Y = (currentVelocity.Y - omega * temp_y) * exp;

            float output_x = target.X + (change_x + temp_x) * exp;
            float output_y = target.Y + (change_y + temp_y) * exp;

            // Prevent overshooting
            float origMinusCurrent_x = originalTo.X - current.X;
            float origMinusCurrent_y = originalTo.Y - current.Y;
            float outMinusOrig_x = output_x - originalTo.X;
            float outMinusOrig_y = output_y - originalTo.Y;

            if (origMinusCurrent_x * outMinusOrig_x + origMinusCurrent_y * outMinusOrig_y > 0) {
                output_x = originalTo.X;
                output_y = originalTo.Y;

                currentVelocity.X = (output_x - originalTo.X) / deltaTime;
                currentVelocity.Y = (output_y - originalTo.Y) / deltaTime;
            }
            return new HiHiVector2(output_x, output_y);
        }
    }
}