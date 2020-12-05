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
";

            var constructor = $@"
                public {componentTypeName}(Entity entity) {{
                    this.entity = entity;
                    constructionTick = entity.world.tick;
                    entity.{componentTypeName.Decapitalize()} = this;
                    entity.{componentTypeName.Decapitalize()}_disposalTick = int.MaxValue;
";

            // add attached entity to system's observer collection
            foreach (var systemType in Generator.GetSystemTypes()) {
                foreach (var (observerName, observedIComponentTypes) in systemType.GetObserverNameAndObservedIComponentTypes()) {
                    if (observedIComponentTypes.Contains(IComponentType)) {
                        var condition = string.Join(" && ", observedIComponentTypes.
                            Where(observedIComponentType => observedIComponentType != IComponentType).
                            Select(observedIComponentType => $"entity.{observedIComponentType.Name[1..].Decapitalize()} != null"));
                        if (!string.IsNullOrEmpty(condition)) {
                            constructor += $@"
                    if ({condition}) {{
                        (({systemType.Name}_Implementation)entity.world.{systemType.Name.Decapitalize()}).{observerName}_collection.Add(entity);
                    }}
";
                        } else {
                            constructor += $@"
                    (({systemType.Name}_Implementation)entity.world.{systemType.Name.Decapitalize()}).{observerName}_collection.Add(entity);
";
                        }
                    }
                }
            }

            var disposer = $@"
                private void Dispose(bool disposing) {{
                    if (entity.{componentTypeName.Decapitalize()}_disposalTick == int.MaxValue) {{
                        if (disposing) {{";

            foreach (var (propertyType, propertyName) in IComponentType.GetComponentProperties()) {
                Console.WriteLine($"Property: {propertyName}");

                var propertyTypeName = propertyType.ToExpression();

                if (!differTypeNameForDiffableTypeName.TryGetValue(propertyTypeName, out string differTypeName)) {
                    throw new InvalidProgramException($"No implementation of {typeof(IDiffer<>).MakeGenericType(propertyType)} found!");
                }

                if (propertyType.IsAssignableTo(typeof(IDisposable))) {
                    disposer += $@"
                            foreach (var state in {propertyName}_states) {{
                                state.Dispose();
                            }}
";
                }

                component += $@"
                private class Snapshot_{propertyName} {{
                    public static readonly ConcurrentBag<Snapshot_{propertyName}> pool = new ConcurrentBag<Snapshot_{propertyName}>();
                    public static readonly {differTypeName} differ = new {differTypeName}();

                    public readonly Ring<(int, int)> diffs = new Ring<(int, int)>();
                    public readonly BinaryWriter diffWriter = new BinaryWriter(new MemoryStream(), Encoding.UTF8, leaveOpen: true);
                    public {propertyTypeName} state;

                    public Snapshot_{propertyName}({propertyTypeName} state) {{
                        this.state = state;
                    }}
                }}

                private readonly Ring<int> {propertyName}_ticks = new Ring<int>();
                private readonly Ring<Snapshot_{propertyName}> {propertyName}_snapshots = new Ring<Snapshot_{propertyName}>();
                public {propertyTypeName} {propertyName} {{
                    get {{
                        return {propertyName}_snapshots.PeekEnd().state;
                    }}

                    set {{
                        if ({propertyName}_snapshots.Count > 0 && {propertyName}_ticks.PeekEnd() == entity.world.tick) {{
                            {propertyName}_ticks.PopEnd();
                            Snapshot_{propertyName}.pool.Add({propertyName}_snapshots.PopEnd());
                        }}

                        if (Snapshot_{propertyName}.pool.TryTake(out Snapshot_{propertyName} snapshot)) {{
                            snapshot.diffs.Clear();
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

                            snapshot.diffs.PushEnd((savedOffset, snapshot.diffWriter.GetOffset() - savedOffset));
                        }}

                        {propertyName}_ticks.PushEnd(entity.world.tick);
                        {propertyName}_snapshots.PushEnd(snapshot);
                    }}
                }}
";
            }

            constructor += @"
                }
";

            // remove attached entity from relevant system's observed collection
            foreach (var systemType in Generator.GetSystemTypes()) {
                foreach (var (observerName, observedIComponentTypes) in systemType.GetObserverNameAndObservedIComponentTypes()) {
                    if (observedIComponentTypes.Contains(IComponentType)) {
                        disposer += $@"
                            (({systemType.Name}_Implementation)entity.world.{systemType.Name.Decapitalize()}).{observerName}_collection.Remove(entity);";
                    }
                }
            }

            disposer += $@"
                        }}

                        entity.{componentTypeName.Decapitalize()} = null;
                        entity.{componentTypeName.Decapitalize()}_disposalTick = entity.world.tick;
                    }}
                }}

                public void Dispose() {{
                    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                    Dispose(disposing: true);
                    System.GC.SuppressFinalize(this);
                }}
";
            component += $@"
{constructor}
{disposer}
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
                        bool changed = false;

                        // reserve room
                        byte flag = 0;
                        int flagOffset = writer.GetOffset();
                        writer.Write(flag);
";

            int propertyIndex = 0;
            foreach (var (propertyType, propertyName) in GetComponentProperties(IComponentType)) {
                componentDiffer += $@"
                        /* {propertyName} */ {{
                            int oldIndex = component.{propertyName}_ticks.BinarySearch(oldTick);
                            if (oldIndex < 0) {{
                                oldIndex = ~oldIndex - 1;
                            }}

                            int newIndex = component.{propertyName}_ticks.BinarySearch(newTick);
                            if (newIndex < 0) {{
                                newIndex = ~newIndex - 1;
                            }}

                            var snapshot = component.{propertyName}_snapshots[newIndex];
                            (int index, int count) = snapshot.diffs[newIndex - oldIndex];
                            if (count > 0) {{
                                changed = true;
                                writer.Write(((MemoryStream)snapshot.diffWriter.BaseStream).GetBuffer(), index, count);
                                flag |= 1 << {propertyIndex++};
                            }}
                        }}
";
            }

            componentDiffer += $@"
                        int savedOffset = writer.GetOffset();
                        writer.SetOffset(flagOffset);
                        writer.Write(flag);
                        writer.SetOffset(savedOffset);

                        return changed;
                    }}

                    public void Patch(ref ({componentTypeName}, int) componentAtTick, BinaryReader reader) {{
                        var component = componentAtTick.Item1;
                        byte flag = reader.ReadByte();
";

            int componentIndex = 0;
            foreach (var (propertyType, propertyName) in GetComponentProperties(IComponentType)) {
                componentDiffer += $@"
                        if ((flag >> {componentIndex++} & 1) != 0) {{
                            var value = component.{propertyName};
                            Snapshot_{propertyName}.differ.Patch(ref value, reader);
                            component.{propertyName} = value;
                        }}
";
            }

            componentDiffer += @"
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
