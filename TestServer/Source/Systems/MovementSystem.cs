using System;
using System.Collections.Generic;
using TestServer.Source.Components;

namespace TestServer.Source.Systems {
    public abstract class MovementSystem : Dullahan.ECS.ISystem {
        public abstract int tick { get; }

        protected abstract IEnumerable<Tuple<IInputComponent, IPositionComponent>> controllables { get; }

        public virtual void Tick() {
            throw new NotImplementedException();
        }
    }
}
