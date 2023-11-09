using HiHi.Common;
using HiHi.Serialization;

namespace HiHi {
    public class SyncPhysicsBody : SyncTransform {
        public HiHiVector3 LinearVelocity {
            get {
                return newLinearVelocity;
            }
            set {
                if (!Authorized) { throw new HiHiException($"Attempted to set {nameof(LinearVelocity)} while {nameof(SyncObject)}.{nameof(Authorized)} is false."); }

                Dirty = Dirty
                    ? true
                    : !newLinearVelocity.Equals(value);

                newLinearVelocity = value;
            }
        }

        public HiHiVector3 AngularVelocity {
            get {
                return newAngularVelocity;
            }
            set {
                if (!Authorized) { throw new HiHiException($"Attempted to set {nameof(AngularVelocity)} while {nameof(SyncObject)}.{nameof(Authorized)} is false."); }

                Dirty = Dirty
                    ? true
                    : !newAngularVelocity.Equals(value);

                newAngularVelocity = value;
            }
        }

        public bool Sleeping { get; set; } = false;

        protected HiHiVector3 newLinearVelocity;
        protected HiHiVector3 newAngularVelocity;

        public SyncPhysicsBody(INetworkObject parent) : base(parent) { }

        public void Set(HiHiVector3? position = null, HiHiQuaternion? rotation = null, HiHiVector3? scale = null, HiHiVector3? velocity = null, HiHiVector3? angularVelocity = null) {
            this.Position = position ?? newPosition;
            this.Rotation = rotation ?? newRotation;
            this.Scale = scale ?? newScale;
            this.LinearVelocity = velocity ?? newLinearVelocity;
            this.AngularVelocity = angularVelocity ?? newAngularVelocity;
        }

        public override void Update() {
            if (Sleeping) { return; }

            base.Update();
        }

        public override void Serialize(BitBuffer buffer) {
            newLinearVelocity.Serialize(buffer);
            newAngularVelocity.Serialize(buffer);

            base.Serialize(buffer);
        }

        public override void Deserialize(BitBuffer buffer) {
            newLinearVelocity = newLinearVelocity.Deserialize(buffer);
            newAngularVelocity = newAngularVelocity.Deserialize(buffer);

            base.Deserialize(buffer);
        }
    }
}
