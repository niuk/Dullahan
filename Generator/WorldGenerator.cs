using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dullahan.Generator {
    static class WorldGenerator {
        public static string GenerateWorld(string @namespace) {
            var world = $@"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace {@namespace} {{
    public sealed partial class World : IReadOnlyDictionary<int, (World, int)> {{
        // ticks and ticking
        public int tick => ticks.Max;
        private readonly SortedSet<int> ticks = new SortedSet<int>();

        public void AddTick(int tick) {{
            ticks.Add(tick);
        }}

        // IReadonlyDictionary implementation
        public IEnumerable<int> Keys => ticks;
        public IEnumerable<(World, int)> Values => Keys.Select(key => (this, key));
        public int Count => ticks.Count;
        public (World, int) this[int key] => (this, key);

        public bool ContainsKey(int key) {{
            return ticks.Contains(key);
        }}

        public bool TryGetValue(int key, out (World, int) value) {{
            value = (this, key);
            return ticks.Contains(key);
        }}

        public IEnumerator<KeyValuePair<int, (World, int)>> GetEnumerator() {{
            return ticks.Select(key => new KeyValuePair<int, (World, int)>(key, (this, key))).GetEnumerator();
        }}

        IEnumerator IEnumerable.GetEnumerator() {{
            return GetEnumerator();
        }}

        // meat and potatoes
        private readonly Dictionary<Guid, Entity> entitiesById = new Dictionary<Guid, Entity>();
";

            var systemTypes = Generator.GetSystemTypes();
            var dependenciesBySystemType = new Dictionary<Type, HashSet<Type>>();
            foreach (var systemType in systemTypes) {
                Console.WriteLine($"Adding property \"{systemType.Name}\" to World class...");
                world += $@"
        public readonly {systemType.FullName} {systemType.Name.Decapitalize()} = new {systemType.Name}_Implementation();
";

                var newDependencies = systemType.GetCustomAttributes<ECS.TickAfter>().Select(attr => attr.systemType);
                if (dependenciesBySystemType.TryGetValue(systemType, out HashSet<Type> oldDependencies)) {
                    oldDependencies.UnionWith(newDependencies);
                } else {
                    dependenciesBySystemType.Add(systemType, new HashSet<Type>(newDependencies));
                }

                foreach (var dependant in systemType.GetCustomAttributes<ECS.TickBefore>().Select(attr => attr.systemType)) {
                    if (dependenciesBySystemType.TryGetValue(dependant, out oldDependencies)) {
                        oldDependencies.UnionWith(new[] { systemType });
                    } else {
                        dependenciesBySystemType.Add(dependant, new HashSet<Type>(new[] { systemType }));
                    }
                }
            }

            world += @"
        public void Tick() {
            ticks.Add(tick + 1);
";

            var untickedSystemTypes = new HashSet<Type>(systemTypes);
            while (untickedSystemTypes.Count > 0) {
                var tickedSystemTypes = new HashSet<Type>();

                foreach (var untickedSystemType in untickedSystemTypes) {
                    if (dependenciesBySystemType.TryGetValue(untickedSystemType, out HashSet<Type> dependencies)) {
                        if (dependencies.Count > 0) {
                            continue;
                        } else {
                            tickedSystemTypes.Add(untickedSystemType);
                        }
                    } else {
                        tickedSystemTypes.Add(untickedSystemType);
                    }

                    world += $@"
            {untickedSystemType.Name.Decapitalize()}.Tick();
";
                }

                if (tickedSystemTypes.Count == 0) {
                    // Couldn't tick anything
                    throw new InvalidProgramException($"Circular system dependencies aren't allowed: {string.Join(", ", untickedSystemTypes)}");
                }

                untickedSystemTypes.ExceptWith(tickedSystemTypes);
                foreach (var systemType in systemTypes) {
                    if (dependenciesBySystemType.TryGetValue(systemType, out HashSet<Type> dependencies)) {
                        dependencies.ExceptWith(tickedSystemTypes);
                    }
                }
            }

            world += @"
        }
    }
}
";
            return world;
        }

        public static string GenerateWorldDiffer(string @namespace) {
            return $@"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.Collections.Generic;
using System.IO;

namespace {@namespace} {{
    partial class World {{
        public class Differ : IDiffer<(World, int)> {{
            private readonly Entity.Differ entityDiffer = new Entity.Differ();

            public bool Diff((World, int) worldAtOldTick, (World, int) worldAtNewTick, BinaryWriter writer) {{
                if (worldAtOldTick.Item1 != worldAtNewTick.Item1) {{
                    throw new InvalidOperationException(""Can only diff the same world at different ticks or a null world with a new world."");
                }}

                var world = worldAtOldTick.Item1;
                int oldTick = worldAtOldTick.Item2;
                int newTick = worldAtNewTick.Item2;

                writer.Write(oldTick);
                writer.Write(newTick);

                // reserve room for count of changed entities
                int startOffset = writer.GetOffset();
                writer.Write(0);
                int changedCount = 0;
                var disposed = new HashSet<Guid>();
                var constructed = new HashSet<Entity>();
                foreach (var entity in world.entitiesById.Values) {{
                    if (entity.constructionTick <= oldTick && oldTick < entity.disposalTick) {{
                        // entity exists in old world
                        if (entity.constructionTick <= newTick && newTick < entity.disposalTick) {{
                            // entity also exists in new world
                            int keyOffset = writer.GetOffset(); // preemptively write the key; erase when entities don't differ
                            writer.Write(entity.id.ToByteArray());
                            if (entityDiffer.Diff((entity, oldTick), (entity, newTick), writer)) {{
                                ++changedCount;
                            }} else {{
                                writer.SetOffset(keyOffset);
                            }}
                        }} else {{
                            // entity was disposed
                            disposed.Add(entity.id);
                        }}
                    }} else {{
                        // entity does not exist in old world
                        if (entity.constructionTick <= newTick && newTick < entity.disposalTick) {{
                            // entity exists in new world
                            constructed.Add(entity);
                        }} else {{
                            // entity existed at some point but not in either world
                        }}
                    }}
                }}

                int savedOffset = writer.GetOffset();
                writer.SetOffset(startOffset);
                writer.Write(changedCount);
                writer.SetOffset(savedOffset);

                writer.Write(disposed.Count);
                foreach (var id in disposed) {{
                    writer.Write(id.ToByteArray());
                }}

                writer.Write(constructed.Count);
                foreach (var entity in constructed) {{
                    entityDiffer.Diff((default, oldTick), (entity, newTick), writer);
                }}

                return changedCount > 0 || disposed.Count > 0 || constructed.Count > 0;
            }}

            public void Patch(ref (World, int) worldAtTick, BinaryReader reader) {{
                var world = worldAtTick.Item1;
                var tick = worldAtTick.Item2;

                int oldTick = reader.ReadInt32();
                int newTick = reader.ReadInt32();
                if (tick != oldTick) {{
                    throw new InvalidOperationException($""World is at tick {{tick}} but patch is from tick {{oldTick}} to {{newTick}}."");
                }}

                world.AddTick(newTick);

                int changedCount = reader.ReadInt32();
                for (int i = 0; i < changedCount; ++i) {{
                    var id = new Guid(reader.ReadBytes(16));
                    var entityAtTick = (world.entitiesById[id], tick);
                    entityDiffer.Patch(ref entityAtTick, reader);
                    world.entitiesById[id] = entityAtTick.Item1;
                }}

                int disposedCount = reader.ReadInt32();
                for (int i = 0; i < disposedCount; ++i) {{
                    world.entitiesById.Remove(new Guid(reader.ReadBytes(16)));
                }}

                int constructedCount = reader.ReadInt32();
                for (int i = 0; i < constructedCount; ++i) {{
                    var entityAtTick = (new Entity(world, new Guid(reader.ReadBytes(16))), tick);
                    entityDiffer.Patch(ref entityAtTick, reader);
                }}

                worldAtTick.Item2 = newTick;
            }}
        }}
    }}
}}
";
        }
    }
}
