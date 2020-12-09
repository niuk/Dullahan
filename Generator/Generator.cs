﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Dullahan.Generator {
    public static class Generator {
        private static readonly Dictionary<string, string> differTypeNameForDiffableTypeName = new Dictionary<string, string>();
        private static readonly Dictionary<string, Assembly> assembliesByFullName = new Dictionary<string, Assembly>();

        public static void Main(string[] args) {
            if (args.Length == 0) {
                args = new string[] { "help" };
            }

            switch (args[0]) {
                case "generate":
                    LoadAssemblyWithReferencedAssemblies(Assembly.GetExecutingAssembly());

                    var outputDirectory = args[1];
                    var @namespace = args[2];
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

                    Generate(@namespace, outputDirectory);

                    break;
                case "clean":
                    outputDirectory = args[1];
                    Clean(outputDirectory);

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

        public static void Generate(string @namespace, string outputDirectory) {
            File.WriteAllText(
                Path.Combine(outputDirectory, "World.cs"),
                WorldGenerator.GenerateWorld(@namespace));
            File.WriteAllText(
                Path.Combine(outputDirectory, "WorldDiffer.cs"),
                WorldGenerator.GenerateWorldDiffer(@namespace));

            File.WriteAllText(
                Path.Combine(outputDirectory, "Entity.cs"),
                EntityGenerator.GenerateEntity(@namespace));
            File.WriteAllText(
                Path.Combine(outputDirectory, "EntityDiffer.cs"),
                EntityGenerator.GenerateEntityDiffer(@namespace));

            foreach (var systemType in GetSystemTypes()) {
                File.WriteAllText(
                    Path.Combine(outputDirectory, $"{systemType.Name}_Implementation.cs"),
                    SystemGenerator.GenerateSystemImplementation(@namespace, systemType));
            }

            foreach (var IComponentType in GetIComponentTypes()) {
                var componentTypeName = IComponentType.Name[1..];
                File.WriteAllText(
                    Path.Combine(outputDirectory, $"{componentTypeName}.cs"),
                    ComponentGenerator.GenerateComponent(@namespace, IComponentType, differTypeNameForDiffableTypeName));
                File.WriteAllText(
                    Path.Combine(outputDirectory, $"{componentTypeName}Differ.cs"),
                    ComponentGenerator.GenerateComponentDiffer(@namespace, IComponentType));
            }
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

        public static void Clean(string outputDirectory) {
            foreach (var file in Directory.GetFiles(outputDirectory)) {
                Console.WriteLine($"Deleting \"{file}\"...");
                File.Delete(file);
            }
        }

        public static IEnumerable<Type> GetIComponentTypes() {
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

        public static IEnumerable<Type> GetSystemTypes() {
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

        public static IEnumerable<(string, IEnumerable<Type>)> GetObserverNameAndObservedIComponentTypes(this Type systemType) {
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

        public static string ToExpression(this Type type) {
            if (type.IsGenericType) {
                return $"{type.GetGenericTypeDefinition().FullName.Split('`')[0]}<{string.Join(", ", type.GetGenericArguments().Select(ToExpression))}>";
            } else {
                return type.FullName;
            }
        }

        public static string Decapitalize(this string s) {
            return s[0].ToString().ToLower() + s[1..];
        }
    }
}