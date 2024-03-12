using HiHi.Commands;
using HiHi.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

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
                connectionKey = $"HiHiV{HiHiConfiguration.Version} {value}";

                if (Info != null) {
                    Info.ConnectionKey = connectionKey;
                }
            }
        }

        public static Action<ushort, PeerConnectReason> OnConnect;
        public static Action<ushort, PeerDisconnectReason> OnDisconnect;
        public static Action<PeerMessageType, ushort> OnMessageProcessed;
        public static Action<ushort, string> OnLog;

        public static bool Initialized { get; private set; }
        public static bool Connected => PeerNetwork.Connected;
        public static bool AcceptingConnections { get; set; } = true;
        public static bool AcceptingUnverifiedInfo { get; set; } = true;

        public static PeerInfo Info { get; private set; }
        public static PeerTransport Transport { get; private set; }
        public static IHelper Helper { get; private set; }

        private static string connectionKey = $"HiHiV{HiHiConfiguration.Version} {DEFAULT_CONNECTION_KEY}";

        public static void Initialize(PeerTransport transport, IHelper helper) {
            if (Initialized) { return; }

            HiHiTime.Reset();
            CommandUtility.Initialize();

            Transport = transport;
            Helper = helper;
            Transport.Start();

            Info = PeerInfo.CreateLocal();

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

            while (Transport.IncomingMessagesAvailable) {
                PeerMessage message = Transport.Receive();
                ProcessPeerMessage(message);
                message.Return();
            }

            NetworkObject.UpdateInstances();

            IEnumerable<PeerInfo> pingablePeers = PeerNetwork.RemotePeerIDs
                .Select(p => PeerNetwork.GetPeerInfo(p))
                .Where(p => p.ShouldRequestPing)
                .Take(HiHiConfiguration.PING_BUDGET_PER_TICK);
            foreach (PeerInfo peerInfo in pingablePeers) {
                PeerMessage pingMessage = NewMessage(PeerMessageType.PingRequest);
                pingMessage.Buffer.AddUShort(HalfPrecision.Quantize(HiHiTime.RealTime));
                Transport.Send(pingMessage);

                peerInfo.RegisterPingRequest();
            }

            foreach (ushort peerID in PeerNetwork.RemotePeerIDs) {
                if (PeerNetwork.GetPeerInfo(peerID).HeartbeatTimedOut) {
                    Disconnect(peerID, PeerDisconnectReason.TimedOut);
                }
            }
        }

        public static PeerMessage NewMessage(PeerMessageType messageType, string destinationEndPoint) => PeerMessage.BorrowOutgoing(messageType, 
            Info.UniqueID, 
            destinationEndPoint);
        public static PeerMessage NewMessage(PeerMessageType messageType, ushort? destinationPeerID = null) => PeerMessage.BorrowOutgoing(messageType, 
            Info.UniqueID, 
            destinationPeerID == null ? string.Empty : PeerNetwork.GetPeerInfo(destinationPeerID ?? default).RemoteEndPoint,
            destinationPeerID == null ? string.Empty : PeerNetwork.GetPeerInfo(destinationPeerID ?? default).LocalEndPoint);

        #region Outgoing

        public static bool TryConnectViaCode(string connectionCode) {
            if (!Transport.TryConnectionCodeToEndPoint(connectionCode, out string endPoint)) { return false; } 
            
            Connect(endPoint);
            return true;
        }
        
        public static void Connect(string destinationEndPoint) {
            if (!Initialized) { return; }
            if (!AcceptingConnections) { return; }

            PeerMessage connectMessage = NewMessage(PeerMessageType.Connect, destinationEndPoint);
            Info.Serialize(connectMessage.Buffer);
            Transport.Send(connectMessage);
        }

        public static void Connect(PeerInfo destinationInfo, PeerConnectReason reason = PeerConnectReason.Unknown) {
            if (!Initialized) { return; }
            if (!AcceptingConnections) { return; }
            if (!AcceptingUnverifiedInfo && !destinationInfo.Verified) { return; }
            if (destinationInfo.ConnectionKey != ConnectionKey) { return; }

            if (!PeerNetwork.TryAddConnection(destinationInfo)) { return; }

            // Connect message
            PeerMessage connectMessage = NewMessage(PeerMessageType.Connect, destinationInfo.UniqueID);
            Info.Serialize(connectMessage.Buffer);
            Transport.Send(connectMessage);

            // PeerNetwork message
            PeerMessage peernetworkMessage = NewMessage(PeerMessageType.PeerNetwork, destinationInfo.UniqueID);
            PeerNetwork.SerializeConnections(peernetworkMessage.Buffer);
            Transport.Send(peernetworkMessage);

            // Time message
            PeerMessage timeMessage = NewMessage(PeerMessageType.Time);
            HiHiTime.Serialize(timeMessage.Buffer);
            Transport.Send(timeMessage);

            // Spawn messages
            foreach (ushort id in NetworkObject.IDs) {
                NetworkObject networkObject = NetworkObject.GetByID(id);

                if (networkObject.OriginSpawnData == null) { continue; }

                NetworkObject.SendSpawn(networkObject.OriginSpawnData, networkObject.UniqueID, networkObject.Owned ? networkObject.OwnerID : null);
            }

            OnConnect?.Invoke(destinationInfo.UniqueID, reason);
        }

        public static void Disconnect(ushort destinationPeerID, PeerDisconnectReason reason = PeerDisconnectReason.UnknownReason) {
            if (!Initialized) { return; }
            if (!PeerNetwork.Contains(destinationPeerID)) { return; }

            PeerMessage message = NewMessage(PeerMessageType.Disconnect, destinationPeerID);
            Transport.Send(message);

            if (!PeerNetwork.TryRemoveConnection(destinationPeerID)) { return; }

            OnDisconnect?.Invoke(destinationPeerID, reason);

            // Change ownership of objects owned by the disconnected peer
            foreach (ushort id in NetworkObject.IDs) {
                NetworkObject networkObject = NetworkObject.GetByID(id);

                if(!networkObject.Owned || networkObject.OwnerID != destinationPeerID) { continue; }

                networkObject.Abandon(true);
            }
        }

        public static void DisconnectAll() {
            if (!Initialized) { return; }

            foreach (ushort connection in PeerNetwork.RemotePeerIDs) {
                Disconnect(connection, PeerDisconnectReason.LocalPeerDisconnected);
            }
        }

        public static void SendMessage(PeerMessage message) {
            if (!Initialized) { return; }

            Transport.Send(message);
        }

        public static void SendLog(string message) {
            if (!Initialized) { return; }

            if (CommandUtility.TryInvokeCommand(message, out string result)) {
                Log(Info.UniqueID, result);
                return;
            }

            PeerMessage outgoingMessage = NewMessage(PeerMessageType.Log);
            outgoingMessage.Buffer.AddString(message);
            Transport.Send(outgoingMessage);

            Log(Info.UniqueID, message);
        }

        #endregion

        #region Incoming

        private static void ProcessPeerMessage(PeerMessage message) {
            if(message.SenderPeerID == Info.UniqueID) { return; }

            switch (message.Type) {
                case PeerMessageType.VerifiedPeerInfo:
                    Info.Deserialize(message.Buffer);
                    break;

                case PeerMessageType.RemotePeerInfo:
                    PeerInfo remotePeerInfo = new PeerInfo();
                    remotePeerInfo.Deserialize(message.Buffer);

                    Connect(remotePeerInfo, PeerConnectReason.ExternalReferrer);
                    break;

                case PeerMessageType.Connect:
                    PeerInfo incomingPeerInfo = new PeerInfo();
                    incomingPeerInfo.Deserialize(message.Buffer);
                    incomingPeerInfo.RemoteEndPoint = message.SenderEndPoint;

                    Connect(incomingPeerInfo, PeerConnectReason.ExternalReferrer);
                    break;

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
                    NetworkObject.ReceiveSyncObjectData(message);
                    break;

                case PeerMessageType.ObjectSpawn:
                    NetworkObject.ReceiveSpawn(message);
                    break;

                case PeerMessageType.ObjectDestroy:
                    NetworkObject.ReceiveDestroy(message);
                    break;

                case PeerMessageType.ObjectOwnershipChange:
                    NetworkObject.ReceiveOwnershipChange(message);
                    break;

                case PeerMessageType.ObjectAbandoned:
                    NetworkObject.ReceiveAbandonment(message);
                    break;

                case PeerMessageType.ObjectAbandonmentPolicyChange:
                    NetworkObject.ReceiveAbandonmentPolicyChange(message);
                    break;

                case PeerMessageType.Unknown:
                default:
                    // OOPS BROKEN MESSAGE :(
                    // TODO LOG WARNING HERE
                    break;
            }

            if (PeerNetwork.Contains(message.SenderPeerID)) {
                PeerNetwork.GetPeerInfo(message.SenderPeerID).RegisterHeartbeat();
            }

            OnMessageProcessed?.Invoke(message.Type, message.SenderPeerID);
        }

        private static void HandlePeerNetworkMessage(PeerMessage message) {
            PeerInfo[] connections = PeerNetwork.DeserializeConnections(message.Buffer);

            foreach(PeerInfo info in connections) {
                Connect(info, PeerConnectReason.PeerNetwork);
            }
        }

        private static void ProcessPingRequest(PeerMessage message) {
            if (!PeerNetwork.Contains(message.SenderPeerID)) { return; }

            float sentPing = HiHiTime.RealTime - HalfPrecision.Dequantize(message.Buffer.ReadUShort());

            PeerMessage pingMessage = NewMessage(PeerMessageType.PingResponse);
            pingMessage.Buffer.AddUShort(HalfPrecision.Quantize(sentPing));
            Transport.Send(pingMessage);
        }

        private static void ProcessPingResponse(PeerMessage message) {
            if (!PeerNetwork.Contains(message.SenderPeerID)) { return; }

            float receivedPing = HalfPrecision.Dequantize(message.Buffer.ReadUShort());
            PeerNetwork.GetPeerInfo(message.SenderPeerID).SetPing(receivedPing);
        }

        #endregion

        #region Misc

        private static void Log(ushort peerID, string message) => OnLog?.Invoke(peerID, message);

        public static string GetConnectionCode(this PeerInfo peerInfo) {
            Transport.TryEndPointToConnectionCode(peerInfo.RemoteEndPoint, out string connectionCode);

            return connectionCode;
        }

        #endregion
    }
}