using HiHi.Common;
using HiHi.Serialization;
using System;

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
    public static class Peer {
        private static readonly string DEFAULT_CONNECTION_KEY = $"{nameof(HiHi)} connection key is not set. This is the key contained in {nameof(Peer)}.{nameof(DEFAULT_CONNECTION_KEY)}.";

        public static string ConnectionKey {
            get => connectionKey;
            set {
                Info.ConnectionKey = connectionKey = value;
            }
        }

        public static Action<ushort, PeerConnectReason> OnConnect;
        public static Action<ushort, PeerDisconnectReason> OnDisconnect;
        public static Action<PeerMessageType, ushort> OnMessageProcessed;
        public static Action<ushort, string> OnLog;

        public static bool Initialized { get; private set; }
        public static bool Connected => Network.Connected;
        public static bool AcceptingConnections { get; set; } = true;

        public static PeerInfo Info { get; private set; }
        public static PeerTransport Transport { get; private set; }
        public static IHelper Helper { get; private set; }
        public static PeerNetwork Network => ISingleton<PeerNetwork>.Instance;

        private static string connectionKey = DEFAULT_CONNECTION_KEY;

        static Peer() {
            Info = new PeerInfo();
        }

        public static void Initialize(PeerTransport transport, IHelper helper) {
            if (Initialized) { return; }

            HiHiTime.Reset();

            Transport = transport;
            Helper = helper;
            Transport.Start();

            Initialized = true;
        }

        public static void UnInitialize() {
            if (!Initialized) { return; }

            if (Connected) {
                DisconnectAll();
            }

            Transport.Stop();

            Initialized = false;
        }

        public static void Update(float deltaTime) {
            if (!Initialized) { return; }

            HiHiTime.AdvanceTick(deltaTime);

            if (Info.ExpectingHeartbeat) {
                Transport.Send(NewMessage(PeerMessageType.HeartBeat));
                Info.RegisterHeartbeat();
            }

            for(int m = 0; m < Transport.IncomingMessages.Count; m++) {
                PeerMessage message = Transport.Receive();
                ProcessPeerMessage(message);
                message.Return();
            }

            INetworkObject.UpdateInstances();

            foreach (ushort peerID in Network.PeerIDs) {
                if (Network[peerID].ShouldRequestPing) {
                    PeerMessage pingMessage = NewMessage(PeerMessageType.PingRequest);
                    pingMessage.Buffer.AddUShort(HalfPrecision.Quantize(HiHiTime.RealTime));
                    Transport.Send(pingMessage);

                    Network[peerID].RegisterPingRequest();
                }

                if (Network[peerID].HeartbeatTimedOut) {
                    Disconnect(peerID, PeerDisconnectReason.TimedOut);
                }
            }
        }

        public static PeerMessage NewMessage(PeerMessageType messageType, ushort? destinationPeer = null) => PeerMessage.Borrow(messageType, Info.UniqueID, destinationPeer);

        #region Outgoing

        public static void Connect(PeerInfo destinationInfo, PeerConnectReason reason = PeerConnectReason.Unknown) {
            if (!Initialized) { return; }
            if(!AcceptingConnections) { return; }
            if(destinationInfo.ConnectionKey != ConnectionKey) { return; }

            if (!Network.TryAddConnection(destinationInfo)) { return; }

            // Connect message
            PeerMessage connectMessage = NewMessage(PeerMessageType.Connect, destinationInfo.UniqueID);
            Info.Serialize(connectMessage.Buffer);
            Transport.Send(connectMessage);

            // PeerNetwork message
            PeerMessage peernetworkMessage = NewMessage(PeerMessageType.PeerNetwork, destinationInfo.UniqueID);
            Network.SerializeConnections(peernetworkMessage.Buffer);
            Transport.Send(peernetworkMessage);

            // Time message
            PeerMessage timeMessage = NewMessage(PeerMessageType.Time);
            HiHiTime.Serialize(timeMessage.Buffer);
            Transport.Send(timeMessage);

            // Spawn messages
            foreach (ushort id in INetworkObject.Instances.Keys) {
                INetworkObject networkObject = INetworkObject.Instances[id];

                if (networkObject.OriginSpawnData == null) { continue; }

                INetworkObject.SendSpawn(networkObject.OriginSpawnData, networkObject.UniqueID, networkObject.Owned ? networkObject.OwnerID : null);
            }

            OnConnect?.Invoke(destinationInfo.UniqueID, reason);
        }

        public static void Disconnect(ushort destinationPeerID, PeerDisconnectReason reason = PeerDisconnectReason.UnknownReason) {
            if (!Initialized) { return; }
            if (!Network.Contains(destinationPeerID)) { return; }

            PeerMessage message = NewMessage(PeerMessageType.Disconnect, destinationPeerID);
            Transport.Send(message);

            if (!Network.TryRemoveConnection(destinationPeerID)) { return; }

            OnDisconnect?.Invoke(destinationPeerID, reason);

            // Change ownership of objects owned by the disconnected peer
            foreach (ushort id in INetworkObject.Instances.Keys) {
                INetworkObject networkObject = INetworkObject.Instances[id];

                if(!networkObject.Owned || networkObject.OwnerID != destinationPeerID) { continue; }

                networkObject.Abandon(true);
            }
        }

        public static void DisconnectAll() {
            if (!Initialized) { return; }

            foreach (ushort connection in Network.PeerIDs) {
                Disconnect(connection, PeerDisconnectReason.LocalPeerDisconnected);
            }
        }

        public static void SendLog(string message) {
            if (!Initialized) { return; }

            PeerMessage outgoingMessage = NewMessage(PeerMessageType.Log);
            outgoingMessage.Buffer.AddString(message);
            Transport.Send(outgoingMessage);

            Log(Info.UniqueID, message);
        }

        public static void SendMessage(PeerMessage message) {
            if (!Initialized) { return; }

            if (!message.DestinationAll && !Network.Contains(message.DestinationPeerID)) {
                throw new HiHiException($"Network doesn't contain peer with ID {message.DestinationPeerID}.");
            }

            Transport.Send(message);
        }

        #endregion

        #region Incoming

        private static void ProcessPeerMessage(PeerMessage message) {
            if(message.SenderPeerID == Info.UniqueID) { return; }

            switch (message.Type) {
                case PeerMessageType.Connect:
                    PeerInfo incomingPeerInfo = new PeerInfo();
                    incomingPeerInfo.Deserialize(message.Buffer);

                    Connect(incomingPeerInfo, PeerConnectReason.ExternalReferrer);
                    break;

                case PeerMessageType.Unknown:
                default:
                    // OOPS BROKEN MESSAGE :(
                    // TODO LOG WARNING HERE
                    break;
            }

            if (!Network.Contains(message.SenderPeerID)) { return; }

            switch (message.Type) {
                case PeerMessageType.Disconnect:
                    Disconnect(message.SenderPeerID, PeerDisconnectReason.RemotePeerDisconnected);
                    break;

                case PeerMessageType.HeartBeat:
                    break;

                case PeerMessageType.PeerNetwork:
                    HandlePeerNetworkMessage(message);
                    break;

                case PeerMessageType.Time:
                    HiHiTime.Deserialize(message.Buffer);
                    break;

                case PeerMessageType.PingRequest:
                    ProcessPingRequest(message);
                    break;

                case PeerMessageType.PingResponse:
                    ProcessPingResponse(message);
                    break;

                case PeerMessageType.Log:
                    string log = message.Buffer.ReadString();
                    Log(message.SenderPeerID, log);
                    break;

                case PeerMessageType.SOData:
                    INetworkObject.ReceiveSyncObjectData(message);
                    break;

                case PeerMessageType.ObjectSpawn:
                    INetworkObject.ReceiveSpawn(message);
                    break;

                case PeerMessageType.ObjectDestroy:
                    INetworkObject.ReceiveDestroy(message);
                    break;

                case PeerMessageType.ObjectOwnershipChange:
                    INetworkObject.ReceiveOwnershipChange(message);
                    break;

                case PeerMessageType.ObjectAbandoned:
                    INetworkObject.ReceiveAbandonment(message);
                    break;

                case PeerMessageType.ObjectAbandonmentPolicyChange:
                    INetworkObject.ReceiveAbandonmentPolicyChange(message);
                    break;
            }

            if (Network.Contains(message.SenderPeerID)) {
                Network[message.SenderPeerID].RegisterHeartbeat();
            }

            OnMessageProcessed?.Invoke(message.Type, message.SenderPeerID);
        }

        private static void HandlePeerNetworkMessage(PeerMessage message) {
            PeerInfo[] connections = Network.DeserializeConnections(message.Buffer);

            foreach(PeerInfo info in connections) {
                Connect(info, PeerConnectReason.PeerNetwork);
            }
        }

        private static void ProcessPingRequest(PeerMessage message) {
            float sentPing = HiHiTime.RealTime - HalfPrecision.Dequantize(message.Buffer.ReadUShort());

            PeerMessage pingMessage = NewMessage(PeerMessageType.PingResponse);
            pingMessage.Buffer.AddUShort(HalfPrecision.Quantize(sentPing));
            Transport.Send(pingMessage);
        }

        private static void ProcessPingResponse(PeerMessage message) {
            float receivedPing = HalfPrecision.Dequantize(message.Buffer.ReadUShort());
            Network[message.SenderPeerID].SetPing(receivedPing);
        }

        #endregion

        private static void Log(ushort peerID, string message) => OnLog?.Invoke(peerID, message);
    }
}