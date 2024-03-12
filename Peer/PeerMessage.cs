using HiHi.Serialization;
using System;
using System.Collections.Concurrent;

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
    public class PeerMessage {
        // Message header is 5 bits, 32 message types (2^5 = 32 / 1 bytes / 8 bits).
        public const int PEER_MESSAGE_TYPE_BITS = 5;
        public const int PEER_MESSAGE_HEADER_BITS = PEER_MESSAGE_TYPE_BITS;

        public const int DEFAULT_INITIAL_AVAILABLE_MESSAGES = 64;

        public static ConcurrentBag<PeerMessage> AvailableMessages = new ConcurrentBag<PeerMessage>();

        protected static float time => Environment.TickCount / (float)TimeSpan.TicksPerMillisecond;

        public PeerMessageType Type { get; private set; }
        public ushort SenderPeerID { get; private set; }
        public string RemoteDestinationEndPoint {
            get => remoteDestinationEndPoint;
            set => remoteDestinationEndPoint = value;
        }
        public string LocalDestinationEndPoint {
            get => localDestinationEndPoint;
            set => localDestinationEndPoint = value;
        }
        public string SenderEndPoint => senderEndPoint;
        public bool DestinationAll => string.IsNullOrEmpty(remoteDestinationEndPoint)
            && string.IsNullOrEmpty(localDestinationEndPoint);
        public bool Expired => borrowedTime - time > HiHiConfiguration.HEARTBEAT_TIMEOUT_INTERVAL_MS;
        public BitBuffer Buffer { get; private set; }

        protected string remoteDestinationEndPoint;
        protected string localDestinationEndPoint;
        protected string senderEndPoint;
        protected float borrowedTime;

        static PeerMessage() {
            for(int m = 0; m < DEFAULT_INITIAL_AVAILABLE_MESSAGES; m++) {
                AvailableMessages.Add(new PeerMessage());
            }
        }

        protected PeerMessage() {
            Buffer = new BitBuffer();

            Clear();
        }

        public static PeerMessage BorrowOutgoing(PeerMessageType type, ushort senderPeerID, string remoteDestinationEndPoint, string localDesinationEndPoint = null) {
            PeerMessage message = Borrow();

            message.Type = type;
            message.SenderPeerID = senderPeerID;
            message.remoteDestinationEndPoint = remoteDestinationEndPoint;
            message.localDestinationEndPoint = localDesinationEndPoint;

            message.Buffer.Add(PEER_MESSAGE_TYPE_BITS, (uint)type);

            return message;
        }

        public static PeerMessage BorrowIncoming(string endPointString, byte[] data, int length) {
            PeerMessage message = Borrow();

            message.senderEndPoint = endPointString;
            message.Buffer.FromArray(data, length);
            message.Type = (PeerMessageType)message.Buffer.Read(PEER_MESSAGE_TYPE_BITS);

            if (!Peer.Initialized) { return message; }

            if (PeerNetwork.TryGetIDFromEndPointString(message.senderEndPoint, out ushort peerID)) {
                message.SenderPeerID = peerID;
            }

            return message;
        }

        protected static PeerMessage Borrow() {
            PeerMessage message;
            
            if (!AvailableMessages.TryTake(out message)) {
                message = new PeerMessage();
            }

            message.borrowedTime = time;

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
            remoteDestinationEndPoint = string.Empty;
            localDestinationEndPoint = string.Empty;
            senderEndPoint = string.Empty;
            Buffer.Clear();
        }
    }
}