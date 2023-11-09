using HiHi.Serialization;

namespace HiHi {
    public interface IHelper {
        public void SerializeSpawnData(ISpawnData spawnData, BitBuffer buffer);
        public ISpawnData DeserializeSpawnData(BitBuffer buffer);
    }
}
