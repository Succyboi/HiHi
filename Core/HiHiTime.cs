using HiHi.Serialization;
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
    public static class HiHiTime {
        public static long StartedAtUTCTick { get; private set; }
        public static uint Tick { get; private set; }
        public static float DeltaTime { get; private set; }
        public static float Time { get; private set; }
        public static float RealTime => (currentUTCTick - StartedAtUTCTick) / (float)TimeSpan.TicksPerSecond;

        private static long currentUTCTick => DateTime.UtcNow.Ticks;

        public static void AdvanceTick(float deltaTime) {
            DeltaTime = deltaTime;
            Tick++;
            Time += DeltaTime;
        }

        public static void Reset() => Set(currentUTCTick, 0, 0f);
        public static void SetRemote(long remoteStartTime, long remoteSendTime, uint remotePassedTicks) {
            if(remoteStartTime >= StartedAtUTCTick) { // Earlier start priority
                return;
            }

            long remoteTimeSinceStart = remoteSendTime - remoteStartTime;
            long localTimeSinceStart = currentUTCTick - remoteStartTime;
            long remoteTimePerTick = remoteTimeSinceStart / remotePassedTicks;
            long timeBetweenLocalAndRemote = localTimeSinceStart - remoteTimeSinceStart;
            float deltaTime = remoteTimePerTick / (float)TimeSpan.TicksPerSecond;

            uint tick = remotePassedTicks > 0 
                ? (uint)(localTimeSinceStart / remoteTimePerTick)
                : 0;
            float time = tick * deltaTime;

            Set(remoteStartTime, tick, time);
        }
        public static void Set(long startedAtUTCTick, uint tick, float time) {
            StartedAtUTCTick = startedAtUTCTick;
            Tick = tick;
            Time = time;
        }

        public static void Serialize(BitBuffer buffer) {
            buffer.AddLong(StartedAtUTCTick);
            buffer.AddLong(currentUTCTick);
            buffer.AddUInt(Tick);
        }

        public static void Deserialize(BitBuffer buffer) {
            long remoteStartTime = buffer.ReadLong();
            long remoteSendTime = buffer.ReadLong();
            uint remotePassedTicks = buffer.ReadUInt();

            SetRemote(remoteStartTime, remoteSendTime, remotePassedTicks);
        }
    }
}
