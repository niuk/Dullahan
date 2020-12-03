using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Dullahan {
    public class CodeGen {
        private static readonly Dictionary<string, string> differTypeNameForDiffableTypeName = new Dictionary<string, string>();
        private static readonly Dictionary<Type, string> componentTypeNameForIComponentType = new Dictionary<Type, string>();
        private static readonly Dictionary<string, Assembly> assembliesByFullName = new Dictionary<string, Assembly>();

        public static void Main(string[] args) {
            if (args.Length == 0) {
                args = new string[] { "help" };
            }

            switch (args[0]) {
                case "generate":
                    LoadAssemblyWithReferencedAssemblies(Assembly.GetExecutingAssembly());

                    for (int i = 3; i < args.Length; ++i) {
                        Console.WriteLine($"Source: {args[i]}");
                        LoadAssembliesFromDirectory(args[i]);
                        CompileFilesFromDirectory(args[i]);
                    }

                    Console.WriteLine($"Namespace: {args[1]}\nOutput directory: {args[2]}");
                    Generate(args[1], args[2]);

                    break;
                case "clean":
                    Clean(args[1]);
                    break;
                case "help":
                    Console.Write($@"Usage:
    {AppDomain.CurrentDomain.FriendlyName} help
    {AppDomain.CurrentDomain.FriendlyName} generate <namespace> <output directory> <sources> <reference assemblies>
    {AppDomain.CurrentDomain.FriendlyName} clean <source project path>
");
                    break;
                default:
                    throw new ArgumentException($"Unrecognized command \"{args[0]}\": must be \"help\", \"generate\" or \"clean\"");
            }
        }

        private static IEnumerable<string> GetFilesRecursive(string directory, string pattern) {
            return Directory.GetFiles(directory, pattern).Concat(Directory.GetDirectories(directory).SelectMany(subDirectory => GetFilesRecursive(subDirectory, pattern)));
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

        private static void LoadAssembliesFromDirectory(string directory) {
            foreach (var file in GetFilesRecursive(directory, "*.dll")) {
                Console.WriteLine($"Loading assembly from file: {file}");
                LoadAssemblyWithReferencedAssemblies(Assembly.LoadFrom(file));
            }
        }

        private static void CompileFilesFromDirectory(string directory) {
            var syntaxTrees = GetFilesRecursive(directory, "*.cs").Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file)).WithFilePath(file));
            foreach (var syntaxTree in syntaxTrees) {
                Console.WriteLine($"{syntaxTree.FilePath}:\n{syntaxTree}\n");
            }

            var references = assembliesByFullName.Values.Where(assembly => !string.IsNullOrEmpty(assembly.Location)).Select(assembly => MetadataReference.CreateFromFile(assembly.Location));
            var compilation = CSharpCompilation.Create(Path.GetRandomFileName(), syntaxTrees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using (var memoryStream = new MemoryStream()) {
                var emitResult = compilation.Emit(memoryStream);
                if (emitResult.Success) {
                    var assembly = Assembly.Load(memoryStream.ToArray());
                    Console.WriteLine($"Loading compiled assembly: {assembly.FullName}");
                    assembliesByFullName.Add(assembly.FullName, assembly);
                } else {
                    foreach (var diagnostic in emitResult.Diagnostics) {
                        Console.Error.WriteLine(diagnostic);
                    }

                    throw new InvalidProgramException($"Could not compile sources in directory \"{directory}\"");
                }
            }
        }

        public static void Clean(string outputDirectory) {
            foreach (var file in Directory.GetFiles(outputDirectory)) {
                Console.WriteLine($"Deleting \"{file}\"...");
                File.Delete(file);
            }
        }

        public static void Generate(string @namespace, string outputPath) {
            // differ types need to be known for the generation of component property accessors below
            GatherDiffers();

            // World.cs
            var worldCode = $@"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace {@namespace} {{
    public class World : IReadOnlyDictionary<int, (World, int)> {{
        public int tick => ticks.Max;
        private readonly SortedSet<int> ticks = new SortedSet<int>();

        public void AddTick(int tick) {{
            ticks.Add(tick);
        }}

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

        public readonly Dictionary<Guid, Entity> entitiesById = new Dictionary<Guid, Entity>();
";

            // WorldDiffer.cs
            var worldDifferCode = $@"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace {@namespace} {{
    public class WorldDiffer : IDiffer<(World, int)> {{
        private readonly EntityDiffer entityDiffer = new EntityDiffer();

        public bool Diff((World, int) worldAtOldTick, (World, int) worldAtNewTick, BinaryWriter writer) {{
            var oldWorld = worldAtOldTick.Item1;
            var newWorld = worldAtNewTick.Item1;
            if (oldWorld != null && oldWorld != newWorld) {{
                throw new InvalidOperationException(""Can only diff the same world at different ticks."");
            }}

            int oldTick = worldAtOldTick.Item2;
            int newTick = worldAtNewTick.Item2;

            writer.Write(newTick - oldTick);

            // reserve room for count of changed entities
            int startPosition = writer.GetPosition();
            writer.Write(0);
            int changedCount = 0;
            var entitiesById = oldWorld != null ? oldWorld.entitiesById : new Dictionary<Guid, Entity>();
            var disposed = new HashSet<Guid>();
            var constructed = new HashSet<Entity>();
            foreach (var entity in entitiesById.Values) {{
                if (entity.constructionTick <= oldTick && oldTick < entity.disposalTick) {{
                    // entity exists in old world
                    if (entity.constructionTick <= newTick && newTick < entity.disposalTick) {{
                        // entity also exists in new world
                        int keyPosition = writer.GetPosition(); // preemptively write the key; erase when entities don't differ
                        writer.Write(entity.id.ToByteArray());
                        if (entityDiffer.Diff((entity, oldTick), (entity, newTick), writer)) {{
                            ++changedCount;
                        }} else {{
                            writer.SetPosition(keyPosition);
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

            int savedPosition = writer.GetPosition();
            writer.SetPosition(startPosition);
            writer.Write(changedCount);
            writer.SetPosition(savedPosition);

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
";

            // Entity.cs
            var entityCode = $@"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System;
using System.IO;
using System.Text;
using Dullahan;

namespace {@namespace} {{
    public class Entity {{
        public readonly Guid id = Guid.NewGuid();

        public readonly World world;
        public readonly int constructionTick;
        public int disposalTick {{ get; private set; }}

        private Entity entity => this;
";

            var entityConstructorCode = $@"
        public Entity(World world) {{
            this.world = world;
            constructionTick = world.tick;
            disposalTick = int.MaxValue;
            world.entitiesById.Add(id, this);
";

            // EntityDiffer.cs
            var entityDifferCode = $@"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.IO;

namespace {@namespace} {{
    public class EntityDiffer : IDiffer<(Entity, int)> {{
";

            // EntityDiffer.Diff
            var entityDiffCode = $@"
        public bool Diff((Entity, int) entityAtOldTick, (Entity, int) entityAtNewTick, BinaryWriter writer) {{
            var oldEntity = entityAtOldTick.Item1;
            var newEntity = entityAtNewTick.Item1;

            if (oldEntity != newEntity) {{
                throw new InvalidOperationException(""Can only diff the same entity at different ticks."");
            }}

            int oldTick = entityAtOldTick.Item2;
            int newTick = entityAtNewTick.Item2;
";

            // EntityDiffer.Patch
            var entityPatchCode = $@"
        public void Patch(ref (Entity, int) entityAtTick, BinaryReader reader) {{
";

            // generate system classes first so that we know which systems need to add/remove component tuples when components are added to/removed from entities
            var systemModificationsForComponentType = new Dictionary<Type, HashSet<string>>();
            // keep track of systems and their dependencies so that we can tick them in the correct order in World.Tick
            var systemTypes = new HashSet<Type>();
            var dependenciesBySystemType = new Dictionary<Type, HashSet<Type>>(); // no actual concurrency; just want AddOrUpdate
            foreach (var type in GetAllTypes()) {
                if (typeof(ECS.ISystem).IsAssignableFrom(type) && type.IsAbstract && type != typeof(ECS.ISystem)) {
                    if (!type.Name.EndsWith("System")) {
                        throw new Exception($"Invalid abstract system name {type.Name}: Must end with \"System\"!");
                    }

                    Console.WriteLine($"Found {typeof(ECS.ISystem)} implementation {type}");

                    systemTypes.Add(type);
                    var newDependencies = type.GetCustomAttributes<ECS.TickAfter>().Select(attr => attr.systemType);
                    if (dependenciesBySystemType.TryGetValue(type, out HashSet<Type> oldDependencies)) {
                        oldDependencies.UnionWith(newDependencies);
                    } else {
                        dependenciesBySystemType.Add(type, new HashSet<Type>(newDependencies));
                    }

                    foreach (var dependant in type.GetCustomAttributes<ECS.TickBefore>().Select(attr => attr.systemType)) {
                        if (dependenciesBySystemType.TryGetValue(dependant, out oldDependencies)) {
                            oldDependencies.UnionWith(new[] { type });
                        } else {
                            dependenciesBySystemType.Add(dependant, new HashSet<Type>(new[] { type }));
                        }
                    }

                    var systemTypeName = type.Name + "_Implementation";
                    var systemPropertyName = Decapitalize(type.Name);

                    Console.WriteLine($"Adding property \"{systemPropertyName}\" to World class...");
                    worldCode += $@"
        public readonly {type.FullName} {systemPropertyName} = new {systemTypeName}();
";

                    Console.WriteLine($"Generating system class \"{systemTypeName}\" from \"{type.FullName}\"...");
                    var systemCode = $@"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System;
using System.Collections.Generic;
using System.Linq;

namespace {@namespace} {{
    public class {systemTypeName} : {type.FullName} {{
";

                    foreach (var method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)) {
                        if (method.Name.StartsWith("get_") && method.IsAbstract) {
                            var propertyName = method.Name.Substring(4);
                            var enumerableType = method.ReturnType;
                            if (enumerableType.IsGenericType && enumerableType.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
                                Console.WriteLine($"Generating accessor {propertyName} with type {enumerableType}...");
                                var genericArgument = enumerableType.GetGenericArguments()[0];
                                if (genericArgument.IsGenericType && genericArgument.GetGenericTypeDefinition().Name.Split('`')[0] == "Tuple") {
                                    // store the entities directly into the collection
                                    var componentTypes = genericArgument.GetGenericArguments();
                                    systemCode += $@"
        public readonly HashSet<Entity> {propertyName}_collection = new HashSet<Entity>();
        protected override IEnumerable<Tuple<{
                                        string.Join(", ", componentTypes.Select(componentType => componentType.FullName))
                                        }>> {propertyName} => {propertyName}_collection.Select(entity => Tuple.Create({
                                        string.Join(", ", componentTypes.Select(componentType => "entity." + Decapitalize(componentType.Name.Substring(1))))
                                        }));
";

                                    foreach (var componentType in componentTypes) {
                                        var modification = $@"
                if (value != null) {{
                    if ({string.Join(" && ", componentTypes.Where(ct => ct != componentType).Select(ct => $"entity.{Decapitalize(ct.Name.Substring(1))} != null"))}) {{
                        (({systemTypeName})entity.world.{systemPropertyName}).{propertyName}_collection.Add(entity);
                    }}
                }} else {{
                    (({systemTypeName})entity.world.{systemPropertyName}).{propertyName}_collection.Remove(entity);
                }}
";
                                        if (systemModificationsForComponentType.TryGetValue(componentType, out HashSet<string> modifications)) {
                                            modifications.Add(modification);
                                        } else {
                                            systemModificationsForComponentType.Add(componentType, new HashSet<string> { modification });
                                        }
                                    }
                                } else {
                                    // store just the components
                                    var componentTypeName = genericArgument.FullName;
                                    systemCode += $@"
        public readonly HashSet<{componentTypeName}> {propertyName}_collection = new HashSet<{componentTypeName}>();
        protected override IEnumerable<{componentTypeName}> {propertyName} => {propertyName}_collection;
";

                                    var modification = $@"
                if (value != null) {{
                    (({systemTypeName})entity.world.{systemPropertyName}).{propertyName}_collection.Add(value);
                }} else {{
                    (({systemTypeName})entity.world.{systemPropertyName}).{propertyName}_collection.Remove({Decapitalize(genericArgument.Name.Substring(1))});
                }}
";
                                    if (systemModificationsForComponentType.TryGetValue(genericArgument, out HashSet<string> modifications)) {
                                        modifications.Add(modification);
                                    } else {
                                        systemModificationsForComponentType.Add(genericArgument, new HashSet<string> { modification });
                                    }
                                }
                            }
                        }
                    }

                    systemCode += @"
    }
}
";
                    File.WriteAllText(Path.Combine(outputPath, systemTypeName + ".cs"), systemCode);
                }
            }

            // generate component classes that automatically add themselves to the tuple containers of relevant systems and that keep track of state diffs
            foreach (var type in GetAllTypes()) {
                if (typeof(ECS.IComponent).IsAssignableFrom(type) && type.IsInterface && type != typeof(ECS.IComponent)) {
                    if (!type.Name.StartsWith("I") || !type.Name.EndsWith("Component")) {
                        throw new Exception($"Invalid component interface name {type.Name}: Must start with 'I' and end with \"Component\"!");
                    }

                    Console.WriteLine($"Found {typeof(ECS.IComponent)} implementation {type}");

                    var componentTypeName = type.Name[1..];
                    var componentPropertyName = Decapitalize(componentTypeName);

                    Console.WriteLine($"Generating component class \"{componentTypeName}\" from \"{type.FullName}\"...");
                    var componentCode = $@"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System.IO;
using System.Text;

namespace {@namespace} {{
    public class {componentTypeName} : {type.FullName} {{
        public Entity entity {{ get; private set; }}
";

                    var componentConstructorCode = $@"

        public {componentTypeName}(Entity entity) {{
            this.entity = entity;
            entity.{componentPropertyName} = this;
";

                    // *ComponentDiffer.cs
                    var componentDifferCode = $@"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System.IO;

namespace {@namespace} {{
    public class {componentTypeName}Differ : IDiffer<({componentTypeName}, int)> {{
";

                    // EntityDiffer.Diff
                    var componentDiffCode = $@"
        public bool Diff(({componentTypeName}, int) componentAtOldTick, ({componentTypeName}, int) componentAtNewTick, BinaryWriter writer) {{
            return true;
";

                    // EntityDiffer.Patch
                    var componentPatchCode = $@"
        public void Patch(ref ({componentTypeName}, int) component, BinaryReader reader) {{
";

                    var getters = new HashSet<string>();
                    var setters = new HashSet<string>();

                    foreach (var methodInfo in type.GetMethods()) {
                        Console.WriteLine($"Method: {methodInfo.Name}");
                        if (methodInfo.Name.StartsWith("get_") && !getters.Contains(methodInfo.Name)) {
                            var propertyName = methodInfo.Name.Substring(4);
                            getters.Add(propertyName);
                            if (setters.Contains(propertyName)) {
                                GenerateStateProperty(methodInfo.ReturnType, propertyName, Enumerable.Empty<string>(), ref componentCode, ref componentConstructorCode);
                            }
                        }

                        if (methodInfo.Name.StartsWith("set_") && !setters.Contains(methodInfo.Name)) {
                            var propertyName = methodInfo.Name.Substring(4);
                            setters.Add(propertyName);
                            if (getters.Contains(propertyName)) {
                                GenerateStateProperty(methodInfo.GetParameters()[0].ParameterType, propertyName, Enumerable.Empty<string>(), ref componentCode, ref componentConstructorCode);
                            }
                        }
                    }

                    componentCode += $@"
{componentConstructorCode}
        }}
    }}
}}
";
                    File.WriteAllText(Path.Combine(outputPath, componentTypeName + ".cs"), componentCode);

                    componentDifferCode += $@"
{componentDiffCode}
        }}
{componentPatchCode}
        }}
    }}
}}
";
                    File.WriteAllText(Path.Combine(outputPath, $"{componentTypeName}Differ.cs"), componentDifferCode);

                    // GenerateStateProperty for the component field in the entity needs to know what differ type to use
                    differTypeNameForDiffableTypeName.Add(componentTypeName, $"{componentTypeName}Differ");
                    componentTypeNameForIComponentType.Add(type, componentTypeName);

                    Console.WriteLine($"Adding property \"{componentPropertyName}\" to Entity class...");
                    GenerateStateProperty(type, componentPropertyName, systemModificationsForComponentType.TryGetValue(type, out HashSet<string> modifications) ? modifications : Enumerable.Empty<string>(), ref entityCode, ref entityConstructorCode);

                    entityDifferCode += $@"
        private readonly {componentTypeName}Differ {componentPropertyName}Differ = new {componentTypeName}Differ();
";

                    entityDiffCode += $@"
            {componentPropertyName}Differ.Diff((oldEntity.{componentPropertyName}, oldTick), (newEntity.{componentPropertyName}, newTick), writer);
";
                }
            }

            entityCode += $@"
{entityConstructorCode}
        }}
    }}
}}
";
            File.WriteAllText(Path.Combine(outputPath, "Entity.cs"), entityCode);

            entityDifferCode += $@"
{entityDiffCode}
        }}
{entityPatchCode}
        }}
    }}
}}
";
            File.WriteAllText(Path.Combine(outputPath, "EntityDiffer.cs"), entityDifferCode);

            worldCode += @"
        public void Tick() {
            ticks.Add(tick + 1);
";

            while (systemTypes.Count > 0) {
                worldCode += @"
            // no mutual dependencies:
";
                var tickedSystemTypes = new HashSet<Type>();

                foreach (var systemType in systemTypes) {
                    if (dependenciesBySystemType.TryGetValue(systemType, out HashSet<Type> dependencies)) {
                        if (dependencies.Count > 0) {
                            continue;
                        } else {
                            tickedSystemTypes.Add(systemType);
                        }
                    } else {
                        tickedSystemTypes.Add(systemType);
                    }

                    worldCode += $@"
            {Decapitalize(systemType.Name)}.Tick();
";
                }

                if (tickedSystemTypes.Count == 0) {
                    throw new InvalidProgramException("We got circular system dependencies!");
                }

                systemTypes.ExceptWith(tickedSystemTypes);
                foreach (var systemType in systemTypes) {
                    if (dependenciesBySystemType.TryGetValue(systemType, out HashSet<Type> dependencies)) {
                        dependencies.ExceptWith(tickedSystemTypes);
                    }
                }
            }

            worldCode += @"
        }
    }
}
";
            File.WriteAllText(Path.Combine(outputPath, "World.cs"), worldCode);

            worldDifferCode += $@"
    }}
}}
";
            File.WriteAllText(Path.Combine(outputPath, "WorldDiffer.cs"), worldDifferCode);
        }

        private static void GatherDiffers() {
            Console.WriteLine($"Gathering {typeof(IDiffer<>)} implementations...");

            foreach (var type in GetAllTypes()) {
                var interfaces = type.GetInterfaces();
                foreach (var @interface in interfaces) {
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IDiffer<>)) {
                        Console.WriteLine($"Found {@interface} implementation {type}. ||{ToExpression(@interface.GetGenericArguments()[0])}||");
                        var genericArguments = @interface.GetGenericArguments();
                        differTypeNameForDiffableTypeName[ToExpression(@interface.GetGenericArguments()[0])] = ToExpression(type);
                    }
                }
            }
        }

        private static void GenerateStateProperty(Type propertyType, string propertyName, IEnumerable<string> onSet, ref string code, ref string constructorCode) {
            Console.WriteLine($"Property: {propertyName}");

            var propertyTypeName = ToExpression(propertyType);

            if (!componentTypeNameForIComponentType.TryGetValue(propertyType, out string actualTypeName)) {
                actualTypeName = propertyTypeName;
            }

            if (!differTypeNameForDiffableTypeName.TryGetValue(actualTypeName, out string differTypeName)) {
                throw new InvalidProgramException($"No implementation of {typeof(IDiffer<>).MakeGenericType(propertyType)} found!");
            }

            constructorCode += $@"
            {propertyName}_diffWriter = new BinaryWriter({propertyName}_diffBuffer, Encoding.UTF8, leaveOpen: true);
";

            code += $@"
        private readonly Ring<int> {propertyName}_ticks = new Ring<int>();
        private readonly Ring<{actualTypeName}> {propertyName}_states = new Ring<{actualTypeName}>();
        private readonly Ring<bool> {propertyName}_diffs = new Ring<bool>();
        private readonly MemoryStream {propertyName}_diffBuffer = new MemoryStream();
        private readonly BinaryWriter {propertyName}_diffWriter;
        private readonly {differTypeName} {propertyName}_differ = new {differTypeName}();
        public {propertyTypeName} {propertyName} {{
            get {{
                return {propertyName}_states.PeekEnd();
            }}

            set {{{string.Join("\r\n", onSet)}
                if ({propertyName}_ticks.Count > 0 && {propertyName}_ticks.PeekEnd() == entity.world.tick) {{
                    {propertyName}_ticks.PopEnd();
                    {propertyName}_states.PopEnd();
                }}

                {propertyName}_diffWriter.SetPosition(0);
                for (int i = 0; i < {propertyName}_states.Count; ++i) {{
                    int index = {propertyName}_states.Start + i;
                    {propertyName}_diffs[index] = {propertyName}_differ.Diff({propertyName}_states[index], ({actualTypeName})value, {propertyName}_diffWriter);
                }}

                {propertyName}_states.PushEnd(({actualTypeName})value);
                {propertyName}_ticks.PushEnd(entity.world.tick);

                while ({propertyName}_diffs.Count > 0 && !{propertyName}_diffs.PeekEnd()) {{
                    {propertyName}_diffs.PopEnd();
                    {propertyName}_ticks.PopEnd();
                    {propertyName}_states.PopEnd();
                }}
            }}
        }}
";
        }

        private static IEnumerable<Type> GetAllTypes() {
            return assembliesByFullName.Values.SelectMany(assembly => {
                try {
                    return assembly.GetTypes();
                } catch (Exception e) {
                    Console.Error.WriteLine(e);
                    return new Type[] { };
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
            return s[0].ToString().ToLower() + s.Substring(1);
        }
    }
}
