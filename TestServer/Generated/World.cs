/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TestGame {
    public sealed partial class World : IReadOnlyDictionary<int, (World, int)>, TestGame.IClientWorld, TestGame.IServerWorld {
        // ticks and ticking
        private int previousTick;
        private int nextTick;
        private readonly SortedSet<int> ticks = new SortedSet<int> { 0 };

        private bool AddTick(int tick) {
            return ticks.Add(tick);
        }

        // IReadonlyDictionary implementation
        public IEnumerable<int> Keys => ticks;
        public IEnumerable<(World, int)> Values => Keys.Select(key => (this, key));
        public int Count => ticks.Count;
        public (World, int) this[int key] => (this, key);

        public bool ContainsKey(int key) {
            return ticks.Contains(key);
        }

        public bool TryGetValue(int key, out (World, int) value) {
            value = (this, key);
            return ticks.Contains(key);
        }

        public IEnumerator<KeyValuePair<int, (World, int)>> GetEnumerator() {
            return ticks.Select(key => new KeyValuePair<int, (World, int)>(key, (this, key))).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        // meat and potatoes
        private readonly Dictionary<Guid, Entity> entitiesById = new Dictionary<Guid, Entity>();

        public TestGame.VisualizationSystem visualizationSystem { get; } = new VisualizationSystem_Implementation();

        void IClientWorld.Tick(int previousTick, int nextTick) {
            if (previousTick != nextTick - 1) {
                throw new InvalidOperationException($"Can't compute from tick {previousTick} to tick {nextTick}. Can only compute one tick at a time.");
            }

            lock (this) {
                if (!ticks.Contains(previousTick)) {
                    throw new InvalidOperationException($"Tick {previousTick} does not yet exist.");
                }

                this.previousTick = previousTick;
                this.nextTick = nextTick;

                visualizationSystem.Tick();

                AddTick(nextTick);
            }
        }

        public TestGame.MovementSystem movementSystem { get; } = new MovementSystem_Implementation();

        void IServerWorld.Tick(int previousTick, int nextTick) {
            if (previousTick != nextTick - 1) {
                throw new InvalidOperationException($"Can't compute from tick {previousTick} to tick {nextTick}. Can only compute one tick at a time.");
            }

            lock (this) {
                if (!ticks.Contains(previousTick)) {
                    throw new InvalidOperationException($"Tick {previousTick} does not yet exist.");
                }

                this.previousTick = previousTick;
                this.nextTick = nextTick;

                movementSystem.Tick();

                AddTick(nextTick);
            }
        }

    }
}
