using HiHi.Common;
using HiHi.Serialization;
using HiHi.Signaling;

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
namespace HiHi.Discovery {
    public class SignalerFinder : PeerFinder {

        public string Address { get; set; }
        public int Port { get; set; }
        public string EndPoint => IPUtility.ToEndPointString(Address, Port);
        public int LobbySize { get; set; }

        protected override int FindRoutineIntervalMS => HiHiConfiguration.SIGNALING_HEARTBEAT_SEND_INTERVAL_MS;

        public SignalerFinder(string address, int port, int? lobbySize = null) : base() {
            this.Address = address;
            this.Port = port;
            this.LobbySize = lobbySize ?? HiHiConfiguration.SIGNALING_DEFAULT_LOBBY_SIZE;
        }

        public override void Start() {
            if (Running) { return; }

            base.Start();
        }

        public override void Stop() {
            if (!Running) { return; }

            SendDisconnect();

            base.Stop();
        }

        public override void Find() {
            if (!Peer.Info.Verified) {
                SendVerificationRequest();
                return;
            }

            if (!Peer.Connected) {
                SendRemotePeerInfoRequest();
                return;
            }

            SendHeartBeat();
        }

        public void SendVerificationRequest() {
            PeerMessage message = PeerMessage.BorrowOutgoing(PeerMessageType.VerifiedPeerInfoRequest, default, EndPoint);
            Peer.Info.Serialize(message.Buffer);
            message.Buffer.AddInt(LobbySize);

            Peer.Transport.Send(message);
        }

        private void SendRemotePeerInfoRequest() {
            PeerMessage message = PeerMessage.BorrowOutgoing(PeerMessageType.RemotePeerInfoRequest, default, EndPoint);
            Peer.Info.Serialize(message.Buffer);
            message.Buffer.AddInt(LobbySize);

            Peer.Transport.Send(message);
        }

        private void SendHeartBeat() {
            PeerMessage message = PeerMessage.BorrowOutgoing(PeerMessageType.HeartBeat, default, EndPoint);

            Peer.Transport.Send(message);
        }

        private void SendDisconnect() {
            PeerMessage message = PeerMessage.BorrowOutgoing(PeerMessageType.Disconnect, default, EndPoint);

            Peer.Transport.Send(message);
        }
    }
}
