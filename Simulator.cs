using System.Collections.Generic;
using System.Reflection;

namespace Dullahan {
    partial class Simulation {
        public abstract class Simulator<TSimulator> : ISimulator where TSimulator : Simulator<TSimulator> {
            public Simulation simulation { get; private set; }
            public IEnumerable<ISimulator> dependencies { get; private set; }
            public ICollection<ISimulator> dependants { get; private set; } = new HashSet<ISimulator>();
            public int tick { get; private set; } = 0;

            public Simulator(Simulation simulation, IEnumerable<ISimulator> dependencies) {
                this.simulation = simulation;
                simulation.simulatorsByType.Add(typeof(TSimulator), this);

                this.dependencies = dependencies;
                foreach (var dependency in dependencies) {
                    dependency.dependants.Add(this);
                }
            }

            public void Tick() {
                foreach (var dependency in dependencies) {
                    if (dependency.tick < tick) {
                        dependency.Tick();
                    }
                }

                foreach (var field in typeof(TSimulator).GetFields(BindingFlags.NonPublic)) {
                    if (field.GetCustomAttribute<GatherAttribute>(true) != null) {

                    }
                }

                Simulate();

                ++tick;
            }

            protected abstract void Simulate();
        }
    }
}