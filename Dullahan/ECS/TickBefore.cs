using System;

namespace Dullahan.ECS {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class TickBefore : Attribute {
        public readonly Type systemType;

        public TickBefore(Type systemType) {
            this.systemType = systemType;
        }
    }
}
