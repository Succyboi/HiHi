using HiHi.Serialization;
using HiHi.Common;
using System;

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
