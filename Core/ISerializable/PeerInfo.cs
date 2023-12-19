using System;
using System.Drawing;
using HiHi.Common;
using HiHi.Serialization;

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
	[Serializable]
	public partial class PeerInfo : ISerializable {
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
		public string ConnectionKey { get; set; }
		public string RemoteEndPoint {
			get => remoteEndPoint;
			set => remoteEndPoint = value;
		}
		public string LocalEndPoint {
			get => localEndPoint;
			set => localEndPoint = value;
        }
		public double Hue => UniqueID / (double)ushort.MaxValue;
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

		private string remoteEndPoint = string.Empty;
		private string localEndPoint = string.Empty;
		private ushort? uniqueID;
		private Color? color;

		public PeerInfo() {
            RegisterHeartbeat();
		}

		public static PeerInfo CreateLocal() {
			PeerInfo peerInfo = new PeerInfo();

            peerInfo.SelfAssignedID = (ushort)random.Next(ushort.MaxValue);
            peerInfo.ConnectionKey = Peer.ConnectionKey;
            peerInfo.LocalEndPoint = Peer.Transport.LocalEndPoint;

			return peerInfo;
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

		public void Verify(ushort uniqueID, string remoteEndpoint) {
			this.UniqueID = uniqueID;
			this.RemoteEndPoint = remoteEndpoint;

			this.Verified = true;
		}

		public void RerollSelfAssignedID() {
            SelfAssignedID = (ushort)random.Next(ushort.MaxValue);
        }

        void ISerializable.Serialize(BitBuffer buffer) {
			buffer.AddUShort(UniqueID);
			buffer.AddString(ConnectionKey);
			buffer.AddString(RemoteEndPoint);
			buffer.AddString(LocalEndPoint);
			buffer.AddBool(Verified);
		}

		void ISerializable.Deserialize(BitBuffer buffer) {
			UniqueID = buffer.ReadUShort();
			ConnectionKey = buffer.ReadString();
			RemoteEndPoint = buffer.ReadString();
			LocalEndPoint = buffer.ReadString();
			Verified = buffer.ReadBool();
		}

		public override string ToString() {
			return $"{nameof(UniqueID)} = {UniqueID}, {nameof(ConnectionKey)} = {ConnectionKey}, {nameof(RemoteEndPoint)} = {RemoteEndPoint}, {nameof(LocalEndPoint)} = {LocalEndPoint}, {nameof(Verified)} = {Verified}";
		}
	}
}
