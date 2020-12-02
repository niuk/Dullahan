using System.Collections.Generic;
using Dullahan.ECS;

namespace TestServer.Source.Systems {
    [TickBefore(typeof(MovementSystem))]
    public abstract class InputSystem : ISystem {
        public Dictionary<int, (int, int)> inputsById = new Dictionary<int, (int, int)>();

        public virtual void Tick() {

        }
    }
}
