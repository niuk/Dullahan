/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TestServer {
    public class World : IReadOnlyDictionary<int, (World, int)> {
        public int tick => ticks.Max;
        private readonly SortedSet<int> ticks = new SortedSet<int>();

        public void AddTick(int tick) {
            ticks.Add(tick);
        }

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

        public readonly Dictionary<Guid, Entity> entitiesById = new Dictionary<Guid, Entity>();

        public readonly TestServer.Source.Systems.InputSystem inputSystem = new InputSystem_Implementation();

        public readonly TestServer.Source.Systems.MovementSystem movementSystem = new MovementSystem_Implementation();

        public void Tick() {
            ticks.Add(tick + 1);

            // no mutual dependencies:

            inputSystem.Tick();

            // no mutual dependencies:

            movementSystem.Tick();

        }
    }
}
