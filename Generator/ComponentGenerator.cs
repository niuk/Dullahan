using System;
using System.Collections.Generic;
using System.Linq;

using static Dullahan.Generator.Generator;

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
                    constructionTick = entity.world.nextTick;
                    disposalTick = int.MaxValue;
                    entity.{componentTypeName.Decapitalize()} = this;
";

            // add attached entity to system's observer collection
            foreach (var systemType in GetSystemTypes()) {
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
                    public readonly Ring<(int, int)> diffSpans = new Ring<(int, int)>();
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
                        if ({propertyName}_ticks.Count == 0) {{
                            return default;
                        }}

                        int index = {propertyName}_ticks.BinarySearch(entity.world.previousTick);
                        if (index < 0) {{
                            return {propertyName}_snapshots[~index - 1].state;
                        }} else {{
                            return {propertyName}_snapshots[index].state;
                        }}
                    }}

                    set {{
                        Snapshot_{propertyName} snapshot;

                        int tick = entity.world.nextTick;
                        int index = {propertyName}_ticks.BinarySearch(tick);
                        if (index < 0) {{
                            if (!Snapshot_{propertyName}.pool.TryTake(out snapshot)) {{
                                snapshot = new Snapshot_{propertyName}(value);
                            }}
                        }} else {{
                            snapshot = {propertyName}_snapshots[index];
                            {propertyName}_snapshots.RemoveAt(index);
                        }}

                        snapshot.diffTicks.Clear();
                        snapshot.diffSpans.Clear();
                        snapshot.diffWriter.SetOffset(0);

                        if (index < 0) {{
                            index = ~index;
                        }}

                        // iterate backwards because we might terminate on finding no diffs w.r.t. the immediately preceding tick
                        int savedOffset;
                        int start = index - 1;
                        for (int i = start; i >= {propertyName}_ticks.Start; --i) {{
                            savedOffset = snapshot.diffWriter.GetOffset();
                            if (!Snapshot_{propertyName}.differ.Diff({propertyName}_snapshots[i].state, snapshot.state, snapshot.diffWriter)) {{
                                if (i == start) {{
                                    // value didn't change
                                    Snapshot_{propertyName}.pool.Add(snapshot);
                                    return;
                                }}

                                snapshot.diffWriter.SetOffset(savedOffset);
                            }}

                            snapshot.diffTicks.PushEnd(tick - {propertyName}_ticks[i]);
                            snapshot.diffSpans.PushEnd((savedOffset, snapshot.diffWriter.GetOffset() - savedOffset));
                        }}

                        // diff with tick 0
                        savedOffset = snapshot.diffWriter.GetOffset();
                        if (!Snapshot_{propertyName}.differ.Diff(default, snapshot.state, snapshot.diffWriter)) {{
                            snapshot.diffWriter.SetOffset(savedOffset);
                        }}

                        snapshot.diffTicks.PushEnd(tick);
                        snapshot.diffSpans.PushEnd((savedOffset, snapshot.diffWriter.GetOffset() - savedOffset));

                        {propertyName}_ticks.Insert(index, tick);
                        {propertyName}_snapshots.Insert(index, snapshot);

                        // now that we have a new or modified snapshot, later snapshots need to diff with it
                        for (int i = index + 1; i < {propertyName}_ticks.End; ++i) {{
                            savedOffset = {propertyName}_snapshots[i].diffWriter.GetOffset();
                            if (!Snapshot_{propertyName}.differ.Diff(snapshot.state, {propertyName}_snapshots[i].state, {propertyName}_snapshots[i].diffWriter)) {{
                                {propertyName}_snapshots[i].diffWriter.SetOffset(savedOffset);
                            }}

                            int diffTick = {propertyName}_ticks[i] - tick;
                            var diffSpan = (savedOffset, {propertyName}_snapshots[i].diffWriter.GetOffset() - savedOffset);
                            int diffIndex = {propertyName}_snapshots[i].diffTicks.BinarySearch(diffTick);
                            if (diffIndex < 0) {{
                                {propertyName}_snapshots[i].diffTicks.Insert(~diffIndex, diffTick);
                                {propertyName}_snapshots[i].diffSpans.Insert(~diffIndex, diffSpan);
                            }} else {{
                                {propertyName}_snapshots[i].diffSpans[diffIndex] = diffSpan;
                            }}
                        }}
                    }}
                }}
";
            }

            component += $@"
                private void Dispose(bool disposing) {{
                    if (disposalTick == int.MaxValue) {{
                        if (disposing) {{";

            // remove attached entity from relevant system's observed collection
            foreach (var systemType in GetSystemTypes()) {
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
                        disposalTick = entity.world.nextTick;
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
                        var component = componentAtOldTick.Item1;
                        if (component == null) {{
                            component = componentAtNewTick.Item1;
                            if (component == null) {{
                                throw new InvalidOperationException(""Cannot diff two null components."");
                            }}
                        }} else {{
                            if (component != componentAtNewTick.Item1) {{
                                throw new InvalidOperationException(""Can only diff the same component at different ticks."");
                            }}
                        }}

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

                                var (offset, size) = snapshot.diffSpans[diffIndex];
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
