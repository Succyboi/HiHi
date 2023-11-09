using HiHi.Serialization;
using System;

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
