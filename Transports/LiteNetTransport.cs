using System.Net;
using System.Collections.Generic;
using LiteNetLib;
using HiHi.Common;

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
    public class LiteNetTransport : PeerTransport {
        public const int MAX_PACKET_SIZE = ushort.MaxValue - 8 /*UDP header*/ - 20 /*IPv4 header*/;

        public override int MaxPacketSize => MAX_PACKET_SIZE;
        public override string LocalEndPoint {
            get {
                return HiHiUtility.ToEndPointString(LocalAddress, LocalPort);
            }
        }
        public override string LocalAddress => HiHiUtility.GetLocalAddressString();
        public override int LocalPort => client.LocalPort;

        private const int PREFERRED_RECEIVE_PORT = HiHiConfiguration.BROADCAST_RECEIVE_PORT;

        private int preferredPort;
        private EventBasedNetListener listener;
        private EventBasedNatPunchListener natListener;
        private NetManager client;
        private Dictionary<IPEndPoint, NetPeer> peers;
        private IPEndPoint outgoingRemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private IPEndPoint incomingRemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private byte[] incomingBuffer;
        private byte[] outgoingBuffer;
        private int outgoingBufferLength;

        public LiteNetTransport(int preferredPort = PREFERRED_RECEIVE_PORT) : base() {
            this.preferredPort = preferredPort;

            listener = new EventBasedNetListener();
            natListener = new EventBasedNatPunchListener();
            client = new NetManager(listener) {
                IPv6Enabled = true,
                NatPunchEnabled = true
            };
            client.NatPunchModule.Init(natListener);
            peers = new Dictionary<IPEndPoint, NetPeer>();

            incomingBuffer = new byte[MAX_PACKET_SIZE];
            outgoingBuffer = new byte[MAX_PACKET_SIZE];
        }

        public override void Start() {
            client.Start(HiHiUtility.GetFreePort(preferredPort));

            listener.ConnectionRequestEvent += HandleConnectionRequest;
            listener.PeerConnectedEvent += HandlePeerConnected;
            listener.PeerDisconnectedEvent += HandlePeerDisconnected;
            listener.NetworkReceiveEvent += HandleNetworkReceive;
            listener.NetworkReceiveUnconnectedEvent += HandleNetworkReceiveUnconnected;

            natListener.NatIntroductionSuccess += HandleNatIntroductionSuccess;

            base.Start();
        }

        public override void Stop() {
            client.Stop();

            listener.ConnectionRequestEvent -= HandleConnectionRequest;
            listener.PeerConnectedEvent -= HandlePeerConnected;
            listener.PeerDisconnectedEvent -= HandlePeerDisconnected;
            listener.NetworkReceiveEvent -= HandleNetworkReceive;
            listener.NetworkReceiveUnconnectedEvent -= HandleNetworkReceiveUnconnected;

            natListener.NatIntroductionSuccess -= HandleNatIntroductionSuccess;

            base.Stop();
        }

        #region Receive

        protected override void ReceiveIncomingMessages() {
            client.BroadcastReceiveEnabled = ReceiveBroadcast;
            client.PollEvents();
            client.NatPunchModule.PollEvents();
        }

        private void HandleConnectionRequest(ConnectionRequest request) {
            request.Accept();
        }

        private void HandlePeerConnected(NetPeer netPeer) {
            if (peers.ContainsKey(netPeer.EndPoint)) { return; }

            peers.Add(netPeer.EndPoint, netPeer);
        }

        private void HandlePeerDisconnected(NetPeer netPeer, DisconnectInfo info) {
            if (!peers.ContainsKey(netPeer.EndPoint)) { return; }

            peers.Remove(netPeer.EndPoint);
        }

        private void HandleNetworkReceive(NetPeer netPeer, NetPacketReader reader, byte channel, DeliveryMethod method) {
            if(reader.IsNull || reader.AvailableBytes <= 0) { return; }

            incomingRemoteEndPoint = netPeer.EndPoint;
            reader.GetBytes(incomingBuffer, 0, reader.AvailableBytes);

            if (!PeerMessage.ContainsValidHeader(incomingBuffer)) { return; }

            PeerMessage message = PeerMessage.Borrow(incomingRemoteEndPoint.ToEndPointString(), incomingBuffer, incomingBuffer.Length);
            IncomingMessages.Enqueue(message);

            reader.Recycle();
        }

        private void HandleNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) {
            if (reader.IsNull || reader.AvailableBytes <= 0) { return; }

            incomingRemoteEndPoint = remoteEndPoint;
            reader.GetBytes(incomingBuffer, 0, reader.AvailableBytes);


            if (!PeerMessage.ContainsValidHeader(incomingBuffer)) { return; }

            PeerMessage message = PeerMessage.Borrow(incomingRemoteEndPoint.ToEndPointString(), incomingBuffer, incomingBuffer.Length);
            IncomingMessages.Enqueue(message);

            reader.Recycle();
        }

        private void HandleNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType natAddressType, string token) {
            client.Connect(targetEndPoint, string.Empty);

            if(!Peer.Initialized || Peer.Transport != this) { return; }

            Peer.Connect(targetEndPoint.ToEndPointString());
        }

        #endregion

        #region Send

        public override void SendBroadcast(PeerMessage message) {
            byte[] outgoingBuffer = new byte[MAX_PACKET_SIZE];
            int outgoingBufferLength = message.Buffer.ToArray(outgoingBuffer);

            client.SendBroadcast(outgoingBuffer, 0, outgoingBufferLength, HiHiConfiguration.BROADCAST_RECEIVE_PORT);
        }

        public override void SendNATIntroduction(string internalEndPointA, string externalEndPointA, string internalEndPointB, string externalEndPointB) {
            if (!HiHiUtility.TryParseStringToIPEndPoint(internalEndPointA, out IPEndPoint internalA)) { return; }
            if (!HiHiUtility.TryParseStringToIPEndPoint(externalEndPointA, out IPEndPoint externalA)) { return; }
            if (!HiHiUtility.TryParseStringToIPEndPoint(internalEndPointB, out IPEndPoint internalB)) { return; }
            if (!HiHiUtility.TryParseStringToIPEndPoint(externalEndPointB, out IPEndPoint externalB)) { return; }

            client.NatPunchModule.NatIntroduce(internalA, externalA, internalB, externalB, string.Empty);
        }

        protected override void SendOutgoingMessages() {
            while (!OutgoingMessages.IsEmpty) {
                if (!OutgoingMessages.TryDequeue(out PeerMessage message)) { continue; }
                outgoingBufferLength = message.Buffer.ToArray(outgoingBuffer);

                if (!PeerMessage.ContainsValidHeader(outgoingBuffer)) {
                    message.Return();
                    throw new HiHiException($"Produced buffer with invalid header.");
                }

                if (outgoingBufferLength > MAX_PACKET_SIZE) {
                    message.Return();
                    throw new HiHiException($"Buffer length exceeded {nameof(MAX_PACKET_SIZE)} ({outgoingBufferLength} vs {MAX_PACKET_SIZE}).");
                }

                bool returnMessage = true;
                if (message.DestinationAll) {
                    foreach (ushort peerID in PeerNetwork.RemotePeerIDs) {
                        if (!PeerNetwork.Contains(peerID)) { continue; }

                        Send(message, PeerNetwork.GetPeerInfo(peerID).RemoteEndPoint, out returnMessage);
                    }
                }
                else {
                    Send(message, message.DestinationEndPoint, out returnMessage);
                }

                if (returnMessage) {
                    message.Return();
                }
            }
        }

        private void Send(PeerMessage message, string endPoint, out bool returnMessage) {
            if (!HiHiUtility.TryParseStringToIPEndPoint(endPoint, out outgoingRemoteEndPoint)) {
                returnMessage = true;
                return;
            }

            if (!peers.ContainsKey(outgoingRemoteEndPoint)) {
                client.Connect(outgoingRemoteEndPoint, string.Empty);

                OutgoingMessages.Enqueue(message);
                returnMessage = false;
                return;
            }

            peers[outgoingRemoteEndPoint].Send(outgoingBuffer, 0, outgoingBufferLength, DeliveryMethod.ReliableOrdered);
            returnMessage = true;
        }

        #endregion
    }
}
