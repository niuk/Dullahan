using System;
using System.Collections.Generic;

using UnityEngine;

namespace Dullahan.Unity {
    public abstract class PhysicsSystem : ECS.ISystem {
        public abstract int tick { get; }

        protected abstract IEnumerable<Tuple<IRigidbodyComponent, IBoxColliderComponent, ISphereColliderComponent>> tuples { get; }
        protected abstract IEnumerable<ISphereColliderComponent> spheres { get; }

        public virtual void Tick() {
            throw new NotImplementedException();
        }
    }
}