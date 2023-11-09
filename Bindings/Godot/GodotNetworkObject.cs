#if GODOT

using Godot;
using System;

namespace HiHi{
    public abstract partial class GodotNetworkObject : Node, INetworkObject {
        #region NetworkObject Implementation

        Action INetworkObject.OnOwnershipChanged { get; set; }
        Action INetworkObject.OnAbandonmentPolicyChanged { get; set; }

        ISpawnData INetworkObject.OriginSpawnData { get; set; }

        bool INetworkObject.Registered { get; set; }
        ushort INetworkObject.UniqueID { get; set; }

        ushort? INetworkObject.ownerID { get; set; }
        ObjectAbandonmentPolicy INetworkObject.abandonmentPolicy { get; set; } = HiHiConfiguration.DEFAULT_ABANDONMENT_POLICY;
        SyncObject[] INetworkObject.syncObjects { get; set; }
        byte INetworkObject.syncObjectCount { get; set; }

        void INetworkObject.OnRegister() => OnRegister();
        void INetworkObject.OnUnregister() => OnUnregister();
        void INetworkObject.DestroyLocally() => QueueFree();
        void INetworkObject.Update() => Update();

        #endregion

        #region Godot

        public INetworkObject NetworkObject => this as INetworkObject;

        public override void _ExitTree() {
            base._ExitTree();

            if (NetworkObject.Registered) {
                NetworkObject.UnRegister();
            }
        }

        protected virtual void OnRegister() { }
        protected virtual void OnUnregister() { }
        protected virtual void Update() { }

        #endregion
    }
}

#endif