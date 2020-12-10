﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using static Dullahan.Generator.Generator;

namespace Dullahan.Generator {
    static class WorldGenerator {
        public static string GenerateWorld(string @namespace) {
            var world = $@"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace {@namespace} {{
    public sealed partial class World : IReadOnlyDictionary<int, (World, int)>, {string.Join(", ", GetIWorldTypes().Select(t => t.FullName))} {{
        // ticks and ticking
        private int previousTick;
        private int nextTick;
        private readonly SortedSet<int> ticks = new SortedSet<int> {{ 0 }};

        private bool AddTick(int tick) {{
            return ticks.Add(tick);
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

            foreach (var IWorldType in GetIWorldTypes()) {
                var getters = IWorldType.GetMethods().Where(method => method.Name.StartsWith("get_"));
                foreach (var getter in getters) {
                    if (!getter.ReturnType.IsAssignableTo(typeof(ECS.ISystem))) {
                        throw new InvalidProgramException($"World property {getter.Name[4..]} must return a system type.");
                    }
                }

                var systemTypes = getters.Select(getter => getter.ReturnType);

                var dependenciesBySystemType = new Dictionary<Type, HashSet<Type>>();
                foreach (var systemType in systemTypes) {
                    Console.WriteLine($"Adding property \"{systemType.Name}\" to World class...");
                    world += $@"
        public {systemType.FullName} {systemType.Name.Decapitalize()} {{ get; }} = new {systemType.Name}_Implementation();
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

                world += $@"
        void {IWorldType.Name}.Tick(int previousTick, int nextTick) {{
            if (previousTick != nextTick - 1) {{
                throw new InvalidOperationException($""Can't compute from tick {{previousTick}} to tick {{nextTick}}. Can only compute one tick at a time."");
            }}

            lock (this) {{
                if (!ticks.Contains(previousTick)) {{
                    throw new InvalidOperationException($""Tick {{previousTick}} does not yet exist."");
                }}

                this.previousTick = previousTick;
                this.nextTick = nextTick;
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
                AddTick(nextTick);
            }
        }
";
            }

            world += @"
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
                var world = worldAtOldTick.Item1;
                if (world == null) {{
                    world = worldAtNewTick.Item1;
                    if (world == null) {{
                        throw new InvalidOperationException(""Cannot diff two null worlds."");
                    }}
                }} else {{
                    if (world != worldAtNewTick.Item1) {{
                        throw new InvalidOperationException(""Can only diff the same world at different ticks."");
                    }}
                }}

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
                lock (world) {{
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
                        entityDiffer.Diff((entity, oldTick), (entity, newTick), writer);
                    }}
                }}

                return changedCount > 0 || disposed.Count > 0 || constructed.Count > 0;
            }}

            public void Patch(ref (World, int) worldAtTick, BinaryReader reader) {{
                var world = worldAtTick.Item1;
                if (world == null) {{
                    world = new World();
                }}

                var tick = worldAtTick.Item2;
                int oldTick = reader.ReadInt32();
                int newTick = reader.ReadInt32();
                if (tick != oldTick) {{
                    throw new InvalidOperationException($""World is at tick {{tick}} but patch is from tick {{oldTick}} to {{newTick}}."");
                }}

                lock (world) {{
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
                }}

                worldAtTick.Item1 = world;
                worldAtTick.Item2 = newTick;
            }}
        }}
    }}
}}
";
        }
    }
}
