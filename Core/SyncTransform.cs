using System;
using HiHi.Common;
using HiHi.Serialization;

namespace HiHi {
    public class SyncTransform : SyncObject {
        public const float DEFAULT_SYNC_INTERVAL = 1f / 15f;
        public const float DEFAULT_INTERPOLATION_TIME = DEFAULT_SYNC_INTERVAL;
        public const float DEFAULT_TELEPORT_DISTANCE = 2f;
        public const float POSITION_TOLERANCE = 0f;
        public const float ROTATION_TOLERANCE = 0f;
        public const float SCALE_TOLERANCE = 0f;

        public float SyncInterval { get; set; } = DEFAULT_SYNC_INTERVAL;
        public float InterpolationTime { get; set; } = DEFAULT_INTERPOLATION_TIME;
        public float TeleportDistance { get; set; } = DEFAULT_TELEPORT_DISTANCE;

        public float InterpolationT {
            get {
                return interpolate 
                    ? Math.Clamp(HiHiTime.Time - deserializedTime / InterpolationTime, 0f, 1f)
                    : 1f;
            }
        }
        public HiHiVector3 Position {
            get {
                return HiHiVector3.Lerp(oldPosition, newPosition, InterpolationT);
            }
            set {
                if (!Authorized) { throw new HiHiException($"Attempted to set {nameof(Position)} while {nameof(SyncObject)}.{nameof(Authorized)} is false."); }

                Dirty = Dirty
                    ? true
                    : !newPosition.Equals(value);
                
                newPosition = value;
                interpolate = false;
            }
        }
        public HiHiQuaternion Rotation {
            get {
                return HiHiQuaternion.Slerpni(oldRotation, newRotation, InterpolationT);
            }
            set {
                if (!Authorized) { throw new HiHiException($"Attempted to set {nameof(Rotation)} while {nameof(SyncObject)}.{nameof(Authorized)} is false."); }

                Dirty = Dirty
                    ? true
                    : !newRotation.Equals(value);

                newRotation = value;
                interpolate = false;
            }
        }
        public HiHiVector3 Scale {
            get {
                return HiHiVector3.Lerp(oldScale, newScale, InterpolationT);
            }
            set {
                if (!Authorized) { throw new HiHiException($"Attempted to set {nameof(Scale)} while {nameof(SyncObject)}.{nameof(Authorized)} is false."); }

                Dirty = Dirty
                    ? true
                    : !newScale.Equals(value);

                newScale = value;
                interpolate = false;
            }
        }
        public bool Dirty { get; protected set; }

        protected HiHiVector3 newPosition = new HiHiVector3();
        protected HiHiQuaternion newRotation = new HiHiQuaternion();
        protected HiHiVector3 newScale = new HiHiVector3();

        protected HiHiVector3 oldPosition = new HiHiVector3();
        protected HiHiQuaternion oldRotation = new HiHiQuaternion();
        protected HiHiVector3 oldScale = new HiHiVector3();

        protected float serializedTime;
        protected float deserializedTime;
        protected bool interpolate = true;

        public SyncTransform(INetworkObject parent) : base(parent) {}

        public void Set(HiHiVector3? position = null, HiHiQuaternion? rotation = null, HiHiVector3? scale = null) {
            this.Position = position ?? newPosition;
            this.Rotation = rotation ?? newRotation;
            this.Scale = scale ?? newScale;
        }

        public bool TryGetPosition(HiHiVector3 fromPosition, out HiHiVector3 returnedPosition) {
            if(HiHiVector3.Distance(fromPosition, newPosition) >= TeleportDistance) {
                returnedPosition = newPosition - fromPosition;
                return true; 
            }

            float oldT = Math.Clamp((HiHiTime.Time - HiHiTime.DeltaTime - deserializedTime) / InterpolationTime, float.NegativeInfinity, 1f);
            float newT = Math.Clamp((HiHiTime.Time - deserializedTime) / InterpolationTime, float.NegativeInfinity, 1f);

            if (oldT == newT && oldT > 0f) {
                returnedPosition = default;
                return false;
            }

            if (HiHiVector3.Distance(fromPosition, newPosition) <= POSITION_TOLERANCE) {
                returnedPosition = default;
                return false;
            }

            returnedPosition = HiHiVector3.LerpUnclamped(fromPosition, newPosition, newT - oldT);
            return true;
        }

        public bool TryGetRotation(HiHiQuaternion fromRotation, out HiHiQuaternion returnedRotation) {
            float oldT = Math.Clamp((HiHiTime.Time - HiHiTime.DeltaTime - deserializedTime) / InterpolationTime, float.NegativeInfinity, 1f);
            float newT = Math.Clamp((HiHiTime.Time - deserializedTime) / InterpolationTime, float.NegativeInfinity, 1f);

            if (oldT == newT && oldT > 0f) {
                returnedRotation = default;
                return false;
            }

            if (HiHiQuaternion.Dot(fromRotation, newRotation) <= ROTATION_TOLERANCE) {
                returnedRotation = default;
                return false;
            }

            returnedRotation = HiHiQuaternion.Slerpni(fromRotation, newRotation, newT - oldT);
            return true;
        }

        public bool TryGetScale(HiHiVector3 fromScale, out HiHiVector3 returnedScale) {
            if (HiHiVector3.Distance(fromScale, newScale) >= TeleportDistance) {
                returnedScale = newScale - fromScale;
                return true;
            }

            float oldT = Math.Clamp((HiHiTime.Time - HiHiTime.DeltaTime - deserializedTime) / InterpolationTime, float.NegativeInfinity, 1f);
            float newT = Math.Clamp((HiHiTime.Time - deserializedTime) / InterpolationTime, float.NegativeInfinity, 1f);

            if (oldT == newT && oldT > 0f) {
                returnedScale = default;
                return false;
            }

            if (HiHiVector3.Distance(fromScale, newScale) <= SCALE_TOLERANCE) {
                returnedScale = default;
                return false;
            }

            returnedScale = HiHiVector3.LerpUnclamped(fromScale, newScale, newT - oldT);
            return true;
        }

        public override void Update() {
            if(!Authorized) { return; }

            if (Dirty && HiHiTime.Time - deserializedTime > SyncInterval) {
                Synchronize();

                deserializedTime = HiHiTime.Time;
            }
        }

        public override void Serialize(BitBuffer buffer) {
            newPosition.Serialize(buffer);
            newRotation.Serialize(buffer);
            newScale.Serialize(buffer);

            serializedTime = HiHiTime.Time;

            base.Serialize(buffer);
        }

        public override void Deserialize(BitBuffer buffer) {
            oldPosition = newPosition;
            oldRotation = newRotation;
            oldScale = newScale;

            newPosition = newPosition.Deserialize(buffer);
            newRotation = newRotation.Deserialize(buffer);
            newScale = newScale.Deserialize(buffer);

            deserializedTime = HiHiTime.Time;
            interpolate = true;

            base.Deserialize(buffer);
        }
    }
}
