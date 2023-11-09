using HiHi.Serialization;
using System;
using System.Collections.Concurrent;
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
