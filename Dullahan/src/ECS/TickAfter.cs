using System;

namespace Dullahan.ECS {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class TickAfter : Attribute {
        public readonly Type systemType;

        public TickAfter(Type systemType) {
            this.systemType = systemType;
        }
    }
}
