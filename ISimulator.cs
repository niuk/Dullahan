using System.Collections.Generic;

namespace Dullahan {
    public interface ISimulator {
        Simulation simulation { get; }
        IEnumerable<ISimulator> dependencies { get; }
        ICollection<ISimulator> dependants { get; }
        int tick { get; }
        void Tick();
    }
}