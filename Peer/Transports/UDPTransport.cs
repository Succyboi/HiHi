using System.Net;
using System.Net.Sockets;
using HiHi.Common;
using HiHi.Serialization;

namespace HiHi {
    public class UDPTransport : PeerTransport {
        public const int MAX_PACKET_SIZE = ushort.MaxValue - 8 /*UDP header*/ - 20 /*IPv4 header*/;

        public override int MaxPacketSize => MAX_PACKET_SIZE;
        public override string LocalIPEndPoint { 
            get { 
                if (HiHiUtility.TryGetLocalIPAddress(out string localIp)) {
                    return $"{localIp}:{(client.Client.LocalEndPoint as IPEndPoint).Port}";
                }

                return string.Empty;
            } 
        }

        private const int PREFERRED_RECEIVE_PORT = HiHiConfiguration.BROADCAST_RECEIVE_PORT;

        private UdpClient client;
        private IPEndPoint? outgoingRemoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
        private IPEndPoint? incomingRemoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
        private readonly object clientLock = new object();
        private byte[] outgoingBuffer;

        public UDPTransport() : base() {
            client = new UdpClient(HiHiUtility.GetFreePort(PREFERRED_RECEIVE_PORT));

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
                byte[] incomingBuffer;
                lock (clientLock) {
                    incomingBuffer = client.Receive(ref incomingRemoteEndpoint);
                }

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

                        lock (clientLock) {
                            client.Send(outgoingBuffer, bufferLength, outgoingRemoteEndpoint);
                        }
                    }
                }
                else {
                    if (!Peer.Network.Contains(message.DestinationPeerID)) { continue; }
                    if (!IPEndPoint.TryParse(Peer.Network[message.DestinationPeerID].EndPoint, out outgoingRemoteEndpoint)) { continue; }

                    lock (clientLock) {
                        client.Send(outgoingBuffer, bufferLength, outgoingRemoteEndpoint);
                    }
                }

                message.Return();
            }
        }
    }
}
