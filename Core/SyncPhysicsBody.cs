﻿using HiHi.Common;
using HiHi.Serialization;

/*
 * ANTI-CAPITALIST SOFTWARE LICENSE (v 1.4)
 *
 * Copyright © 2023 Pelle Bruinsma
 * 
 * This is anti-capitalist software, released for free use by individuals and organizations that do not operate by capitalist principles.
 *
 * Permission is hereby granted, free of charge, to any person or organization (the "User") obtaining a copy of this software and associated documentation files (the "Software"), to use, copy, modify, merge, distribute, and/or sell copies of the Software, subject to the following conditions:
 * 
 * 1. The above copyright notice and this permission notice shall be included in all copies or modified versions of the Software.
 * 
 * 2. The User is one of the following:
 *    a. An individual person, laboring for themselves
 *    b. A non-profit organization
 *    c. An educational institution
 *    d. An organization that seeks shared profit for all of its members, and allows non-members to set the cost of their labor
 *    
 * 3. If the User is an organization with owners, then all owners are workers and all workers are owners with equal equity and/or equal vote.
 * 
 * 4. If the User is an organization, then the User is not law enforcement or military, or working for or under either.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT EXPRESS OR IMPLIED WARRANTY OF ANY KIND, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
namespace HiHi {
    public class SyncPhysicsBody : SyncTransform {
        public HiHiVector3 LinearVelocity {
            get {
                return newLinearVelocity;
            }
            set {
                if (!Authorized) { throw new HiHiException($"Attempted to set {nameof(LinearVelocity)} while {nameof(SyncObject)}.{nameof(Authorized)} is false."); }

                Dirty = Dirty
                    ? true
                    : !newLinearVelocity.Equals(value);

                newLinearVelocity = value;
            }
        }

        public HiHiVector3 AngularVelocity {
            get {
                return newAngularVelocity;
            }
            set {
                if (!Authorized) { throw new HiHiException($"Attempted to set {nameof(AngularVelocity)} while {nameof(SyncObject)}.{nameof(Authorized)} is false."); }

                Dirty = Dirty
                    ? true
                    : !newAngularVelocity.Equals(value);

                newAngularVelocity = value;
            }
        }

        public bool Sleeping { get; set; } = false;

        protected HiHiVector3 newLinearVelocity;
        protected HiHiVector3 newAngularVelocity;

        public SyncPhysicsBody(INetworkObject parent) : base(parent) { }

        public void Set(HiHiVector3? position = null, HiHiQuaternion? rotation = null, HiHiVector3? scale = null, HiHiVector3? velocity = null, HiHiVector3? angularVelocity = null) {
            this.Position = position ?? newPosition;
            this.Rotation = rotation ?? newRotation;
            this.Scale = scale ?? newScale;
            this.LinearVelocity = velocity ?? newLinearVelocity;
            this.AngularVelocity = angularVelocity ?? newAngularVelocity;
        }

        public override void Update() {
            if (Sleeping) { return; }

            base.Update();
        }

        public override void Serialize(BitBuffer buffer) {
            newLinearVelocity.Serialize(buffer);
            newAngularVelocity.Serialize(buffer);

            base.Serialize(buffer);
        }

        public override void Deserialize(BitBuffer buffer) {
            newLinearVelocity = newLinearVelocity.Deserialize(buffer);
            newAngularVelocity = newAngularVelocity.Deserialize(buffer);

            base.Deserialize(buffer);
        }
    }
}
