#if GODOT

using Godot;
using HiHi.Serialization;

namespace HiHi {
    public partial class GodotSpawnData : Resource, ISpawnData {
        public int Index => helper.SpawnDataRegistry.IndexOf(this);

        [Export] public PackedScene Scene;

        private GodotHelper helper => Peer.Helper as GodotHelper;

        void ISpawnData.Serialize(BitBuffer buffer) {
            buffer.AddByte((byte)Index);
        }

        INetworkObject ISpawnData.Spawn() {
            Node spawnedNode = Scene.Instantiate();
            helper.AddChild(spawnedNode);

            return spawnedNode as INetworkObject;
        }
    }
}

#endif