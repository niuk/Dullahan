using System;
using System.Collections.Generic;
using System.Linq;

namespace Dullahan.Generator {
    static class ComponentGenerator {
        private static IEnumerable<(Type, string)> GetComponentProperties(this Type IComponentType) {
            var getters = new HashSet<string>();
            var setters = new HashSet<string>();
            foreach (var methodInfo in IComponentType.GetMethods()) {
                Console.WriteLine($"Method: {methodInfo.Name}");
                if (methodInfo.Name.StartsWith("get_") && !getters.Contains(methodInfo.Name)) {
                    var propertyName = methodInfo.Name[4..];
                    getters.Add(propertyName);
                    if (setters.Contains(propertyName)) {
                        yield return (methodInfo.ReturnType, propertyName);
                    }
                }

                if (methodInfo.Name.StartsWith("set_") && !setters.Contains(methodInfo.Name)) {
                    var propertyName = methodInfo.Name[4..];
                    setters.Add(propertyName);
                    if (getters.Contains(propertyName)) {
                        yield return (methodInfo.GetParameters()[0].ParameterType, propertyName);
                    }
                }
            }
        }

        public static string GenerateComponent(string @namespace, Type IComponentType, Dictionary<string, string> differTypeNameForDiffableTypeName) {
            var componentTypeName = IComponentType.Name[1..];
            var component = $@"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace {@namespace} {{
    partial class World {{
        partial class Entity {{
            public sealed partial class {componentTypeName} : {IComponentType.FullName} {{
                public readonly Entity entity;
                public readonly int constructionTick;
                public int disposalTick {{ get; private set; }}

                public {componentTypeName}(Entity entity) {{
                    this.entity = entity;
                    constructionTick = entity.world.tick;
                    disposalTick = int.MaxValue;
                    entity.{componentTypeName.Decapitalize()} = this;
";

            // add attached entity to system's observer collection
            foreach (var systemType in Generator.GetSystemTypes()) {
                foreach (var (observerName, observedIComponentTypes) in systemType.GetObserverNameAndObservedIComponentTypes()) {
                    if (observedIComponentTypes.Contains(IComponentType)) {
                        var condition = string.Join(" && ", observedIComponentTypes.
                            Where(observedIComponentType => observedIComponentType != IComponentType).
                            Select(observedIComponentType => $"entity.{observedIComponentType.Name[1..].Decapitalize()} != null"));
                        if (!string.IsNullOrEmpty(condition)) {
                            component += $@"
                    if ({condition}) {{
                        (({systemType.Name}_Implementation)entity.world.{systemType.Name.Decapitalize()}).{observerName}_collection.Add(entity);
                    }}
";
                        } else {
                            component += $@"
                    (({systemType.Name}_Implementation)entity.world.{systemType.Name.Decapitalize()}).{observerName}_collection.Add(entity);
";
                        }
                    }
                }
            }

            component += @"
                }
";

            foreach (var (propertyType, propertyName) in IComponentType.GetComponentProperties()) {
                var propertyTypeName = propertyType.ToExpression();

                if (!differTypeNameForDiffableTypeName.TryGetValue(propertyTypeName, out string differTypeName)) {
                    throw new InvalidProgramException($"No implementation of {typeof(IDiffer<>).MakeGenericType(propertyType)} found!");
                }

                component += $@"
                private class Snapshot_{propertyName} {{
                    public static readonly ConcurrentBag<Snapshot_{propertyName}> pool = new ConcurrentBag<Snapshot_{propertyName}>();
                    public static readonly {differTypeName} differ = new {differTypeName}();

                    // diffTicks and diffEnds form an associative array
                    public readonly Ring<int> diffTicks = new Ring<int>();
                    public readonly Ring<int> diffOffsets = new Ring<int>();
                    public readonly BinaryWriter diffWriter = new BinaryWriter(new MemoryStream(), Encoding.UTF8, leaveOpen: true);
                    public {propertyTypeName} state;

                    public Snapshot_{propertyName}({propertyTypeName} state) {{
                        this.state = state;
                    }}
                }}

                // {propertyName}_ticks and {propertyName}_snapshots form an associative array
                private readonly Ring<int> {propertyName}_ticks = new Ring<int>();
                private readonly Ring<Snapshot_{propertyName}> {propertyName}_snapshots = new Ring<Snapshot_{propertyName}>();
                public {propertyTypeName} {propertyName} {{
                    get {{
                        return {propertyName}_snapshots.PeekEnd().state;
                    }}

                    set {{
                        int tick = entity.world.tick;
                        if ({propertyName}_snapshots.Count > 0 && {propertyName}_ticks.PeekEnd() == tick) {{
                            {propertyName}_ticks.PopEnd();
                            Snapshot_{propertyName}.pool.Add({propertyName}_snapshots.PopEnd());
                        }}

                        if (Snapshot_{propertyName}.pool.TryTake(out Snapshot_{propertyName} snapshot)) {{
                            snapshot.diffTicks.Clear();
                            snapshot.diffOffsets.Clear();
                            snapshot.diffWriter.SetOffset(0);
                        }} else {{
                            snapshot = new Snapshot_{propertyName}(value);
                        }}

                        int start = {propertyName}_ticks.Start + {propertyName}_ticks.Count - 1;
                        for (int i = start; i >= {propertyName}_ticks.Start; --i) {{
                            int savedOffset = snapshot.diffWriter.GetOffset();
                            if (!Snapshot_{propertyName}.differ.Diff({propertyName}_snapshots[i].state, snapshot.state, snapshot.diffWriter)) {{
                                if (i == start) {{
                                    // value didn't change
                                    Snapshot_{propertyName}.pool.Add(snapshot);
                                    return;
                                }}

                                snapshot.diffWriter.SetOffset(savedOffset);
                            }}

                            snapshot.diffTicks.PushEnd(tick - {propertyName}_ticks[i]);
                            snapshot.diffOffsets.PushEnd(snapshot.diffWriter.GetOffset());
                        }}

                        {propertyName}_ticks.PushEnd(tick);
                        {propertyName}_snapshots.PushEnd(snapshot);
                    }}
                }}
";
            }

            component += $@"
                private void Dispose(bool disposing) {{
                    if (entity.{componentTypeName.Decapitalize()}_disposalTick == int.MaxValue) {{
                        if (disposing) {{";

            // remove attached entity from relevant system's observed collection
            foreach (var systemType in Generator.GetSystemTypes()) {
                foreach (var (observerName, observedIComponentTypes) in systemType.GetObserverNameAndObservedIComponentTypes()) {
                    if (observedIComponentTypes.Contains(IComponentType)) {
                        component += $@"
                            (({systemType.Name}_Implementation)entity.world.{systemType.Name.Decapitalize()}).{observerName}_collection.Remove(entity);";
                    }
                }
            }

            foreach (var (propertyType, propertyName) in IComponentType.GetComponentProperties()) {
                if (propertyType.IsAssignableTo(typeof(IDisposable))) {
                    component += $@"
                            foreach (var state in {propertyName}_states) {{
                                state?.Dispose();
                            }}
";
                }
            }

            component += $@"
                        }}

                        entity.{componentTypeName.Decapitalize()} = null;
                        disposalTick = entity.world.tick;
                    }}
                }}

                public void Dispose() {{
                    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                    Dispose(disposing: true);
                    System.GC.SuppressFinalize(this);
                }}
";
            component += $@"
            }}
        }}
    }}
}}
";
            return component;
        }

