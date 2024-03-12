namespace HiHi {
    public class SyncNetworkObject : Sync<ushort> {
        public SyncNetworkObject(NetworkObject parent) : base(parent) { }

        public new NetworkObject Value {
            get {
                if (!NetworkObject.Exists(base.Value)) { return null; }

                return NetworkObject.GetByID(base.Value);
            }
            set {
                base.Value = value.UniqueID;

                if(Value == null) { return; }

                Value.OnUnregistered -= HandleUnregistered;
                Value.OnUnregistered += HandleUnregistered;
            }
        }

        protected void HandleUnregistered() {
            base.Value = 0;

            Value.OnUnregistered -= HandleUnregistered;
        }
    }
}