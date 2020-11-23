using System;
using System.Collections.Generic;

using UnityEngine;

namespace Dullahan.Unity {
    public abstract class PhysicsSystem : ECS.System {
        protected abstract IEnumerable<Tuple<IRigidbodyComponent, IBoxColliderComponent>> rigidbodyComponents { get; }


    }
}