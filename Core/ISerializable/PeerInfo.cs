using System;
using System.Drawing;
using HiHi.Serialization;

namespace HiHi {
    [Serializable]
    public class PeerInfo : ISerializable {
        private static Random random = new Random();

        public bool ExpectingHeartbeat => Environment.TickCount - HeartbeatTick > HiHiConfiguration.HEARTBEAT_SEND_INTERVAL_MS;
        public bool HeartbeatTimedOut => Environment.TickCount - HeartbeatTick > HiHiConfiguration.HEARTBEAT_TIMEOUT_INTERVAL_MS;
        public int HeartbeatTick { get; private set; }

        public bool ShouldRequestPing => Environment.TickCount - PingRequestTick > HiHiConfiguration.PING_INTERVAL_MS;
        public int PingRequestTick { get; private set; }
        public float Ping { get; private set; } = -1f;
        public float PingMS => Ping * 1000f;

        public bool Verified { get; set; } = false;
        public ushort UniqueID {
            get {
                return uniqueID ?? SelfAssignedID;
            }
            set {
                uniqueID = value;
                color = null;
            }
        }
        public ushort SelfAssignedID { get; private set; }
        public string GUIDString { get; private set; }
        public string ConnectionKey { get; set; }
        public string EndPoint { 
            get {
                return endPoint;
            }
            set {
                endPoint = value;
            }
        }
        public Color Color {
            get {
                if(color == null) {
                    double hue = UniqueID / (double)ushort.MaxValue * 360d;
                    double saturation = 1f;
                    double lightness = 1f;

                    int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
                    double f = hue / 60 - Math.Floor(hue / 60);

                    lightness = lightness * 255;
                    int v = Convert.ToInt32(lightness);
                    int p = Convert.ToInt32(lightness * (1 - saturation));
                    int q = Convert.ToInt32(lightness * (1 - f * saturation));
                    int t = Convert.ToInt32(lightness * (1 - (1 - f) * saturation));

                    if (hi == 0)
                        return Color.FromArgb(255, v, t, p);
                    else if (hi == 1)
                        return Color.FromArgb(255, q, v, p);
                    else if (hi == 2)
                        return Color.FromArgb(255, p, v, t);
                    else if (hi == 3)
                        return Color.FromArgb(255, p, q, v);
                    else if (hi == 4)
                        return Color.FromArgb(255, t, p, v);
                    else
                        return Color.FromArgb(255, v, p, q);
                }

                return color ?? default;
            }
        }
        public string ColorCode => $"#{Color.R:X2}{Color.G:X2}{Color.B:X2}";

        private string endPoint;
        private ushort? uniqueID;
        private Color? color;

        public PeerInfo() {
            Guid guid = Guid.NewGuid();
            GUIDString = guid.ToString();
            SelfAssignedID = (ushort)random.Next(ushort.MaxValue);
            ConnectionKey = Peer.ConnectionKey;

            RegisterHeartbeat();
        }

        public void RegisterHeartbeat() {
            HeartbeatTick = Environment.TickCount;
        }

        public void RegisterPingRequest() {
            PingRequestTick = Environment.TickCount;
        }

        public void SetPing(float ping) {
            Ping = ping;
        }

        void ISerializable.Serialize(BitBuffer buffer) {
            buffer.AddUShort(UniqueID);
            buffer.AddString(GUIDString);
            buffer.AddString(ConnectionKey);
            buffer.AddString(EndPoint);
        }

        void ISerializable.Deserialize(BitBuffer buffer) {
            uniqueID = buffer.ReadUShort();
            GUIDString = buffer.ReadString();
            ConnectionKey = buffer.ReadString();
            EndPoint = buffer.ReadString();
        }
    }
}