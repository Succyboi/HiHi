using System.Net;
using System.Net.Sockets;
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
    public class UDPTransport : PeerTransport {
        public const int MAX_PACKET_SIZE = ushort.MaxValue - 8 /*UDP header*/ - 20 /*IPv4 header*/;

        public override int MaxPacketSize => MAX_PACKET_SIZE;
        public override string LocalEndPoint { 
            get {
                return HiHiUtility.ToEndPointString(client.Client.LocalEndPoint as IPEndPoint);
            }
        }
        public override string LocalAddress => (client.Client.LocalEndPoint as IPEndPoint).Address.ToString();
        public override int Port => (client.Client.LocalEndPoint as IPEndPoint).Port;

        private const int PREFERRED_RECEIVE_PORT = HiHiConfiguration.BROADCAST_RECEIVE_PORT;

        private UdpClient client;
        private IPEndPoint outgoingRemoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
        private IPEndPoint incomingRemoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
        private byte[] incomingBuffer;
        private byte[] outgoingBuffer;

        public UDPTransport(int preferredPort = PREFERRED_RECEIVE_PORT) : base() {
            client = new UdpClient(HiHiUtility.GetFreePort(preferredPort));
            outgoingBuffer = new byte[MAX_PACKET_SIZE];
        }

        public override void Start() {
            base.Start();
        }

        public override void Stop() {
            base.Stop();
        }

        protected override void ReceiveIncomingMessages() {
            client.EnableBroadcast = ReceiveBroadcast;

            while (client.Available > 0) {
                try {
                    incomingBuffer = client.Receive(ref incomingRemoteEndpoint);
                } 
                catch (SocketException) { continue; }

                if (!PeerMessage.ContainsValidHeader(incomingBuffer)) { continue; }

                PeerMessage message = PeerMessage.Borrow(incomingRemoteEndpoint.ToEndPointString(), incomingBuffer, incomingBuffer.Length);
                IncomingMessages.Enqueue(message);
            }
        }

        protected override void SendOutgoingMessages() {
            while (!OutgoingMessages.IsEmpty) {
                if (!OutgoingMessages.TryDequeue(out PeerMessage message)) { continue; }
                int bufferLength = message.Buffer.ToArray(outgoingBuffer);

                if (!PeerMessage.ContainsValidHeader(outgoingBuffer)) {
                    message.Return();
                    throw new HiHiException($"Produced buffer with invalid header.");
                }

                if(bufferLength > MAX_PACKET_SIZE) {
                    message.Return();
                    throw new HiHiException($"Buffer length exceeded {nameof(MAX_PACKET_SIZE)} ({bufferLength} vs {MAX_PACKET_SIZE}).");
                }

                if (message.DestinationAll) {
                    foreach(ushort peerID in Peer.Network.PeerIDs) {
                        if (!Peer.Network.Contains(peerID)) { continue; }
                        if (!IPEndPoint.TryParse(Peer.Network[peerID].EndPoint, out outgoingRemoteEndpoint)) { continue; }

                        try {
                            client.Send(outgoingBuffer, bufferLength, outgoingRemoteEndpoint);
                        }
                        catch (SocketException) { continue; }
                    }
                }
                else {
                    outgoingRemoteEndpoint = HiHiUtility.ParseStringToIPEndpoint(message.DestinationEndPoint);

                    try {
                        client.Send(outgoingBuffer, bufferLength, outgoingRemoteEndpoint);
                    }
                    catch (SocketException) { continue; }
                }

                message.Return();
            }
        }
    }
}
