using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dullahan {
    public class ClassGenerator {
        public static void DeleteGeneratedClasses(string outputDirectory, Action<string> logCallback) {
            foreach (var file in Directory.GetFiles(outputDirectory)) {
                logCallback($"Deleting \"{file}\"...");
                File.Delete(file);
            }
        }

        private static string Decapitalize(string s) {
            return s[0].ToString().ToLower() + s.Substring(1);
        }

        public static void GenerateClasses(string outputDirectory, string @namespace, Action<string> logCallback) {
            // differ types need to be known for the generation of component property accessors below
            GatherDiffers(logCallback);

            // World.cs
            var worldCode = @$"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;

namespace {@namespace} {{
    public class World {{
";

            // Entity.cs
            var entityCode = @$"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
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
            var systemFieldsWithComponentType = new Dictionary<Type, Tuple<Type, string>>();
            foreach (var type in GetAllTypes()) {
                if (typeof(ECS.System).IsAssignableFrom(type) && type.IsAbstract && type != typeof(ECS.System)) {
                    if (!type.Name.EndsWith("System")) {
                        throw new Exception($"Invalid abstract system name {type.Name}: Must end with \"System\"!");
                    }

                    var systemTypeName = type.Name + "Implementation";
                    var systemPropertyName = Decapitalize(type.Name);

                    logCallback($"Adding property \"{systemPropertyName}\" to World class...");
                    worldCode += $@"
        private readonly {ToExpression(type)} {systemPropertyName} = new {systemTypeName}();
";

                    logCallback($"Generating system class \"{systemTypeName}\" from \"{type.FullName}\"...");
                    var systemCode = @$"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System;
using System.Collections.Generic;
using System.Linq;

namespace {@namespace} {{
    public class {systemTypeName} : {ToExpression(type)} {{
";

                    foreach (var method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)) {
                        if (method.Name.StartsWith("get_") && method.IsAbstract) {
                            var propertyName = method.Name.Substring(4);
                            var enumerableType = method.ReturnType;
                            if (enumerableType.IsGenericType && enumerableType.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
                                logCallback($"Generating accessor {propertyName} with type {enumerableType}...");
                                var genericArgument = enumerableType.GetGenericArguments()[0];
                                if (genericArgument.IsGenericType && genericArgument.GetGenericTypeDefinition().Name.Split('`')[0] == "Tuple") {
                                    // store the entities directly into the collection
                                    var componentTypes = genericArgument.GetGenericArguments();
                                    systemCode += $@"
        public readonly HashSet<Entity> {propertyName}_collection = new HashSet<Entity>();
        protected override IEnumerable<Tuple<{
                                        string.Join(", ", componentTypes.Select(ToExpression))
                                        }>> {propertyName} => {propertyName}_collection.Select(entity => Tuple.Create({
                                        string.Join(", ", componentTypes.Select(componentType => "entity." + Decapitalize(componentType.Name.Substring(1))))
                                        }));
";
                                } else {
                                    // store just the components
                                    var componentTypeExpression = ToExpression(genericArgument);
                                    systemCode += $@"
        public readonly HashSet<{componentTypeExpression}> {propertyName}_collection = new HashSet<{componentTypeExpression}>();
        protected override IEnumerable<{componentTypeExpression}> {propertyName} => {propertyName}_collection;
";
                                }
                            }
                        }
                    }

                    systemCode += @"
    }
}
";
                    File.WriteAllText(Path.Combine(outputDirectory, systemTypeName + ".cs"), systemCode);
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

                    logCallback($"Adding property \"{componentPropertyName}\" to Entity class...");
                    GenerateStateProperty(type, componentPropertyName, ref entityCode, logCallback);

                    logCallback($"Generating component class \"{componentTypeName}\" from \"{type.FullName}\"...");
                    var componentCode = @$"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;

namespace {@namespace} {{
    public class {componentTypeName} : {ToExpression(type)} {{
        public Entity entity {{ get; private set; }}

        public {componentTypeName}(Entity entity) {{
            this.entity = entity;
            entity.{componentPropertyName} = this;
        }}
";

                    var getters = new HashSet<string>();
                    var setters = new HashSet<string>();

                    foreach (var methodInfo in type.GetMethods()) {
                        logCallback($"Method: {methodInfo.Name}");
                        if (methodInfo.Name.StartsWith("get_") && !getters.Contains(methodInfo.Name)) {
                            var propertyName = methodInfo.Name.Substring(4);
                            getters.Add(propertyName);
                            if (setters.Contains(propertyName)) {
                                GenerateStateProperty(methodInfo.ReturnType, propertyName, ref componentCode, logCallback);
                            }
                        }

                        if (methodInfo.Name.StartsWith("set_") && !setters.Contains(methodInfo.Name)) {
                            var propertyName = methodInfo.Name.Substring(4);
                            setters.Add(propertyName);
                            if (getters.Contains(propertyName)) {
                                GenerateStateProperty(methodInfo.GetParameters()[0].ParameterType, propertyName, ref componentCode, logCallback);
                            }
                        }
                    }

                    componentCode += @"
    }
}
";
                    File.WriteAllText(Path.Combine(outputDirectory, componentTypeName + ".cs"), componentCode);
                }
            }

            entityCode += @"
    }
}
";
            File.WriteAllText(Path.Combine(outputDirectory, "Entity.cs"), entityCode);

            worldCode += @"
    }
}
";
            File.WriteAllText(Path.Combine(outputDirectory, "World.cs"), worldCode);
        }

        private static readonly Dictionary<Type, Tuple<Type, Type>> differTypesAndDiffTypesByDiffableType = new Dictionary<Type, Tuple<Type, Type>>();

        private static void GatherDiffers(Action<string> logCallback) {
            logCallback($"Gathering {typeof(IDiffer<,>)} implementations...");

            foreach (var type in GetAllTypes()) {
                var interfaces = type.GetInterfaces();
                foreach (var @interface in interfaces) {
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IDiffer<,>)) {
                        logCallback($"Found {@interface} implementation {type}.");
                        var genericArguments = @interface.GetGenericArguments();
                        differTypesAndDiffTypesByDiffableType[genericArguments[0]] = Tuple.Create(type, genericArguments[1]);
                    }
                }
            }
        }

        private static void GenerateStateProperty(Type propertyType, string propertyName, ref string code, Action<string> logCallback) {
            logCallback($"Property: {propertyName}");

            string differTypeName;
            string diffTypeName;
            var propertyTypeName = ToExpression(propertyType);
            if (differTypesAndDiffTypesByDiffableType.TryGetValue(propertyType, out Tuple<Type, Type> differTypeAndDiffType)) {
                differTypeName = ToExpression(differTypeAndDiffType.Item1);
                diffTypeName = ToExpression(differTypeAndDiffType.Item2);
            } else if (propertyType.IsClass || propertyType.IsInterface) {
                differTypeName = $"GenericClassObjectDiffer<{propertyTypeName}>";
                diffTypeName = propertyTypeName;
            } else {
                var diffType = typeof(IDiffer<,>).GetGenericArguments()[1];
                throw new InvalidProgramException($"No implementation of {typeof(IDiffer<,>).MakeGenericType(propertyType, diffType)} found!");
            }

            code += $@"
        private readonly Ring<int> {propertyName}_ticks = new Ring<int>();
        private readonly Ring<{propertyTypeName}> {propertyName}_states = new Ring<{propertyTypeName}>();
        private readonly Ring<{diffTypeName}> {propertyName}_diffs = new Ring<{diffTypeName}>();
        public {propertyTypeName} {propertyName} {{
            get {{
                return {propertyName}_states.PeekEnd();
            }}

            set {{
                if ({propertyName}_ticks.Count > 0 && {propertyName}_ticks.PeekEnd() == entity.world.tick) {{
                    {propertyName}_states.PopEnd();
                    {propertyName}_ticks.PopEnd();
                }}

                var differ = new {differTypeName}();
                for (int i = 0; i < {propertyName}_states.Count; ++i) {{
                    int index = {propertyName}_states.Start + i;
                    if (differ.Diff({propertyName}_states[index], value, out {diffTypeName} diff)) {{
                        {propertyName}_diffs[index] = diff;
                    }} else {{
                        {propertyName}_diffs[index] = null;
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
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes());
        }

        private static string ToExpression(Type type) {
            if (type.IsGenericType) {
                return $"{type.GetGenericTypeDefinition().FullName.Split('`')[0]}<{string.Join(", ", type.GetGenericArguments().Select(ToExpression))}>";
            } else {
                return type.FullName;
            }
        }
    }
}
