using System;
using System.Collections.Generic;
using TestServer.Source.Components;
using Dullahan.ECS;

namespace TestServer.Source.Systems {
    [TickAfter(typeof(InputSystem))]
    public abstract class MovementSystem : ISystem {
        protected abstract IEnumerable<Tuple<IInputComponent, IPositionComponent>> controllables { get; }

        public virtual void Tick() {
            foreach (var (inputComponent, positionComponent) in controllables) {
                positionComponent.x += inputComponent.deltaX;
                positionComponent.y += inputComponent.deltaY;
            }
        }
    }
}
