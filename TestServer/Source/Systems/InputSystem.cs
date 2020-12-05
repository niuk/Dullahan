using System.Collections.Generic;
using Dullahan.ECS;

namespace TestServer {
    [TickBefore(typeof(MovementSystem))]
    public abstract class InputSystem : ISystem {
        public Dictionary<int, (int, int)> inputsById = new Dictionary<int, (int, int)>();

        protected abstract IEnumerable<IInputComponent> inputComponents { get; }
        
        public virtual void Tick() {

        }
    }
}
