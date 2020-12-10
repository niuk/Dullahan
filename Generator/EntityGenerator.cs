using static Dullahan.Generator.Generator;

namespace Dullahan.Generator {
    static class EntityGenerator {
        public static string GenerateEntity(string @namespace) {
            var entity = $@"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;

namespace {@namespace} {{
    partial class World {{
        public sealed partial class Entity : IDisposable {{
            public readonly Guid id;

            public readonly World world;
            public readonly int constructionTick;
            public int disposalTick {{ get; private set; }}

            public Entity(World world) : this(world, Guid.NewGuid()) {{ }}

            public Entity(World world, Guid id) {{
                this.id = id;
                this.world = world;
                world.entitiesById.Add(id, this);
                constructionTick = world.nextTick;
                disposalTick = int.MaxValue;
            }}

            private Entity() {{ }}

            private void Dispose(bool disposing) {{
                if (disposalTick == int.MaxValue) {{
                    if (disposing) {{";

            foreach (var IComponentType in GetIComponentTypes()) {
                entity += $@"
                        {IComponentType.Name[1..].Decapitalize()}.Dispose();
";
            }

            entity += @"
                    }

                    disposalTick = world.nextTick;
                }
            }

            public void Dispose() {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
";

            foreach (var IComponentType in GetIComponentTypes()) {
                var componentTypeName = IComponentType.Name[1..];
                var componentPropertyName = componentTypeName.Decapitalize();
                entity += $@"
            private Ring<int> {componentPropertyName}_ticks = new Ring<int>();
            private Ring<{componentTypeName}> {componentPropertyName}_snapshots = new Ring<{componentTypeName}>();
            public {componentTypeName} {componentPropertyName} {{
                get {{
                    if ({componentPropertyName}_ticks.Count == 0) {{
                        return default;
                    }}

                    int index = {componentPropertyName}_ticks.BinarySearch(world.previousTick);
                    if (index < 0) {{
                        return {componentPropertyName}_snapshots[~index - 1];
                    }} else {{
                        return {componentPropertyName}_snapshots[index];
                    }}
                }}

                private set {{
                    if ({componentPropertyName} != value) {{
                        int index = {componentPropertyName}_ticks.BinarySearch(world.nextTick);
                        if (index < 0) {{
                            {componentPropertyName}_ticks.Insert(~index, world.nextTick);
                            {componentPropertyName}_snapshots.Insert(~index, value);
                        }} else {{
                            {componentPropertyName}_ticks[index] = world.nextTick;
                            {componentPropertyName}_snapshots[index] = value;
                        }}
                    }}
                }}
            }}
";
            }

            entity += @"
        }
    }
}";
            return entity;
        }

        public static string GenerateEntityDiffer(string @namespace) {
            // EntityDiffer.cs
            var entityDiffer = $@"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.IO;

namespace {@namespace} {{
    partial class World {{
        partial class Entity {{
            public sealed class Differ : IDiffer<(Entity, int)> {{";

            foreach (var IComponentType in GetIComponentTypes()) {
                var componentTypeName = IComponentType.Name[1..];
                entityDiffer += $@"
                private readonly {componentTypeName}.Differ {componentTypeName.Decapitalize()}Differ = new {componentTypeName}.Differ();";
            }

            entityDiffer += @"
                public bool Diff((Entity, int) entityAtOldTick, (Entity, int) entityAtNewTick, BinaryWriter writer) {
                    var entity = entityAtOldTick.Item1;
                    if (entity == null) {{
                        entity = entityAtNewTick.Item1;
                        if (entity == null) {{
                            throw new InvalidOperationException(""Cannot diff two null entities."");
                        }}
                    }} else {{
                        if (entity != entityAtNewTick.Item1) {{
                            throw new InvalidOperationException(""Can only diff the same entity at different ticks."");
                        }}
                    }}

                    int oldTick = entityAtOldTick.Item2;
                    int newTick = entityAtNewTick.Item2;
                    writer.Write(oldTick);
                    writer.Write(newTick);

                    byte dirtyFlags = 0;
                    byte deleteFlags = 0;
                    byte createFlags = 0;
                    int flagOffset = writer.GetOffset();
                    writer.Write(dirtyFlags);
                    writer.Write(createFlags);
                    writer.Write(deleteFlags);
";

            int propertyIndex = 0;
            foreach (var IComponentType in GetIComponentTypes()) {
                var componentTypeName = IComponentType.Name[1..];
                var componentPropertyName = componentTypeName.Decapitalize();
                entityDiffer += $@"
                    /* {componentPropertyName} */ {{
                        {componentTypeName} oldSnapshot;
                        int oldSnapshotIndex = entity.{componentPropertyName}_ticks.BinarySearch(oldTick);
                        if (oldSnapshotIndex < 0) {{
                            oldSnapshotIndex = ~oldSnapshotIndex - 1;
                        }}

                        if (oldSnapshotIndex < 0) {{
                            oldSnapshot = default;
                        }} else {{
                            oldSnapshot = entity.{componentPropertyName}_snapshots[oldSnapshotIndex];
                        }}

                        {componentTypeName} newSnapshot;
                        int newSnapshotIndex = entity.{componentPropertyName}_ticks.BinarySearch(newTick);
                        if (newSnapshotIndex < 0) {{
                            newSnapshotIndex = ~newSnapshotIndex - 1;
                        }}

                        if (newSnapshotIndex < 0) {{
                            newSnapshot = default;
                        }} else {{
                            newSnapshot = entity.{componentPropertyName}_snapshots[newSnapshotIndex];
                        }}

                        if (oldSnapshot != null) {{
                            if (newSnapshot != null) {{
                                int componentOffset = writer.GetOffset();
                                if ({componentPropertyName}Differ.Diff((oldSnapshot, oldTick), (newSnapshot, newTick), writer)) {{
                                    dirtyFlags |= 1 << {propertyIndex};
                                }} else {{
                                    writer.SetOffset(componentOffset);
                                }}
                            }} else {{
                                deleteFlags |= 1 << {propertyIndex};
                            }}
                        }} else {{
                            if (newSnapshot != null) {{
                                {componentPropertyName}Differ.Diff((default, oldTick), (newSnapshot, newTick), writer);
                                createFlags |= 1 << {propertyIndex};
                            }} else {{
                                // stayed null
                            }}
                        }}
                    }}
";
                ++propertyIndex;
            }

            entityDiffer += $@"
                    int savedOffset = writer.GetOffset();
                    writer.SetOffset(flagOffset);
                    writer.Write(dirtyFlags);
                    writer.Write(createFlags);
                    writer.Write(deleteFlags);
                    writer.SetOffset(savedOffset);

                    return dirtyFlags != 0 || createFlags != 0 || deleteFlags != 0;
                }}
";

            // EntityDiffer.Patch
            entityDiffer += @"
                public void Patch(ref (Entity, int) entityAtTick, BinaryReader reader) {
                    var entity = entityAtTick.Item1;
                    if (entity == null) {{
                        entity = new Entity();
                    }}

                    int tick = entityAtTick.Item2;
                    int oldTick = reader.ReadInt32();
                    int newTick = reader.ReadInt32();
                    if (tick != oldTick) {
                        throw new InvalidOperationException($""Entity is at tick {tick} but patch is from tick {oldTick} to {newTick}."");
                    }

                    byte dirtyFlags = reader.ReadByte();
                    byte createFlags = reader.ReadByte();
                    byte deleteFlags = reader.ReadByte();
";
            
            propertyIndex = 0;
            foreach (var IComponentType in GetIComponentTypes()) {
                var componentPropertyName = IComponentType.Name[1..].Decapitalize();
                entityDiffer += $@"
                    if ((dirtyFlags >> {propertyIndex} & 1) != 0) {{
                        var tuple = (entity.{componentPropertyName}, tick);
                        {componentPropertyName}Differ.Patch(ref tuple, reader);
                    }} else if ((createFlags >> {propertyIndex} & 1) != 0) {{
                        var tuple = (new {IComponentType.Name[1..]}(entity), tick);
                        {componentPropertyName}Differ.Patch(ref tuple, reader);
                    }} else if ((deleteFlags >> {propertyIndex} & 1) != 0) {{
                        entity.{componentPropertyName}.Dispose();
                    }}
";
                ++propertyIndex;
            }

            entityDiffer += @"
                    entityAtTick.Item2 = newTick;
                }
            }
        }
    }
}
";
            return entityDiffer;
        }
    }
}
