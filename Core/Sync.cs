using HiHi.Serialization;

namespace HiHi {
    public class Sync<T> : SyncObject {
        public T Value {
            get {
                return value;
            }
            set {
                AuthorizationCheck();

                Dirty = Dirty
                    ? true
                    : !this.value.Equals(value);

                this.value = value;
            }
        }
        public bool Dirty { get; private set; }

        protected T value;

        public Sync(INetworkObject parent, T value = default) : base(parent) {
            this.value = value;
        }

        public override void Update() {
            if (!Authorized) { return; }

            if (Dirty) {
                Synchronize();
            }
        }

        public override void Serialize(BitBuffer buffer) {
            value.Serialize(buffer);

            Dirty = false;

            base.Serialize(buffer);
        }

        public override void Deserialize(BitBuffer buffer) {
            value = value.Deserialize(buffer);

            base.Deserialize(buffer);
        }
    }
}
