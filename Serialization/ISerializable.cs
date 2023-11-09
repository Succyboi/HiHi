namespace HiHi.Serialization {
    public interface ISerializable {
        void Serialize(BitBuffer buffer);
        void Deserialize(BitBuffer buffer);
    }
}