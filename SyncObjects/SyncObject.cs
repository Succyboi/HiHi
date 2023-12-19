using HiHi.Serialization;
using HiHi.Common;
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

            parent.RegisterSyncObject(this);
            this.parent = parent;
            Registered = true;
        }

        public void Unregister() {
            if (!Registered) { return; }

            parent.UnregisterSyncObject(this);

            UniqueID = default;
            parent = null;
            Registered = false;
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

        public virtual void OnRegister(byte uniqueID) {
            this.UniqueID = uniqueID;
        }

        public virtual void OnUnregister() { }

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