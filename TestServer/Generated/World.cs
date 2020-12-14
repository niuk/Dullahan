/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TestGame {
    public sealed partial class World : IReadOnlyDictionary<int, (World, int)>, TestGame.IClientWorld, TestGame.IServerWorld {
        // for deterministic entity creation and identification
        private int nextEntityId = 0;

        // ticks and ticking
        private int currentTick = 0;
        private readonly SortedSet<int> ticks = new SortedSet<int> { 0 };

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
        private readonly Dictionary<int, Entity> entitiesById = new Dictionary<int, Entity>();

        public TestGame.VisualizationSystem visualizationSystem { get; } = new VisualizationSystem_Implementation();

        void IClientWorld.Tick(int tick) {
            if (!ticks.Contains(tick - 1)) {
                throw new InvalidOperationException($"Tick {tick - 1} does not yet exist.");
            }

            currentTick = tick;

            visualizationSystem.Tick();

            ticks.Add(currentTick);
        }

        public TestGame.MovementSystem movementSystem { get; } = new MovementSystem_Implementation();

        void IServerWorld.Tick(int tick) {
            if (!ticks.Contains(tick - 1)) {
                throw new InvalidOperationException($"Tick {tick - 1} does not yet exist.");
            }

            currentTick = tick;

            movementSystem.Tick();

            ticks.Add(currentTick);
        }

    }
}
