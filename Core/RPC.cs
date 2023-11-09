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
    public class RPC<T0, T1, T2, T3> : RPCBase {
        T0 p0;
        T1 p1;
        T2 p2;
        T3 p3;

        public RPC(INetworkObject parent) : base(parent) { }

        public void Invoke(T0 p0, T1 p1, T2 p2, T3 p3, ushort? targetPeerID = null) {
            if (!Authorized) { throw new HiHiException($"Attempted to invoke {nameof(RPC)} while {nameof(RPC)}.{nameof(Authorized)} is false."); }

            this.p0 = p0;
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;

            Synchronize(targetPeerID);
        }

        public override void Serialize(BitBuffer buffer) {
            p0.Serialize(buffer);
            p1.Serialize(buffer);
            p2.Serialize(buffer);
            p3.Serialize(buffer);

            action?.DynamicInvoke(p0, p1, p2, p3);

            base.Serialize(buffer);
        }

        public override void Deserialize(BitBuffer buffer) {
            p0 = p0.Deserialize(buffer);
            p1 = p1.Deserialize(buffer);
            p2 = p2.Deserialize(buffer);
            p3 = p3.Deserialize(buffer);

            action?.DynamicInvoke(p0, p1, p2, p3);

            base.Deserialize(buffer);
        }
    }

    public class RPC<T0, T1, T2> : RPCBase {
        T0 p0;
        T1 p1;
        T2 p2;

        public RPC(INetworkObject parent) : base(parent) { }

        public void Invoke(T0 p0, T1 p1, T2 p2, ushort? targetPeerID = null) {
            if (!Authorized) { throw new HiHiException($"Attempted to invoke {nameof(RPC)} while {nameof(RPC)}.{nameof(Authorized)} is false."); }

            this.p0 = p0;
            this.p1 = p1;
            this.p2 = p2;

            Synchronize(targetPeerID);
        }

        public override void Serialize(BitBuffer buffer) {
            p0.Serialize(buffer);
            p1.Serialize(buffer);
            p2.Serialize(buffer);

            action?.DynamicInvoke(p0, p1, p2);

            base.Serialize(buffer);
        }

        public override void Deserialize(BitBuffer buffer) {
            p0 = p0.Deserialize(buffer);
            p1 = p1.Deserialize(buffer);
            p2 = p2.Deserialize(buffer);

            action?.DynamicInvoke(p0, p1, p2);

            base.Deserialize(buffer);
        }
    }

    public class RPC<T0, T1> : RPCBase {
        T0 p0;
        T1 p1;

        public RPC(INetworkObject parent) : base(parent) { }

        public void Invoke(T0 p0, T1 p1, ushort? targetPeerID = null) {
            if (!Authorized) { throw new HiHiException($"Attempted to invoke {nameof(RPC)} while {nameof(RPC)}.{nameof(Authorized)} is false."); }

            this.p0 = p0;
            this.p1 = p1;

            Synchronize(targetPeerID);
        }

        public override void Serialize(BitBuffer buffer) {
            p0.Serialize(buffer);
            p1.Serialize(buffer);

            action?.DynamicInvoke(p0, p1);

            base.Serialize(buffer);
        }

        public override void Deserialize(BitBuffer buffer) {
            p0 = p0.Deserialize(buffer);
            p1 = p1.Deserialize(buffer);

            action?.DynamicInvoke(p0, p1);

            base.Deserialize(buffer);
        }
    }

    public class RPC<T0> : RPCBase {
        T0 p0;

        public RPC(INetworkObject parent) : base(parent) { }

        public void Invoke(T0 p0, ushort? targetPeerID = null) {
            if (!Authorized) { throw new HiHiException($"Attempted to invoke {nameof(RPC)} while {nameof(RPC)}.{nameof(Authorized)} is false."); }

            this.p0 = p0;

            Synchronize(targetPeerID);
        }

        public override void Serialize(BitBuffer buffer) {
            p0.Serialize(buffer);

            action?.DynamicInvoke(p0);

            base.Serialize(buffer);
        }

        public override void Deserialize(BitBuffer buffer) {
            p0 = p0.Deserialize(buffer);

            action?.DynamicInvoke(p0);

            base.Deserialize(buffer);
        }
    }

    public class RPC : RPCBase {
        public RPC(INetworkObject parent) : base(parent) { }

        public void Invoke(ushort? targetPeerID = null) {
            if (!Authorized) { throw new HiHiException($"Attempted to invoke {nameof(RPC)} while {nameof(RPC)}.{nameof(Authorized)} is false."); }

            Synchronize(targetPeerID);
        }

        public override void Serialize(BitBuffer buffer) {
            action?.DynamicInvoke();

            base.Serialize(buffer);
        }

        public override void Deserialize(BitBuffer buffer) {
            action?.DynamicInvoke();

            base.Deserialize(buffer);
        }
    }

    public abstract class RPCBase : SyncObject {
        public Delegate Action { 
            get {
                return action;
            }
            set {
                action = value;
            }
        }

        protected Delegate action;

        public RPCBase(INetworkObject parent) : base(parent) { }

        public override void Update() { }
    }
}
