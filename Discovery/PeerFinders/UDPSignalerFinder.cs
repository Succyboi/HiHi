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
namespace HiHi.Discovery {
    public class UDPSignalerFinder : PeerFinder {
        public string Address { get; set; }
        public int Port { get; set; }
        public string EndPoint => HiHiUtility.ToEndPointString(Address, Port);

        protected override int FindRoutineIntervalMS => HiHiConfiguration.SIGNALER_HEARTBEAT_SEND_INTERVAL_MS;

        public UDPSignalerFinder(string address, int port) : base() {
            this.Address = address;
            this.Port = port;
        }

        public override void Start() {
            if (Running) { return; }

            Peer.Transport.ReceiveBroadcast = true;

            base.Start();
        }

        public override void Stop() {
            if (!Running) { return; }

            SendDisconnect();
            Peer.Transport.ReceiveBroadcast = false;

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

        private void SendVerificationRequest() {
            PeerMessage message = PeerMessage.Borrow(PeerMessageType.VerifiedPeerInfoRequest, default, EndPoint);
            Peer.Info.Serialize(message.Buffer);

            Peer.Transport.Send(message);
        }

        private void SendRemotePeerInfoRequest() {
            PeerMessage message = PeerMessage.Borrow(PeerMessageType.RemotePeerInfoRequest, default, EndPoint);

            Peer.Transport.Send(message);
        }

        private void SendHeartBeat() {
            PeerMessage message = PeerMessage.Borrow(PeerMessageType.HeartBeat, default, EndPoint);

            Peer.Transport.Send(message);
        }

        private void SendDisconnect() {
            PeerMessage message = PeerMessage.Borrow(PeerMessageType.Disconnect, default, EndPoint);

            Peer.Transport.Send(message);
        }
    }
}
