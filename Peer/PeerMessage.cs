using HiHi.Serialization;
using System.Collections.Concurrent;
using HiHi.Common;

namespace HiHi {
    public class PeerMessage {
        // Message header is 4 bits (1 bytes / 8 bits).
        public const int PEER_MESSAGE_TYPE_BITS = 4;
        public const int PEER_MESSAGE_HEADER_BITS = PEER_MESSAGE_TYPE_BITS;

        public const int DEFAULT_INITIAL_AVAILABLE_MESSAGES = 64;

        public static ConcurrentBag<PeerMessage> AvailableMessages = new ConcurrentBag<PeerMessage>();

        public PeerMessageType Type { get; private set; }
        public ushort SenderPeerID { get; private set; }
        public ushort DestinationPeerID => destinationPeerID ?? 0;
        public bool DestinationAll => destinationPeerID == null;
        public BitBuffer Buffer { get; private set; }

        protected ushort? destinationPeerID;

        static PeerMessage() {
            for(int m = 0; m < DEFAULT_INITIAL_AVAILABLE_MESSAGES; m++) {
                AvailableMessages.Add(new PeerMessage());
            }
        }

        protected PeerMessage() {
            Buffer = new BitBuffer();

            Clear();
        }

        public static PeerMessage Borrow(PeerMessageType type, ushort senderPeerID, ushort? destinationPeerID = null) {
            PeerMessage message = Borrow();

            message.Type = type;
            message.SenderPeerID = senderPeerID;
            message.destinationPeerID = destinationPeerID;

            message.Buffer.Add(PEER_MESSAGE_TYPE_BITS, (uint)type);

            return message;
        }

        public static PeerMessage Borrow(string endPoint, byte[] data, int length) {
            PeerMessage message = Borrow();

            message.Buffer.FromArray(data, length);

            message.Type = (PeerMessageType)message.Buffer.Read(PEER_MESSAGE_TYPE_BITS);
            if (Peer.Network.TryGetIDFromEndpoint(endPoint, out ushort peerID)) {
                message.SenderPeerID = peerID;
            }

            return message;
        }

        protected static PeerMessage Borrow() {
            PeerMessage message;
            
            if (!AvailableMessages.TryTake(out message)) {
                message = new PeerMessage();
            }

            return message;
        }


        public static bool ContainsValidHeader(byte[] dgram) {
            return dgram != null
                && dgram.Length * 8 >= PEER_MESSAGE_HEADER_BITS;
        }

        public void Return() {
            Clear();

            AvailableMessages.Add(this);
        }

        protected void Clear() {
            Type = PeerMessageType.Unknown;
            SenderPeerID = default;
            destinationPeerID = default;
            Buffer.Clear();
        }
    }
}