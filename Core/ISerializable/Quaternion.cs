using HiHi.Serialization;

namespace HiHi {
    public partial struct HiHiQuaternion : ISerializable {
        public float X = 0f;
        public float Y = 0f;
        public float Z = 0f;
        public float W = 1f;

        public HiHiQuaternion() { }
        public HiHiQuaternion(float X, float Y, float Z, float W) {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
            this.W = W;
        }

        void ISerializable.Serialize(BitBuffer buffer) {
            buffer.AddUShort(HalfPrecision.Quantize(X));
            buffer.AddUShort(HalfPrecision.Quantize(Y));
            buffer.AddUShort(HalfPrecision.Quantize(Z));
            buffer.AddUShort(HalfPrecision.Quantize(W));
        }

        void ISerializable.Deserialize(BitBuffer buffer) {
            X = HalfPrecision.Dequantize(buffer.ReadUShort());
            Y = HalfPrecision.Dequantize(buffer.ReadUShort());
            Z = HalfPrecision.Dequantize(buffer.ReadUShort());
            W = HalfPrecision.Dequantize(buffer.ReadUShort());
        }
    }
}