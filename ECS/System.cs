using System.Collections.Generic;

namespace Dullahan.ECS {
    public abstract class System {
        public IEnumerable<System> dependencies { get; private set; }
        public ICollection<System> dependants { get; private set; }

        public int tick { get; private set; }
        public void Tick() {
            // TODO
        }
    }
}