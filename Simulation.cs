using System;
using System.Collections.Generic;

namespace Dullahan {
    public partial class Simulation {
        private readonly Dictionary<Type, ISimulator> simulatorsByType = new Dictionary<Type, ISimulator>();

        private int nextTick = 0;

        public void Tick() {
            foreach (var simulator in simulatorsByType)

            ++nextTick;
        }
    }
}