        public static string GenerateComponentDiffer(string @namespace, Type IComponentType) {
            var componentTypeName = IComponentType.Name[1..];

            var componentDiffer = $@"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.IO;

namespace {@namespace} {{
    partial class World {{
        partial class Entity {{
            partial class {componentTypeName} {{
                public class Differ : IDiffer<({componentTypeName}, int)> {{
                    public bool Diff(({componentTypeName}, int) componentAtOldTick, ({componentTypeName}, int) componentAtNewTick, BinaryWriter writer) {{
                        if (componentAtOldTick.Item1 != componentAtNewTick.Item1) {{
                            throw new InvalidOperationException(""Can only diff the same component at different ticks."");
                        }}

                        var component = componentAtOldTick.Item1;
                        int oldTick = componentAtOldTick.Item2;
                        int newTick = componentAtNewTick.Item2;
                        writer.Write(oldTick);
                        writer.Write(newTick);

                        // reserve room
                        byte dirtyFlags = 0;
                        int dirtyFlagsOffset = writer.GetOffset();
                        writer.Write(dirtyFlags);
";

            int propertyIndex = 0;
            foreach (var (propertyType, propertyName) in GetComponentProperties(IComponentType)) {
                componentDiffer += $@"
                        /* {propertyName} */ {{
                            int snapshotIndex = component.{propertyName}_ticks.BinarySearch(newTick);
                            if (snapshotIndex < 0) {{
                                snapshotIndex = ~snapshotIndex - 1;
                            }}

                            if (snapshotIndex < 0) {{
                                throw new InvalidOperationException($""Tick {{newTick}} is too old to diff."");
                            }}

                            int snapshotTick = component.{propertyName}_ticks[snapshotIndex];
                            int diffTick = snapshotTick - oldTick;
                            if (diffTick > 0) {{
                                var snapshot = component.{propertyName}_snapshots[snapshotIndex];

                                int diffIndex = snapshot.diffTicks.BinarySearch(diffTick);
                                if (diffIndex < 0) {{
                                    diffIndex = ~diffIndex;
                                }}

                                if (diffIndex == snapshot.diffTicks.Count) {{
                                    throw new InvalidOperationException(""Tick {{oldTick}} is too old to diff."");
                                }}

                                int offset = diffIndex == 0 ? 0 : snapshot.diffOffsets[diffIndex - 1];
                                int size = snapshot.diffOffsets[diffIndex] - offset;
                                if (size > 0) {{
                                    writer.Write(((MemoryStream)snapshot.diffWriter.BaseStream).GetBuffer(), offset, size);
                                    dirtyFlags |= 1 << {propertyIndex++};
                                }}
                            }}
                        }}
";
            }

            componentDiffer += $@"
                        int savedOffset = writer.GetOffset();
                        writer.SetOffset(dirtyFlagsOffset);
                        writer.Write(dirtyFlags);
                        writer.SetOffset(savedOffset);

                        return dirtyFlags != 0;
                    }}

                    public void Patch(ref ({componentTypeName}, int) componentAtTick, BinaryReader reader) {{
                        var component = componentAtTick.Item1;
                        var tick = componentAtTick.Item2;
                        var oldTick = reader.ReadInt32();
                        var newTick = reader.ReadInt32();
                        if (tick != oldTick) {{
                            throw new InvalidOperationException($""Component is at tick {{tick}} but patch is from tick {{oldTick}} to {{newTick}}."");
                        }}

                        byte dirtyFlags = reader.ReadByte();
";

            propertyIndex = 0;
            foreach (var (propertyType, propertyName) in GetComponentProperties(IComponentType)) {
                componentDiffer += $@"
                        if ((dirtyFlags >> {propertyIndex++} & 1) != 0) {{
                            var value = component.{propertyName};
                            Snapshot_{propertyName}.differ.Patch(ref value, reader);
                            component.{propertyName} = value;
                        }}
";
            }

            componentDiffer += @"
                        componentAtTick.Item2 = newTick;
                    }
                }
            }
        }
    }
}
";
            return componentDiffer;
        }
    }
}
