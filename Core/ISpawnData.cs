using HiHi.Serialization;

namespace HiHi {
    public interface ISpawnData { 
        void Serialize(BitBuffer buffer);
        INetworkObject Spawn();
    }
}
