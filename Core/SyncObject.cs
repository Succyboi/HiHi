using HiHi.Serialization;
using HiHi.Common;
using System;

namespace HiHi {
    public abstract class SyncObject : ISerializable {
        public Action OnSerialize;
        public Action OnDeserialize;

        public byte UniqueID { get; private set; }
        public bool Registered { get; private set; }
        public ushort? OwnerID => parent.OwnerID;
        public bool Authorized => parent.Authorized;
        public bool OwnedLocally => parent.OwnedLocally;
        public bool Owned => parent.Owned;

        protected INetworkObject parent { get; set; }

        protected virtual bool RequiresAuthorization => true;

        public SyncObject(INetworkObject parent) {
            Register(parent);
        }

        public void Register(INetworkObject parent) {
            if (Registered) { return; }

            UniqueID = parent.RegisterSyncObject(this);
            this.parent = parent;

            Registered = true;
        }

        #region Checks

        protected void RegistrationCheck() {
            if (!Registered) { throw new HiHiException($"{nameof(INetworkObject)} is not registered."); }
        }

        protected void AuthorizationCheck() {
            if (RequiresAuthorization && !Authorized) { throw new HiHiException($"Local {nameof(Peer)} is not authorized."); }
        }

        #endregion

        #region Virtual

        public virtual void Synchronize(ushort? destinationPeer = null) {
            RegistrationCheck();
            AuthorizationCheck();

            PeerMessage message = NewMessage(destinationPeer);

            Serialize(message.Buffer);

            Peer.SendMessage(message);
        }

        public virtual void Deserialize(ushort senderPeerID, BitBuffer buffer) {
            Deserialize(buffer);
        }

        public virtual void Serialize(BitBuffer buffer) {
            OnSerialize?.Invoke();
        }

        public virtual void Deserialize(BitBuffer buffer) {
            OnDeserialize?.Invoke();
        }

        protected PeerMessage NewMessage(ushort? destinationPeer = null) {
            PeerMessage message = Peer.NewMessage(PeerMessageType.SOData, destinationPeer);
            message.Buffer.AddUShort(parent.UniqueID);
            message.Buffer.AddByte(UniqueID);

            return message;
        }

        #endregion

        #region Abstract

        public abstract void Update();

        #endregion
    }
}