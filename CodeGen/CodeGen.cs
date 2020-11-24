using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Dullahan {
    public class CodeGen {
        private static readonly Dictionary<Type, Tuple<Type, Type>> differTypesAndDiffTypesByDiffableType = new Dictionary<Type, Tuple<Type, Type>>();
        private static readonly Dictionary<string, Assembly> assembliesByFullName = new Dictionary<string, Assembly>();

        private static IEnumerable<string> GetFilesRecursive(string directory, string pattern) {
            return Directory.GetFiles(directory, pattern).Concat(Directory.GetDirectories(directory).SelectMany(subDirectory => GetFilesRecursive(subDirectory, pattern)));
        }

        private static void AddAssemblyWithReferencedAssemblies(Assembly assembly) {
            if (!assembliesByFullName.ContainsKey(assembly.FullName)) {
                assembliesByFullName.Add(assembly.FullName, assembly);
                //Console.WriteLine($"Added assembly \"{assembly.FullName}\"");
                foreach (var referencedAssemblyName in assembly.GetReferencedAssemblies()) {
                    try {
                        AddAssemblyWithReferencedAssemblies(Assembly.Load(referencedAssemblyName));
                    } catch (FileNotFoundException e) {
                        Console.Error.WriteLine(e);
                    }
                }
            }
        }

        private static void AddAssemblyFromFile(string filePath) {
            AddAssemblyWithReferencedAssemblies(Assembly.LoadFile(filePath));
        }

        private static void CompileFilesInDirectory(string directory) {
            var syntaxTrees = GetFilesRecursive(directory, "*.cs").Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file)).WithFilePath(file));
            foreach (var syntaxTree in syntaxTrees) {
                //Console.WriteLine($"{syntaxTree.FilePath}:\n{syntaxTree.ToString()}\n");
            }

            var references = assembliesByFullName.Values.Where(assembly => !string.IsNullOrEmpty(assembly.Location)).Select(assembly => MetadataReference.CreateFromFile(assembly.Location));
            references = references.Concat(new[] { MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location) });
            foreach (var reference in references) {
                //Console.WriteLine(reference.Display);
            }

            var compilation = CSharpCompilation.Create(Path.GetRandomFileName(), syntaxTrees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using (var memoryStream = new MemoryStream()) {
                var emitResult = compilation.Emit(memoryStream);
                if (emitResult.Success) {
                    Console.WriteLine($"Compiled sources in \"{directory}\"");
                    var assembly = Assembly.Load(memoryStream.ToArray());
                    assembliesByFullName.Add(assembly.FullName, assembly);
                    Console.WriteLine($"Added compiled assembly \"{assembly.FullName}\"");
                } else {
                    foreach (var diagnostic in emitResult.Diagnostics) {
                        Console.Error.WriteLine(diagnostic);
                    }

                    throw new InvalidProgramException($"Could not compile sources in {directory}!");
                }
            }
        }

        public static void Main(string[] args) {
            if (args.Length == 0) {
                args = new string[] { "help" };
            }

            switch (args[0]) {
                case "generate":
                    AddAssemblyWithReferencedAssemblies(Assembly.GetExecutingAssembly());

                    for (int i = 4; i < args.Length; ++i) {
                        Console.WriteLine($"Referenced assembly: {args[i]}");
                        AddAssemblyFromFile(args[i]);
                    }

                    Console.WriteLine($"Source directory: {args[3]}");
                    CompileFilesInDirectory(args[3]);

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
                    throw new ArgumentException($"Unrecognized command \"{args[0]}\": must be \"help\", \"generate\" or \"clean\".");
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
using Dullahan;

namespace {@namespace} {{
    public class World {{
        public int tick {{ get; private set; }}
";

            // Entity.cs
            var entityCode = $@"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;

namespace {@namespace} {{
    public class Entity {{
        public World world {{ get; private set; }}

        private Entity entity => this;

        public Entity(World world) {{
            this.world = world;
        }}
";

            // generate system classes first so that we know which systems need to add/remove component tuples when components are added to/removed from entities
            var systemModificationsForComponentType = new Dictionary<Type, HashSet<string>>();
            foreach (var type in GetAllTypes()) {
                if (typeof(ECS.ISystem).IsAssignableFrom(type) && type.IsAbstract && type != typeof(ECS.ISystem)) {
                    if (!type.Name.EndsWith("System")) {
                        throw new Exception($"Invalid abstract system name {type.Name}: Must end with \"System\"!");
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
        private int _tick = 0;
        public override int tick => _tick;

        public override void Tick() {{
            ++_tick;
            base.Tick();
        }}
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

                    var componentTypeName = type.Name.Substring(1);
                    var componentPropertyName = Decapitalize(componentTypeName);

                    Console.WriteLine($"Adding property \"{componentPropertyName}\" to Entity class...");
                    GenerateStateProperty(type, componentPropertyName, systemModificationsForComponentType.TryGetValue(type, out HashSet<string> modifications) ? modifications : Enumerable.Empty<string>(), ref entityCode);

                    Console.WriteLine($"Generating component class \"{componentTypeName}\" from \"{type.FullName}\"...");
                    var componentCode = $@"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;

namespace {@namespace} {{
    public class {componentTypeName} : {type.FullName} {{
        public Entity entity {{ get; private set; }}

        public {componentTypeName}(Entity entity) {{
            this.entity = entity;
            entity.{componentPropertyName} = this;
        }}
";

                    var getters = new HashSet<string>();
                    var setters = new HashSet<string>();

                    foreach (var methodInfo in type.GetMethods()) {
                        Console.WriteLine($"Method: {methodInfo.Name}");
                        if (methodInfo.Name.StartsWith("get_") && !getters.Contains(methodInfo.Name)) {
                            var propertyName = methodInfo.Name.Substring(4);
                            getters.Add(propertyName);
                            if (setters.Contains(propertyName)) {
                                GenerateStateProperty(methodInfo.ReturnType, propertyName, Enumerable.Empty<string>(), ref componentCode);
                            }
                        }

                        if (methodInfo.Name.StartsWith("set_") && !setters.Contains(methodInfo.Name)) {
                            var propertyName = methodInfo.Name.Substring(4);
                            setters.Add(propertyName);
                            if (getters.Contains(propertyName)) {
                                GenerateStateProperty(methodInfo.GetParameters()[0].ParameterType, propertyName, Enumerable.Empty<string>(), ref componentCode);
                            }
                        }
                    }

                    componentCode += @"
    }
}
";
                    File.WriteAllText(Path.Combine(outputPath, componentTypeName + ".cs"), componentCode);
                }
            }

            entityCode += @"
    }
}
";
            File.WriteAllText(Path.Combine(outputPath, "Entity.cs"), entityCode);

            worldCode += @"
    }
}
";
            File.WriteAllText(Path.Combine(outputPath, "World.cs"), worldCode);
        }

        private static void GatherDiffers() {
            Console.WriteLine($"Gathering {typeof(IDiffer<,>)} implementations...");

            foreach (var type in GetAllTypes()) {
                var interfaces = type.GetInterfaces();
                foreach (var @interface in interfaces) {
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IDiffer<,>)) {
                        Console.WriteLine($"Found {@interface} implementation {type}.");
                        var genericArguments = @interface.GetGenericArguments();
                        differTypesAndDiffTypesByDiffableType[genericArguments[0]] = Tuple.Create(type, genericArguments[1]);
                    }
                }
            }
        }

        private static void GenerateStateProperty(Type propertyType, string propertyName, IEnumerable<string> onSet, ref string code) {
            Console.WriteLine($"Property: {propertyName}");

            string differTypeName;
            string diffTypeName;
            var propertyTypeName = ToExpression(propertyType);
            if (differTypesAndDiffTypesByDiffableType.TryGetValue(propertyType, out Tuple<Type, Type> differTypeAndDiffType)) {
                differTypeName = ToExpression(differTypeAndDiffType.Item1);
                diffTypeName = ToExpression(differTypeAndDiffType.Item2);
            } else if (propertyType.IsClass || propertyType.IsInterface) {
                differTypeName = $"ReferenceDiffer<{propertyTypeName}>";
                diffTypeName = propertyTypeName;
            } else {
                var diffType = typeof(IDiffer<,>).GetGenericArguments()[1];
                throw new InvalidProgramException($"No implementation of {typeof(IDiffer<,>).MakeGenericType(propertyType, diffType)} found!");
            }

            code += $@"
        private readonly Ring<int> {propertyName}_ticks = new Ring<int>();
        private readonly Ring<{propertyTypeName}> {propertyName}_states = new Ring<{propertyTypeName}>();
        private readonly Ring<Maybe<{diffTypeName}>> {propertyName}_diffs = new Ring<Maybe<{diffTypeName}>>();
        public {propertyTypeName} {propertyName} {{
            get {{
                return {propertyName}_states.PeekEnd();
            }}

            set {{{string.Join("\r\n", onSet)}
                if ({propertyName}_ticks.Count > 0 && {propertyName}_ticks.PeekEnd() == entity.world.tick) {{
                    {propertyName}_states.PopEnd();
                    {propertyName}_ticks.PopEnd();
                }}

                var differ = new {differTypeName}();
                for (int i = 0; i < {propertyName}_states.Count; ++i) {{
                    int index = {propertyName}_states.Start + i;
                    if (differ.Diff({propertyName}_states[index], value, out {diffTypeName} diff)) {{
                        {propertyName}_diffs[index] = new Maybe<{diffTypeName}>.Just(diff);
                    }} else {{
                        {propertyName}_diffs[index] = new Maybe<{diffTypeName}>.Nothing();
                    }}
                }}

                {propertyName}_states.PushEnd(value);
                {propertyName}_ticks.PushEnd(entity.world.tick);

                while ({propertyName}_diffs.Count > 0 && {propertyName}_diffs.PeekEnd() == null) {{
                    {propertyName}_ticks.PopEnd();
                    {propertyName}_states.PopEnd();
                    {propertyName}_diffs.PopEnd();
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
