using System.Collections.Generic;

namespace Dullahan.ECS {
    public abstract class System {
        public IEnumerable<System> dependencies { get; protected set; }
        public ICollection<System> dependants { get; protected set; }

        public int tick { get; protected set; } = 0;
        public abstract void Tick();
    }
}