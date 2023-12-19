using HiHi.Serialization;
using System;
using System.Collections.Generic;

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
namespace HiHi.Signaling {
    public class LiteNetSignaler : Signaler {
        public event Action<SignalerConnectionInfo> OnPeerConnected;
        public event Action<SignalerConnectionInfo> OnPeerLobbied;
        public event Action<SignalerConnectionInfo> OnPeerDisconnected;

        public string LocalEndPoint => transport.LocalEndPoint;
        public string LocalAddress => transport.LocalAddress;
        public int Port => transport.LocalPort;
        public List<SignalerLobby<SignalerConnectionInfo>> Lobbies = new List<SignalerLobby<SignalerConnectionInfo>>();
        public Dictionary<string, SignalerConnectionInfo> Connections = new Dictionary<string, SignalerConnectionInfo>();

        private LiteNetTransport transport;

        public LiteNetSignaler(int? port = null) : base() {
            this.transport = new LiteNetTransport(port ?? HiHiConfiguration.SIGNALER_DEFAULT_PORT);
        }

        public override void Start() {
            transport.Start();

            base.Start();
        }

        public override void Stop() {
            transport.Stop();

            base.Stop();
        }

        public override void Signal() {
            ReceiveMessages();
            ProcessLobbies();
        }

        private void ReceiveMessages() {
            while (transport.IncomingMessagesAvailable) {
                PeerMessage message = transport.Receive();
                ProcessMessage(message);
                message.Return();
            }
        }

        private void ProcessMessage(PeerMessage message) {
            if (!Connections.ContainsKey(message.SenderEndPoint)) {
                SignalerConnectionInfo info = new SignalerConnectionInfo() { RemoteEndPoint = message.SenderEndPoint };
                Connections.Add(message.SenderEndPoint, info);
                OnPeerConnected?.Invoke(info);
            }

            SignalerConnectionInfo connection = Connections[message.SenderEndPoint];

            switch (message.Type) {
                case PeerMessageType.VerifiedPeerInfoRequest:
                    connection.Deserialize(message.Buffer);
                    connection.DesiredLobbySize = message.Buffer.ReadInt();
                    connection.RemoteEndPoint = message.SenderEndPoint;
                    connection.RegisterHeartbeat();
                    
                    LobbyConnection(connection);
                    break;

                case PeerMessageType.RemotePeerInfoRequest:
                    connection.Deserialize(message.Buffer);
                    connection.DesiredLobbySize = message.Buffer.ReadInt();
                    connection.RemoteEndPoint = message.SenderEndPoint;
                    connection.RegisterHeartbeat();

                    if (connection.Lobby == null) {
                        LobbyConnection(connection);
                    }

                    SendRemotePeerInfo(connection.Lobby);
                    break;

                case PeerMessageType.Disconnect:
                    Disconnect(connection);
                    break;
            }

            connection.RegisterHeartbeat();
        }

        private void Disconnect(SignalerConnectionInfo info) {
            info.Lobby?.Remove(info);
            Connections.Remove(info.RemoteEndPoint);

            OnPeerDisconnected?.Invoke(info);
        }

        private void ProcessLobbies() {
            int l = 0;
            while (l < Lobbies.Count) {
                SignalerLobby<SignalerConnectionInfo> lobby = Lobbies[l];

                int p = 0;
                while (p < lobby.Count) {
                    SignalerConnectionInfo info = lobby.Connections[p];

                    if (Environment.TickCount - info.HeartbeatTick > HiHiConfiguration.HEARTBEAT_TIMEOUT_INTERVAL_MS) {
                        Disconnect(info);
                        p--;
                    }

                    p++;
                }

                if (lobby.Empty) {
                    Lobbies.Remove(lobby);
                    l--;
                }

                l++;
            }
        }

        private void LobbyConnection(SignalerConnectionInfo info) {
            SignalerLobby<SignalerConnectionInfo> lobby = null;

            foreach (SignalerLobby<SignalerConnectionInfo> existingLobby in Lobbies) {
                if (existingLobby.TryAdd(info)) {
                    lobby = existingLobby;
                    break;
                }
            }

            if (lobby == null) {
                SignalerLobby<SignalerConnectionInfo> newLobby = new SignalerLobby<SignalerConnectionInfo>(info.DesiredLobbySize, info.ConnectionKey);
                newLobby.TryAdd(info);
                Lobbies.Add(newLobby);
                lobby = newLobby;
            }

            SendVerifiedPeerInfo(info);

            info.Lobby = lobby;
            OnPeerLobbied?.Invoke(info);
        }

        private void SendVerifiedPeerInfo(SignalerConnectionInfo destinationInfo) {
            PeerMessage message = PeerMessage.Borrow(PeerMessageType.VerifiedPeerInfo, default, destinationInfo.RemoteEndPoint);
            destinationInfo.Verified = true;
            destinationInfo.Serialize(message.Buffer);
            transport.Send(message);
        }

        private void SendRemotePeerInfo(SignalerLobby<SignalerConnectionInfo> lobby) {
            if(lobby == null) { return; }

            for(int c0 = 0; c0 < lobby.Count; c0++) {
                SignalerConnectionInfo peer0 = lobby.Connections[c0];

                for (int c1 = 0; c1 < lobby.Count; c1++) {
                    if(c0 == c1) { continue; }

                    SignalerConnectionInfo peer1 = lobby.Connections[c1];

                    transport.SendNATIntroduction(peer0.LocalEndPoint, peer0.RemoteEndPoint, peer1.LocalEndPoint, peer1.RemoteEndPoint);
                }
            }
        }
    }
}