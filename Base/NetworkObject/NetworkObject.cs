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
    public partial class NetworkObject {
        public const ushort NULL_ID = 0;

        #region Static

        public static bool Exists(ushort uniqueID) => instances.ContainsKey(uniqueID);
        public static NetworkObject GetByID(ushort uniqueID) => instances[uniqueID];
        public static bool TryGetByID(ushort uniqueID, out NetworkObject instance) => instances.TryGetValue(uniqueID, out instance);
        public static IEnumerable<ushort> IDs => instances.Keys;

        private static Dictionary<ushort, NetworkObject> instances = new Dictionary<ushort, NetworkObject>();
        private static Queue<ushort> availableIDs = new Queue<ushort>();
        private static IHelper helper => Peer.Helper;

        #endregion

        public Action OnOwnershipChanged { get; set; }
        public Action OnAbandonmentPolicyChanged { get; set; }
        public Action OnRegistered { get; set; }
        public Action OnUnregistered { get; set; }

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

        protected Dictionary<byte, SyncObject> SyncObjects {
            get {
                syncObjects ??= new Dictionary<byte, SyncObject>();
                return syncObjects;
            }
        }
        private ushort? ownerID { get; set; }
        private NetworkObjectAbandonmentPolicy abandonmentPolicy { get; set; }
        private Dictionary<byte, SyncObject> syncObjects { get; set; }
        private static Queue<byte> availableSyncObjectIDs { get; set; }
        private static Random random;

        static NetworkObject() {
            random = new Random();

            for(ushort i = 0; i < ushort.MaxValue; i++) {
                if (i == NULL_ID) { continue; }
                
                availableIDs.Enqueue(i);
            }

            availableIDs = new Queue<ushort>(availableIDs.OrderBy(i => random.Next()));
        }

        public static void UpdateInstances() {
            foreach (ushort uniqueID in instances.Keys) {
                instances[uniqueID].OnUpdate();
                instances[uniqueID].UpdateSyncObjects();
            }
        }

        public static bool IsIDAvailable(ushort ID) {
            return !instances.ContainsKey(ID);
        }

        public ushort Register(ushort? proposedUniqueID = null, ushort? ownerID = null, ISpawnData originSpawnData = null) {
            if (Registered) { return UniqueID; }

            UniqueID = proposedUniqueID ?? UniqueID;
            OwnerID = ownerID;
            OriginSpawnData = originSpawnData;
            if (!IsIDAvailable(UniqueID)) {
                UniqueID = availableIDs.Dequeue();
            }
            
            availableSyncObjectIDs = new Queue<byte>();
            for (byte i = 0; i < byte.MaxValue; i++) {
                availableSyncObjectIDs.Enqueue(i);
            }

            instances.Add(UniqueID, this);
            Registered = true;

            OnRegister();
            OnRegistered?.Invoke();

            return UniqueID;
        }

        public void UnRegister() {
            if (!Registered) { return; }

            while(SyncObjects.Count > 0) {
               UnregisterSyncObject(SyncObjects[syncObjects.Keys.FirstOrDefault()]);
            }

            availableIDs.Enqueue(UniqueID);

            instances.Remove(UniqueID);
            Registered = false;

            OnUnregister();
            OnUnregistered?.Invoke();
        }

        #region Spawning

        public static T SyncSpawn<T>(ISpawnData spawnData, ushort? ownerID = null) where T : NetworkObject {
            NetworkObject spawnedObject = spawnData.Spawn();
            spawnedObject.Register(null, ownerID, spawnData);

            SendSpawn(spawnData, spawnedObject.UniqueID, ownerID);

            return spawnedObject as T;
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

            if (instances.ContainsKey(uniqueID)) { return; }

            NetworkObject spawnedObject = spawnData.Spawn();
            spawnedObject.Register(uniqueID, ownerID, spawnData);
        }

        #endregion

        #region Destroying

        public static void SyncDestroy(NetworkObject target) {
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

            NetworkObject targetObject = instances[uniqueID];

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

            NetworkObject targetObject = instances[uniqueID];

            if (targetObject.Owned && targetObject.OwnerID != message.SenderPeerID) { return; }

            targetObject.OwnerID = isOwned 
                ? newPeerID
                : null;
        }

        public void Claim() => GiveToPeer(Peer.Info.UniqueID);
        public void Forfeit() => GiveToPeer(null);
        public void GiveToRandomPeer() => GiveToPeer(PeerNetwork.GetElectedPeer(random.Next(int.MaxValue)));
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

            NetworkObject targetObject = instances[uniqueID];

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

            NetworkObject targetObject = instances[uniqueID];

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

                    GiveToPeer(PeerNetwork.GetElectedPeer(UniqueID), true);
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

            NetworkObject parent = instances[parentUniqueID];

            if (parent.Owned && parent.OwnerID != message.SenderPeerID) { return; }

            SyncObject syncObject = parent.SyncObjects[syncObjectID];

            syncObject.Deserialize(message.SenderPeerID, message.Buffer);
        }

        public void RegisterSyncObject(SyncObject syncObject) {
            byte uniqueID = availableSyncObjectIDs.Dequeue();
            SyncObjects[uniqueID] = syncObject;

            syncObject.OnRegister(uniqueID);
        }

        public void UnregisterSyncObject(SyncObject syncObject) {
            SyncObjects.Remove(syncObject.UniqueID);
            availableSyncObjectIDs.Enqueue(syncObject.UniqueID);

            syncObject.OnUnregister();
        }

        public void UpdateSyncObjects() {
            foreach (KeyValuePair<byte, SyncObject> syncObjectPair in SyncObjects) {
                syncObjectPair.Value.Update();
            }
        }

        #endregion

        #region Checks

        protected static void ExistenceCheck(ushort uniqueID) { 
            if (!instances.ContainsKey(uniqueID)) { throw new HiHiException($"Received a {nameof(PeerMessageType)} targeting a {nameof(NetworkObject)} with ID {uniqueID}, which doesn't exist locally."); } 
        }

        protected void RegistrationCheck() {
            if(!Registered) { throw new HiHiException($"{nameof(NetworkObject)} is not registered."); }
        }

        protected void AuthorizationCheck() {
            if (!Authorized) { throw new HiHiException($"Local {nameof(Peer)} is not authorized."); }
        }

        #endregion

        #region Virtual & Abstract

        protected virtual void OnRegister() { }
        protected virtual void OnUnregister() { }
        protected virtual void OnUpdate() { }

        #endregion
    }
}
