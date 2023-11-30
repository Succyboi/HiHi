using HiHi.Common;
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
    public interface INetworkObject {
        public static Dictionary<ushort, INetworkObject> Instances = new Dictionary<ushort, INetworkObject>();
        
        protected static Queue<ushort> availableIDs = new Queue<ushort>();
        protected static IHelper helper => Peer.Helper;

        public Action OnOwnershipChanged { get; set; }
        public Action OnAbandonmentPolicyChanged { get; set; }

        public ISpawnData OriginSpawnData { get; protected set; }
        public bool Registered { get; protected set; }
        public ushort UniqueID { get; protected set; }
        public ushort? OwnerID {
            get {
                return ownerID;
            }
            protected set {
                if (value == ownerID) { return; }

                ownerID = value;
                OnOwnershipChanged?.Invoke();
            }
        }
        public bool Authorized => OwnerID == null || OwnedLocally;
        public bool OwnedLocally => OwnerID == Peer.Info.UniqueID;
        public bool Owned => OwnerID != null;
        public NetworkObjectAbandonmentPolicy AbandonmentPolicy {
            get {
                return abandonmentPolicy;
            }
            set {
                AuthorizationCheck();

                if (value == abandonmentPolicy) { return; }
                
                abandonmentPolicy = value;
                OnAbandonmentPolicyChanged?.Invoke();

                SendAbandonmentPolicyChange(UniqueID, AbandonmentPolicy);
            }
        }
        public SyncObject[] SyncObjects {
            get {
                syncObjects ??= new SyncObject[byte.MaxValue + 1];
                return syncObjects;
            }
        }

        protected ushort? ownerID { get; set; }
        protected NetworkObjectAbandonmentPolicy abandonmentPolicy { get; set; }
        protected SyncObject[] syncObjects { get; set; }
        protected byte syncObjectCount { get; set; }

        static INetworkObject() {
            Random random = new Random();

            for(ushort i = 0; i < ushort.MaxValue; i++) {
                availableIDs.Enqueue(i);
            }

            availableIDs = new Queue<ushort>(availableIDs.OrderBy(i => random.Next()));
        }

        public static void UpdateInstances() {
            foreach (ushort uniqueID in Instances.Keys) {
                Instances[uniqueID].Update();
                Instances[uniqueID].UpdateSyncObjects();
            }
        }

        public ushort Register(ushort? proposedUniqueID = null, ushort? ownerID = null, ISpawnData originSpawnData = null) {
            if (Registered) { return UniqueID; }

            UniqueID = proposedUniqueID ?? UniqueID;
            OwnerID = ownerID;
            OriginSpawnData = originSpawnData;
            if (Instances.ContainsKey(UniqueID)) {
                UniqueID = availableIDs.Dequeue();
            }
            
            Instances.Add(UniqueID, this);
            Registered = true;

            OnRegister();

            return UniqueID;
        }

        public void UnRegister() {
            if (!Registered) { return; }

            availableIDs.Enqueue(UniqueID);

            Instances.Remove(UniqueID);
            Registered = false;

            OnUnregister();
        }

        #region Spawning

        public static INetworkObject SyncSpawn(ISpawnData spawnData, ushort? ownerID = null) {
            INetworkObject spawnedObject = spawnData.Spawn();
            spawnedObject.Register(null, ownerID, spawnData);

            SendSpawn(spawnData, spawnedObject.UniqueID, ownerID);

            return spawnedObject;
        }

        public static void SendSpawn(ISpawnData spawnData, ushort uniqueID, ushort? ownerID) {
            PeerMessage message = Peer.NewMessage(PeerMessageType.ObjectSpawn);
            helper.SerializeSpawnData(spawnData, message.Buffer);
            message.Buffer.AddUShort(uniqueID);
            message.Buffer.AddBool(ownerID == null);
            message.Buffer.AddUShort(ownerID ?? 0);

            Peer.SendMessage(message);
        }

        public static void ReceiveSpawn(PeerMessage message) {
            ISpawnData spawnData = helper.DeserializeSpawnData(message.Buffer);
            ushort uniqueID = message.Buffer.ReadUShort();
            bool shared = message.Buffer.ReadBool();
            ushort? ownerID = message.Buffer.ReadUShort();
            ownerID = shared ? null : ownerID;

            if (Instances.ContainsKey(uniqueID)) { return; }

            INetworkObject spawnedObject = spawnData.Spawn();
            spawnedObject.Register(uniqueID, ownerID, spawnData);
        }

        #endregion

        #region Destroying

        public static void SyncDestroy(INetworkObject target) {
            target.RegistrationCheck();
            target.AuthorizationCheck();

            SendDestroy(target.UniqueID);
            target.UnRegister();
            target.DestroyLocally();
        }

        public static void SendDestroy(ushort uniqueID) {
            PeerMessage message = Peer.NewMessage(PeerMessageType.ObjectDestroy);
            message.Buffer.AddUShort(uniqueID);

            Peer.SendMessage(message);
        }

        public static void ReceiveDestroy(PeerMessage message) {
            ushort uniqueID = message.Buffer.ReadUShort();

            ExistenceCheck(uniqueID);

            INetworkObject targetObject = Instances[uniqueID];

            if (targetObject.Owned && targetObject.OwnerID != message.SenderPeerID) { return; }

            targetObject.UnRegister();
            targetObject.DestroyLocally();
        }

        public void SyncDestroy() => SyncDestroy(this);

        #endregion

        #region Ownership

        public static void SendOwnershipChange(ushort uniqueID, ushort? newPeerID) {
            PeerMessage message = Peer.NewMessage(PeerMessageType.ObjectOwnershipChange);
            message.Buffer.AddUShort(uniqueID);
            message.Buffer.AddBool(newPeerID != null);
            message.Buffer.AddUShort(newPeerID ?? 0);

            Peer.SendMessage(message);
        }

        public static void ReceiveOwnershipChange(PeerMessage message) {
            ushort uniqueID = message.Buffer.ReadUShort();
            bool isOwned = message.Buffer.ReadBool();
            ushort newPeerID = message.Buffer.ReadUShort();

            ExistenceCheck(uniqueID);

            INetworkObject targetObject = Instances[uniqueID];

            if (targetObject.Owned && targetObject.OwnerID != message.SenderPeerID) { return; }

            targetObject.OwnerID = isOwned 
                ? newPeerID
                : null;
        }

        public void Claim() => GiveToPeer(Peer.Info.UniqueID);
        public void Forfeit() => GiveToPeer(null);
        public void GiveToRandomPeer() => GiveToPeer(Peer.Network.GetRandomPeerID());
        public void GiveToPeer(ushort? peerID) => GiveToPeer(peerID, false);

        protected void GiveToPeer(ushort? peerID, bool forceLocally = false) {
            RegistrationCheck();

            if (forceLocally) {
                OwnerID = peerID;
                return; 
            }

            AuthorizationCheck();

            OwnerID = peerID;
            SendOwnershipChange(UniqueID, peerID);
        }

        #endregion

        #region Abandonment

        public static void SendAbandonment(ushort uniqueID) {
            PeerMessage message = Peer.NewMessage(PeerMessageType.ObjectAbandoned);
            message.Buffer.AddUShort(uniqueID);

            Peer.SendMessage(message);
        }

        public static void ReceiveAbandonment(PeerMessage message) {
            ushort uniqueID = message.Buffer.ReadUShort();

            ExistenceCheck(uniqueID);

            INetworkObject targetObject = Instances[uniqueID];

            if (targetObject.Owned && targetObject.OwnerID != message.SenderPeerID) { return; }

            targetObject.HandleAbandonment();
        }

        public static void SendAbandonmentPolicyChange(ushort uniqueID, NetworkObjectAbandonmentPolicy abandonmentPolicy) {
            PeerMessage message = Peer.NewMessage(PeerMessageType.ObjectAbandonmentPolicyChange);
            message.Buffer.AddUShort(uniqueID);
            message.Buffer.AddByte((byte)abandonmentPolicy);

            Peer.SendMessage(message);
        }

        public static void ReceiveAbandonmentPolicyChange(PeerMessage message) {
            ushort uniqueID = message.Buffer.ReadUShort();
            NetworkObjectAbandonmentPolicy abandonmentPolicy = (NetworkObjectAbandonmentPolicy)message.Buffer.ReadByte();

            ExistenceCheck(uniqueID);

            INetworkObject targetObject = Instances[uniqueID];

            if (targetObject.Owned && targetObject.OwnerID != message.SenderPeerID) { return; }

            targetObject.AbandonmentPolicy = abandonmentPolicy;
            targetObject.OnAbandonmentPolicyChanged?.Invoke();
        }

        public void Abandon(bool forceLocally = false) {
            if (forceLocally) {
                HandleAbandonment();
                return;
            }

            AuthorizationCheck();

            HandleAbandonment();
            SendAbandonment(UniqueID);
        }

        protected void HandleAbandonment() {
            switch (AbandonmentPolicy) {
                case NetworkObjectAbandonmentPolicy.RemainOwnedRandomly:
                    if (!Owned) { break; }

                    IEnumerable<ushort> candidates = Peer.Network.PeerIDs.Concat(new ushort[1] { Peer.Info.UniqueID }).OrderBy(p => p);
                    ushort pickedID = candidates.Skip(UniqueID % candidates.Count()).First();

                    GiveToPeer(pickedID, true);
                    break;

                case NetworkObjectAbandonmentPolicy.BecomeShared:
                    GiveToPeer(null, true);
                    break;

                case NetworkObjectAbandonmentPolicy.Destroy:
                    DestroyLocally();
                    break;
            }
        }

        #endregion


        #region SyncObjects

        public static void ReceiveSyncObjectData(PeerMessage message) {
            ushort parentUniqueID = message.Buffer.ReadUShort();
            byte syncObjectID = message.Buffer.ReadByte();

            ExistenceCheck(parentUniqueID);

            INetworkObject parent = Instances[parentUniqueID];

            if (parent.Owned && parent.OwnerID != message.SenderPeerID) { return; }

            SyncObject syncObject = parent.SyncObjects[syncObjectID];

            syncObject.Deserialize(message.SenderPeerID, message.Buffer);
        }

        public byte RegisterSyncObject(SyncObject syncObject) {
            byte uniqueID = syncObjectCount++;
            SyncObjects[uniqueID] = syncObject;

            return uniqueID;
        }

        public void UpdateSyncObjects() {
            for (int s = 0; s < syncObjectCount; s++) {
                SyncObjects[s]?.Update();
            }
        }

        #endregion

        #region Checks

        protected static void ExistenceCheck(ushort uniqueID) { 
            if (!Instances.ContainsKey(uniqueID)) { throw new HiHiException($"Received a {nameof(PeerMessageType)} targeting a {nameof(INetworkObject)} with ID {uniqueID}, which doesn't exist locally."); } 
        }

        protected void RegistrationCheck() {
            if(!Registered) { throw new HiHiException($"{nameof(INetworkObject)} is not registered."); }
        }

        protected void AuthorizationCheck() {
            if (!Authorized) { throw new HiHiException($"Local {nameof(Peer)} is not authorized."); }
        }

        #endregion

        #region Abstract

        protected abstract void OnRegister();
        protected abstract void OnUnregister();
        protected abstract void Update();
        protected abstract void DestroyLocally();

        #endregion
    }
}
