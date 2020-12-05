using System;
using System.Linq;

namespace Dullahan.Generator {
    static class SystemGenerator {
        public static string GenerateSystemImplementation(string @namespace, Type systemType) {
            Console.WriteLine($"Generating system implementation class \"{systemType.Name}_Implementation\" from \"{systemType.FullName}\"...");

            var systemImplementation = $@"/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System.Collections.Generic;
using System.Linq;

namespace {@namespace} {{
    partial class World {{
        public sealed class {systemType.Name}_Implementation : {systemType.FullName} {{
";

            foreach (var (observerName, observedIComponentTypes) in systemType.GetObserverNameAndObservedIComponentTypes()) {
                var isTuple = observedIComponentTypes.Count() > 1;
                var tuplePrefix = isTuple ? "(" : "";
                var tupleSuffix = isTuple ? ")" : "";
                var observedIComponentTypeNames = string.Join(", ", observedIComponentTypes.Select(t => t.FullName));
                var observedIComponentPropertyNames = string.Join(", ", observedIComponentTypes.Select(t => $"({t.FullName})entity.{t.Name[1..].Decapitalize()}"));
                systemImplementation += $@"
            public readonly HashSet<Entity> {observerName}_collection = new HashSet<Entity>();
            protected override IEnumerable<{tuplePrefix}{observedIComponentTypeNames}{tupleSuffix}> {
                    observerName} => {observerName}_collection.Select(entity => {tuplePrefix}{observedIComponentPropertyNames}{tupleSuffix});
";
            }

            systemImplementation += @"
        }
    }
}
";
            return systemImplementation;
        }
    }
}
