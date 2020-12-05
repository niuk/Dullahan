using System.Collections.Generic;
using Dullahan.ECS;

namespace TestServer {
    [TickAfter(typeof(InputSystem))]
    public abstract class MovementSystem : ISystem {
        protected abstract IEnumerable<(IInputComponent, IPositionComponent)> controllables { get; }

        public virtual void Tick() {
            foreach (var (inputComponent, positionComponent) in controllables) {
                positionComponent.x += inputComponent.deltaX;
                positionComponent.y += inputComponent.deltaY;
            }
        }
    }
}
