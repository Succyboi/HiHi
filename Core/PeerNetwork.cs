using HiHi.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HiHi {
    public class PeerNetwork {
        public ICollection<ushort> PeerIDs => connections.Keys;
        public int Connections => connections.Count;
        public bool Connected => connections.Count > 0; 

        private ConcurrentDictionary<ushort, PeerInfo> connections { get; set; }
        private Random syncedRandom;

        public PeerNetwork() {
            connections = new ConcurrentDictionary<ushort, PeerInfo>();

            ResetSyncedRandom();
        }

        public PeerInfo this[ushort ID] => ID == Peer.Info.UniqueID 
            ? Peer.Info 
            : connections[ID];

        public bool TryAddConnection(PeerInfo info) {
            if (info.UniqueID == Peer.Info.UniqueID) { return false; }
            if (connections.ContainsKey(info.UniqueID)) { return false; }

            connections.AddOrUpdate(info.UniqueID, info, (id, p) => info);
            ResetSyncedRandom();
            return true;
        }

        public bool TryRemoveConnection(ushort peerID) {
            if (!connections.ContainsKey(peerID)) { return false; }

            connections.Remove(peerID, out _);
            ResetSyncedRandom();
            return true;
        }

        public bool Contains(ushort ID) {
            return ID == Peer.Info.UniqueID 
                ? true
                : connections.ContainsKey(ID);
        }

        public bool TryGetIDFromEndpoint(string endpoint, out ushort id) {
            foreach(KeyValuePair<ushort, PeerInfo> connection in connections) {
                if (string.Equals(connection.Value.EndPoint, endpoint, StringComparison.OrdinalIgnoreCase)) {
                    id = connection.Key;
                    return true;
                }
            }

            id = default;
            return false;
        }

        public ushort GetSyncedRandomUShort() {
            return (ushort)syncedRandom.Next(ushort.MaxValue);
        }

        public ushort GetRandomPeerID() {
            IEnumerable<ushort> peers = Peer.Network.PeerIDs.Append(Peer.Info.UniqueID);
            return peers.Skip(Peer.Network.GetSyncedRandomUShort() % peers.Count()).First();
        }

        private void ResetSyncedRandom() {
            syncedRandom = new Random(connections.Sum(c => c.Key));
        }

        #region Serialization

        public void SerializeConnections(BitBuffer buffer) {
            buffer.AddUShort((ushort)PeerIDs.Count);

            foreach(ushort peerID in PeerIDs) {
                connections[peerID].Serialize(buffer);
            }
        }

        public PeerInfo[] DeserializeConnections(BitBuffer buffer) {
            ushort connectionCount = buffer.ReadUShort();
            PeerInfo[] connections = new PeerInfo[connectionCount];

            for(int c = 0; c < connectionCount; c++) {
                connections[c] = new PeerInfo();
                connections[c].Deserialize(buffer);
            }

            return connections;
        }

        #endregion
    }
}
