#if GODOT

namespace HiHi {
    public partial struct HiHiVector3 {
        public static implicit operator Godot.Vector3(HiHiVector3 from) => new Godot.Vector3(from.X, from.Y, from.Z);
        public static implicit operator HiHiVector3(Godot.Vector3 from) => new HiHiVector3(from.X, from.Y, from.Z);
    }

    public partial struct HiHiQuaternion {
        public static implicit operator Godot.Quaternion(HiHiQuaternion from) => new Godot.Quaternion(from.X, from.Y, from.Z, from.W);
        public static implicit operator HiHiQuaternion(Godot.Quaternion from) => new HiHiQuaternion(from.X, from.Y, from.Z, from.W);
    }
}

#endif