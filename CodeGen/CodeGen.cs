using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Dullahan {
    public static class CodeGen {
        private static readonly Dictionary<string, string> differTypeNameForDiffableTypeName = new Dictionary<string, string>();
        private static readonly Dictionary<string, Assembly> assembliesByFullName = new Dictionary<string, Assembly>();
        private static string @namespace;
        private static string outputDirectory;

        public static void Main(string[] args) {
            if (args.Length == 0) {
                args = new string[] { "help" };
            }

            switch (args[0]) {
                case "generate":
                    LoadAssemblyWithReferencedAssemblies(Assembly.GetExecutingAssembly());

                    @namespace = args[1];
                    outputDirectory = args[2];
                    Console.WriteLine($"Namespace: {args[1]}\nOutput directory: {args[2]}");

                    for (int i = 3; i < args.Length; ++i) {
                        Console.WriteLine($"Source: {args[i]}");

                        foreach (var file in GetFilesRecursively(args[i], "*.dll")) {
                            Console.WriteLine($"Loading assembly from file: {file}");
                            LoadAssemblyWithReferencedAssemblies(Assembly.LoadFrom(file));
                        }

                        var sourceFiles = GetFilesRecursively(args[i], "*.cs");
                        foreach (var file in sourceFiles) {
                            Console.WriteLine($"Compiling file: {file}");
                        }

                        CompileFiles(sourceFiles);
                    }

                    GatherDiffers();

                    Generate();

                    break;
                case "clean":
                    Clean();

                    break;
                case "help":
                    ShowHelp();

                    break;
                default:
                    throw new ArgumentException($"Unrecognized command \"{args[0]}\": must be \"help\", \"generate\" or \"clean\"");
            }
        }

        private static void ShowHelp() {
            Console.Write($@"Usage:
    {AppDomain.CurrentDomain.FriendlyName} help
    {AppDomain.CurrentDomain.FriendlyName} generate <namespace> <output directory> <sources> <reference assemblies>
    {AppDomain.CurrentDomain.FriendlyName} clean <source project path>
");
        }

        private static IEnumerable<string> GetFilesRecursively(string directory, string pattern) {
            return Directory.GetFiles(directory, pattern).
                Concat(Directory.GetDirectories(directory).SelectMany(subDirectory => GetFilesRecursively(subDirectory, pattern)));
        }

        private static void LoadAssemblyWithReferencedAssemblies(Assembly assembly) {
            if (!assembliesByFullName.ContainsKey(assembly.FullName)) {
                assembliesByFullName.Add(assembly.FullName, assembly);
                foreach (var referencedAssemblyName in assembly.GetReferencedAssemblies()) {
                    try {
                        //Console.WriteLine($"Loading assembly by name: {referencedAssemblyName.FullName}");
                        LoadAssemblyWithReferencedAssemblies(Assembly.Load(referencedAssemblyName));
                    } catch (FileNotFoundException e) {
                        Console.Error.WriteLine(e.FileName);
                    }
                }
            }
        }

        private static void CompileFiles(IEnumerable<string> sourceFiles) {
            var syntaxTrees = sourceFiles.Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file)).WithFilePath(file));
            foreach (var syntaxTree in syntaxTrees) {
                Console.WriteLine($"{syntaxTree.FilePath}:\n{syntaxTree}\n");
            }

            var references = assembliesByFullName.Values.Where(assembly => !string.IsNullOrEmpty(assembly.Location)).Select(assembly => MetadataReference.CreateFromFile(assembly.Location));
            var compilation = CSharpCompilation.Create(Path.GetRandomFileName(), syntaxTrees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var memoryStream = new MemoryStream();
            var emitResult = compilation.Emit(memoryStream);
            if (emitResult.Success) {
                var assembly = Assembly.Load(memoryStream.ToArray());
                Console.WriteLine($"Loading compiled assembly: {assembly.FullName}");
                assembliesByFullName.Add(assembly.FullName, assembly);
            } else {
                throw new InvalidProgramException($"Compilation failed with diagnostics:\n{string.Join("\n", emitResult.Diagnostics)}");
            }
        }

        public static void Clean() {
            foreach (var file in Directory.GetFiles(outputDirectory)) {
                Console.WriteLine($"Deleting \"{file}\"...");
                File.Delete(file);
            }
        }

        private static IEnumerable<Type> GetIComponentTypes() {
            foreach (var type in GetAllTypes()) {
                if (type.IsAssignableTo(typeof(ECS.IComponent)) && type.IsInterface && type != typeof(ECS.IComponent)) {
                    if (type.Name.StartsWith("I") && type.Name.EndsWith("Component")) {
                        Console.WriteLine($"Found component interface {type.FullName}");
                        yield return type;
                    } else {
                        throw new Exception($"Invalid component interface name \"{type.Name}\": Must start with 'I' and end with \"Component\"!");
                    }
                }
            }
        }

        private static IEnumerable<Type> GetSystemTypes() {
            foreach (var type in GetAllTypes()) {
                if (typeof(ECS.ISystem).IsAssignableFrom(type) && type.IsAbstract && type != typeof(ECS.ISystem)) {
                    if (type.Name.EndsWith("System")) {
                        Console.WriteLine($"Found {typeof(ECS.ISystem)} implementation {type}");
                        yield return type;
                    } else {
                        throw new Exception($"Invalid abstract system type name \"{type.Name}\": Must end with \"System\"!");
                    }
                }
            }
        }

        private static void GenerateWorld() {
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

            var systemTypes = GetSystemTypes();
            var dependenciesBySystemType = new Dictionary<Type, HashSet<Type>>();
            foreach (var systemType in systemTypes) {
                Console.WriteLine($"Adding property \"{systemType.Name}\" to World class...");
                world += $@"
        public readonly {systemType.FullName} {Decapitalize(systemType.Name)} = new {systemType.Name}_Implementation();
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
            {Decapitalize(untickedSystemType.Name)}.Tick();
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
            File.WriteAllText(Path.Combine(outputDirectory, "World.cs"), world);
        }

        private static void GenerateWorldDiffer() {
            File.WriteAllText(Path.Combine(outputDirectory, "WorldDiffer.cs"), $@"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
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

                writer.Write(newTick - oldTick);

                // reserve room for count of changed entities
                int startOffset = writer.GetOffset();
                writer.Write(0);
                int changedCount = 0;
                var entitiesById = world != null ? world.entitiesById : new Dictionary<Guid, Entity>();
                var disposed = new HashSet<Guid>();
                var constructed = new HashSet<Entity>();
                foreach (var entity in entitiesById.Values) {{
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
                    entityDiffer.Diff((null, -1), (entity, newTick), writer);
                }}

                return changedCount > 0 || disposed.Count > 0 || constructed.Count > 0;
            }}

            public void Patch(ref (World, int) worldAtTick, BinaryReader reader) {{
                var world = worldAtTick.Item1;
                var tick = worldAtTick.Item2;

                int deltaTick = reader.ReadInt32();
                world.AddTick(world.tick + deltaTick);

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
                    var id = new Guid(reader.ReadBytes(16));
                    var entityAtTick = (default(Entity), tick);
                    entityDiffer.Patch(ref entityAtTick, reader);
                    world.entitiesById.Add(id, entityAtTick.Item1);
                }}
            }}
        }}
    }}
}}
");
        }

        private static void GenerateEntity() {
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

            foreach (var IComponentType in GetIComponentTypes()) {
                entity += $@"
                        {Decapitalize(IComponentType.Name[1..])}.Dispose();
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

            foreach (var IComponentType in GetIComponentTypes()) {
                var componentTypeOrPropertyName = IComponentType.Name[1..];
                var componentFieldName = Decapitalize(componentTypeOrPropertyName);
                entity += $@"
            public {componentTypeOrPropertyName} {Decapitalize(componentTypeOrPropertyName)} {{ get; private set; }}
            public int {Decapitalize(componentTypeOrPropertyName)}_disposalTick {{ get; private set; }} = -1;
";
            }

            entity += @"
        }
    }
}";
            File.WriteAllText(Path.Combine(outputDirectory, "Entity.cs"), entity);
        }

        private static void GenerateEntityDiffer() {
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

            foreach (var IComponentType in GetIComponentTypes()) {
                var componentTypeName = IComponentType.Name[1..];
                var componentPropertyName = Decapitalize(componentTypeName);

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
            File.WriteAllText(Path.Combine(outputDirectory, "EntityDiffer.cs"), entityDiffer);
        }

        private static IEnumerable<(Type, string)> GetComponentProperties(Type IComponentType) {
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

        private static void GenerateComponent(Type IComponentType) {
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
                    entity.{Decapitalize(componentTypeName)} = this;
                    entity.{Decapitalize(componentTypeName)}_disposalTick = int.MaxValue;
";

            // add attached entity to system's observer collection
            foreach (var systemType in GetSystemTypes()) {
                foreach (var (observerName, observedIComponentTypes) in GetObserverNameAndObservedIComponentTypes(systemType)) {
                    if (observedIComponentTypes.Contains(IComponentType)) {
                        var condition = string.Join(" && ", observedIComponentTypes.
                            Where(observedIComponentType => observedIComponentType != IComponentType).
                            Select(observedIComponentType => $"entity.{Decapitalize(observedIComponentType.Name[1..])} != null"));
                        if (!string.IsNullOrEmpty(condition)) {
                            constructor += $@"
                    if ({condition}) {{
                        (({systemType.Name}_Implementation)entity.world.{Decapitalize(systemType.Name)}).{observerName}_collection.Add(entity);
                    }}
";
                        } else {
                            constructor += $@"
                    (({systemType.Name}_Implementation)entity.world.{Decapitalize(systemType.Name)}).{observerName}_collection.Add(entity);
";
                        }
                    }
                }
            }

            var disposer = $@"
                private void Dispose(bool disposing) {{
                    if (entity.{Decapitalize(componentTypeName)}_disposalTick == int.MaxValue) {{
                        if (disposing) {{";

            foreach (var (propertyType, propertyName) in GetComponentProperties(IComponentType)) {
                Console.WriteLine($"Property: {propertyName}");

                var propertyTypeName = ToExpression(propertyType);

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

                        if (!Snapshot_{propertyName}.pool.TryTake(out Snapshot_{propertyName} snapshot)) {{
                            snapshot = new Snapshot_{propertyName}(value);
                            snapshot.diffs.Clear();
                            snapshot.diffWriter.SetOffset(0);
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
            foreach (var systemType in GetSystemTypes()) {
                foreach (var (observerName, observedIComponentTypes) in GetObserverNameAndObservedIComponentTypes(systemType)) {
                    if (observedIComponentTypes.Contains(IComponentType)) {
                        disposer += $@"
                            (({systemType.Name}_Implementation)entity.world.{Decapitalize(systemType.Name)}).{observerName}_collection.Remove(entity);";
                    }
                }
            }

            disposer += $@"
                        }}

                        entity.{Decapitalize(componentTypeName)} = null;
                        entity.{Decapitalize(componentTypeName)}_disposalTick = entity.world.tick;
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
            File.WriteAllText(Path.Combine(outputDirectory, componentTypeName + ".cs"), component);
        }

        private static void GenerateComponentDiffer(Type IComponentType) {
            var componentTypeName = IComponentType.Name[1..];

            // *ComponentDiffer.cs
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

                    public void Patch(ref ({componentTypeName}, int) component, BinaryReader reader) {{
                    }}";

            componentDiffer += @"
                }
            }
        }
    }
}
";
            File.WriteAllText(Path.Combine(outputDirectory, $"{componentTypeName}Differ.cs"), componentDiffer);
        }

        private static IEnumerable<(string, IEnumerable<Type>)> GetObserverNameAndObservedIComponentTypes(Type systemType) {
            foreach (var method in systemType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)) {
                if (method.Name.StartsWith("get_") && method.IsAbstract) {
                    var propertyName = method.Name[4..];
                    if (!method.ReturnType.IsGenericType || method.ReturnType.GetGenericTypeDefinition() != typeof(IEnumerable<>)) {
                        throw new InvalidProgramException($"Property \"{propertyName}\" in system {systemType} must be {typeof(IEnumerable<>)} type!");
                    }

                    var badPropertyMessage = $"Property \"{propertyName}\" must enumerate components or tuples of components!";
                    var elementType = method.ReturnType.GetGenericArguments()[0];
                    if (elementType.IsGenericType) {
                        if (elementType.GetGenericTypeDefinition().Name.Split('`')[0] != "ValueTuple") {
                            throw new InvalidProgramException(badPropertyMessage);
                        }

                        var arguments = elementType.GetGenericArguments();
                        if (arguments.Any(itemType => !itemType.IsAssignableTo(typeof(ECS.IComponent)))) {
                            throw new InvalidProgramException(badPropertyMessage);
                        }

                        yield return (propertyName, arguments);
                    } else {
                        if (!elementType.IsAssignableTo(typeof(ECS.IComponent))) {
                            throw new InvalidProgramException(badPropertyMessage);
                        }

                        yield return (propertyName, Enumerable.Repeat(elementType, 1));
                    }
                }
            }
        }

        private static void GenerateSystemImplementation(Type systemType) {
            Console.WriteLine($"Generating system implementation class \"{systemType.Name}_Implementation\" from \"{systemType.FullName}\"...");

            var SystemImplementation_cs = $@"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System.Collections.Generic;
using System.Linq;

namespace {@namespace} {{
    partial class World {{
        public sealed class {systemType.Name}_Implementation : {systemType.FullName} {{
";

            foreach (var (observerName, observedIComponentTypes) in GetObserverNameAndObservedIComponentTypes(systemType)) {
                var isTuple = observedIComponentTypes.Count() > 1;
                var tuplePrefix = isTuple ? "(" : "";
                var tupleSuffix = isTuple ? ")" : "";
                var observedIComponentTypeNames = string.Join(", ", observedIComponentTypes.Select(t => t.FullName));
                var observedIComponentPropertyNames = string.Join(", ", observedIComponentTypes.Select(t => $"({t.FullName})entity.{Decapitalize(t.Name[1..])}"));
                SystemImplementation_cs += $@"
            public readonly HashSet<Entity> {observerName}_collection = new HashSet<Entity>();
            protected override IEnumerable<{tuplePrefix}{observedIComponentTypeNames}{tupleSuffix}> {
                    observerName} => {observerName}_collection.Select(entity => {tuplePrefix}{observedIComponentPropertyNames}{tupleSuffix});
";
            }

            SystemImplementation_cs += @"
        }
    }
}
";
            File.WriteAllText(Path.Combine(outputDirectory, $"{systemType.Name}_Implementation.cs"), SystemImplementation_cs);
        }

        public static void Generate() {
            GenerateWorld();

            GenerateWorldDiffer();

            GenerateEntity();

            GenerateEntityDiffer();

            foreach (var systemType in GetSystemTypes()) {
                GenerateSystemImplementation(systemType);
            }

            foreach (var IComponentType in GetIComponentTypes()) {
                GenerateComponent(IComponentType);

                GenerateComponentDiffer(IComponentType);
            }
        }

        private static void GatherDiffers() {
            Console.WriteLine($"Gathering {typeof(IDiffer<>)} implementations...");

            foreach (var type in GetAllTypes()) {
                var interfaces = type.GetInterfaces();
                foreach (var @interface in interfaces) {
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IDiffer<>)) {
                        Console.WriteLine($"Found {@interface} implementation {type}.");
                        differTypeNameForDiffableTypeName[ToExpression(@interface.GetGenericArguments()[0])] = ToExpression(type);
                    }
                }
            }
        }

        private static IEnumerable<Type> GetAllTypes() {
            return assembliesByFullName.Values.SelectMany(assembly => {
                try {
                    return assembly.GetTypes();
                } catch (Exception e) {
                    Console.Error.WriteLine(e);
                    return Array.Empty<Type>();
                }
            });

        }

        private static string ToExpression(Type type) {
            if (type.IsGenericType) {
                return $"{type.GetGenericTypeDefinition().FullName.Split('`')[0]}<{string.Join(", ", type.GetGenericArguments().Select(ToExpression))}>";
            } else {
                return type.FullName;
            }
        }

        private static string Decapitalize(string s) {
            return s[0].ToString().ToLower() + s[1..];
        }
    }
}
