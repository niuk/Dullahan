using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Dullahan {
    public class ClassGenerator {
        public static void DeleteGeneratedClasses(string outputDirectory, Action<string> logCallback) {
            foreach (var file in Directory.GetFiles(Path.Combine("Assets", outputDirectory))) {
                logCallback($"Deleting \"{file}\"...");
                File.Delete(file);
            }
        }

        public static void GenerateClasses(string outputDirectory, string @namespace, Action<string> logCallback) {
            GatherDiffers(logCallback);

            var worldCode = @$"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;

namespace {@namespace} {{
    public class World : IWorld {{
        public int tick {{ get; private set; }} = 0;

        public void Tick() {{
            ++tick;
        }}
";

            var entityCode = @$"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;

namespace {@namespace} {{
    public class Entity : IEntity {{
        public IWorld world {{ get; private set; }}
        public Entity(World world) {{
            this.world = world;
        }}
";

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                foreach (var type in assembly.GetTypes()) {
                    if (typeof(IComponent).IsAssignableFrom(type) && type.IsInterface && type != typeof(IComponent)) {
                        if (!type.Name.StartsWith("I") || !type.Name.EndsWith("Component")) {
                            throw new Exception($"Invalid Component interface name {type.Name}: Must start with 'I' and end with \"Component\"!");
                        }

                        var componentName = type.Name.Substring(1);
                        logCallback($"Generating component class \"{componentName}\" from \"{type.FullName}\"...");

                        GenerateStateProperty(type, componentName[0].ToString().ToLower() + componentName.Substring(1), ref entityCode, logCallback);

                        var componentCode = @$"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;

namespace {@namespace} {{
    public class {componentName} : {ToExpression(type)} {{
        public IEntity entity {{ get; private set; }}

        public {componentName}(Entity entity) {{
            this.entity = entity;
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
                        File.WriteAllText(Path.Combine("Assets", outputDirectory, componentName + ".cs"), componentCode);
                    }
                }
            }

            entityCode += @"
    }
}
";
            File.WriteAllText(Path.Combine("Assets", outputDirectory, "Entity.cs"), entityCode);

            worldCode += @"
    }
}
";
            File.WriteAllText(Path.Combine("Assets", outputDirectory, "World.cs"), worldCode);
        }

        private static readonly Dictionary<Type, Tuple<Type, Type>> differTypesAndDiffTypesByDiffableType = new Dictionary<Type, Tuple<Type, Type>>();

        private static void GatherDiffers(Action<string> logCallback) {
            logCallback($"Gathering {typeof(IDiffer<,>)} implementations...");

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                foreach (var type in assembly.GetTypes()) {
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
        }

        private static void GenerateStateProperty(Type propertyType, string propertyName, ref string code, Action<string> logCallback) {
            logCallback($"Property: {propertyName}");
            var propertyTypeName = ToExpression(propertyType);
            if (differTypesAndDiffTypesByDiffableType.TryGetValue(propertyType, out Tuple<Type, Type> differTypeAndDiffType)) {
                var differTypeName = ToExpression(differTypeAndDiffType.Item1);
                var diffTypeName = ToExpression(differTypeAndDiffType.Item2);

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
            } else {
                var diffType = typeof(IDiffer<,>).GetGenericArguments()[1];
                throw new InvalidProgramException($"No implementation of {typeof(IDiffer<,>).MakeGenericType(propertyType, diffType)} found!");
            }
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
