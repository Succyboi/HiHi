using HiHi.Serialization;

namespace HiHi {
    public partial struct HiHiVector2 : ISerializable {
        public float X = 0f;
        public float Y = 0f;

        public HiHiVector2() { }
        public HiHiVector2(float X, float Y) {
            this.X = X;
            this.Y = Y;
        }

        void ISerializable.Serialize(BitBuffer buffer) {
            buffer.AddUShort(HalfPrecision.Quantize(X));
            buffer.AddUShort(HalfPrecision.Quantize(Y));
        }

        void ISerializable.Deserialize(BitBuffer buffer) {
            X = HalfPrecision.Dequantize(buffer.ReadUShort());
            Y = HalfPrecision.Dequantize(buffer.ReadUShort());
        }
    }

    public partial struct HiHiVector3 : ISerializable {
        public float X = 0f;
        public float Y = 0f;
        public float Z = 0f;

        public HiHiVector3() { }
        public HiHiVector3(float X, float Y, float Z) {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        void ISerializable.Serialize(BitBuffer buffer) {
            buffer.AddUShort(HalfPrecision.Quantize(X));
            buffer.AddUShort(HalfPrecision.Quantize(Y));
            buffer.AddUShort(HalfPrecision.Quantize(Z));
        }

        void ISerializable.Deserialize(BitBuffer buffer) {
            X = HalfPrecision.Dequantize(buffer.ReadUShort());
            Y = HalfPrecision.Dequantize(buffer.ReadUShort());
            Z = HalfPrecision.Dequantize(buffer.ReadUShort());
        }
    }

    public partial struct HiHiVector4 : ISerializable {
        public float X = 0f;
        public float Y = 0f;
        public float Z = 0f;
        public float W = 0f;

        public HiHiVector4() { }
        public HiHiVector4(float X, float Y, float Z, float W) {
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
