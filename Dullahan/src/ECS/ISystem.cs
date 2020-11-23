using System.Collections.Generic;

namespace Dullahan.ECS {
    public interface ISystem {
        int tick { get; }
        void Tick();
    }
}