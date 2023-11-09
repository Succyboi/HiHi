using HiHi.Serialization;
using System.Net;
using System.Net.Sockets;

namespace HiHi.Signaling {
    public class UDPBroadcastFinder : PeerFinder {
        public const int BROADCAST_RECEIVE_PORT = HiHiConfiguration.BROADCAST_RECEIVE_PORT;

        private static IPEndPoint BROADCAST_RECEIVE_ENDPOINT = new IPEndPoint(IPAddress.Broadcast, BROADCAST_RECEIVE_PORT);

        private UdpClient broadcastClient;
        private byte[] peerInfoDatagram;
        private int peerInfoDatagramLength;

        public UDPBroadcastFinder() : base() {
            broadcastClient = new UdpClient();
        }

        public override void Start() {
            peerInfoDatagram = new byte[Peer.Transport.MaxPacketSize];
            Peer.Transport.ReceiveBroadcast = true;

            Peer.Info.EndPoint = Peer.Transport.LocalIPEndPoint;
            Peer.Info.Verified = true;

            base.Start();
        }

        public override void Stop() {
            base.Stop();

            Peer.Transport.ReceiveBroadcast = false;
        }

        public override void Find() {
            if (!Peer.Info.Verified) { return; }

            if(Peer.Connected) { return; }

            PeerMessage message = Peer.NewMessage(PeerMessageType.Connect);
            Peer.Info.Serialize(message.Buffer);
            peerInfoDatagramLength = message.Buffer.ToArray(peerInfoDatagram);

            broadcastClient.Send(peerInfoDatagram, peerInfoDatagramLength, BROADCAST_RECEIVE_ENDPOINT);
        }
    }
}
