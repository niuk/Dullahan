using System;
using System.Collections.Generic;

namespace Dullahan.Generator {
    static class EntityGenerator {
        public static string GenerateEntity(string @namespace) {
            var entity = $@"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System;

namespace {@namespace} {{
    partial class World {{
        public sealed partial class Entity : IDisposable {{
            public readonly Guid id = Guid.NewGuid();

            public readonly World world;
            public readonly int constructionTick;
            public int disposalTick {{ get; private set; }}

            public Entity(World world) {{
                this.world = world;
                world.entitiesById.Add(id, this);
                constructionTick = world.tick;
                disposalTick = int.MaxValue;
            }}

            private void Dispose(bool disposing) {{
                if (disposalTick == int.MaxValue) {{
                    if (disposing) {{";

            foreach (var IComponentType in Generator.GetIComponentTypes()) {
                entity += $@"
                        {IComponentType.Name[1..].Decapitalize()}.Dispose();
";
            }

            entity += @"
                    }

                    disposalTick = world.tick;
                }
            }

            public void Dispose() {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
";

            foreach (var IComponentType in Generator.GetIComponentTypes()) {
                var componentTypeOrPropertyName = IComponentType.Name[1..];
                var componentFieldName = componentTypeOrPropertyName.Decapitalize();
                entity += $@"
            public {componentTypeOrPropertyName} {componentTypeOrPropertyName.Decapitalize()} {{ get; private set; }}
            public int {componentTypeOrPropertyName.Decapitalize()}_disposalTick {{ get; private set; }} = -1;
";
            }

            entity += @"
        }
    }
}";
            return entity;
        }

        public static string GenerateEntityDiffer(string @namespace, Dictionary<string, string> differTypeNameForDiffableTypeName) {
            // EntityDiffer.cs
            var entityDiffer = $@"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.IO;

namespace {@namespace} {{
    partial class World {{
        partial class Entity {{
            public sealed class Differ : IDiffer<(Entity, int)> {{";

            // EntityDiffer.Diff
            var entityDifferDiff = $@"
                public bool Diff((Entity, int) entityAtOldTick, (Entity, int) entityAtNewTick, BinaryWriter writer) {{
                    if (entityAtOldTick.Item1 != entityAtNewTick.Item1) {{
                        throw new InvalidOperationException(""Can only diff the same entity at different ticks."");
                    }}

                    var entity = entityAtOldTick.Item1;
                    int oldTick = entityAtOldTick.Item2;
                    int newTick = entityAtNewTick.Item2;

                    bool anyComponentsChanged = false;";

            foreach (var IComponentType in Generator.GetIComponentTypes()) {
                var componentTypeName = IComponentType.Name[1..];
                var componentPropertyName = componentTypeName.Decapitalize();

                differTypeNameForDiffableTypeName.Add(componentTypeName, $"{componentTypeName}Differ");

                Console.WriteLine($"Adding property \"{componentTypeName}\" to Entity class...");

                entityDiffer += $@"
                private readonly {componentTypeName}.Differ {componentPropertyName}Differ = new {componentTypeName}.Differ();";

                entityDifferDiff += $@"
                    anyComponentsChanged = {componentPropertyName}Differ.Diff(
                        (entity.{componentPropertyName}, oldTick),
                        (entity.{componentPropertyName}, newTick),
                        writer
                    ) || anyComponentsChanged;";
            }

            entityDifferDiff += $@"
                    return anyComponentsChanged;
                }}
";

            // EntityDiffer.Patch
            var entityDifferPatch = $@"
                public void Patch(ref (Entity, int) entityAtTick, BinaryReader reader) {{
";

            entityDifferPatch += @"
                }";

            entityDiffer += $@"
{entityDifferDiff}
{entityDifferPatch}
            }}
        }}
    }}
}}
";
            return entityDiffer;
        }
    }
}
