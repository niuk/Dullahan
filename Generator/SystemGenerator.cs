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

            foreach (var (observerName, observedTypes, isSingleton) in systemType.GetObserverNameAndObservedTypes()) {
                var isTuple = observedTypes.Length > 1;
                var tuplePrefix = isTuple ? "(" : "";
                var tupleSuffix = isTuple ? ")" : "";
                var observedIComponentTypeNames = string.Join(", ", observedTypes.Select(t => t.FullName));
                var observedIComponentPropertyNames = string.Join(", ", observedTypes.Select(t => $"({t.FullName}){observerName}_entity.{t.Name[1..].Decapitalize()}"));
                if (isSingleton) {
                    systemImplementation += $@"
            public Entity {observerName}_entity = null;
            protected override {tuplePrefix}{observedIComponentTypeNames}{tupleSuffix} {observerName} => {tuplePrefix}{observedIComponentPropertyNames}{tupleSuffix};
";
                } else {
                    systemImplementation += $@"
            public readonly HashSet<Entity> {observerName}_entities = new HashSet<Entity>();
            protected override IEnumerable<{tuplePrefix}{observedIComponentTypeNames}{tupleSuffix}> {
                        observerName} => {observerName}_entities.Select({observerName}_entity => {tuplePrefix}{observedIComponentPropertyNames}{tupleSuffix});
";
                }
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
