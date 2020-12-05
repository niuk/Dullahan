/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System.Collections.Generic;
using System.Linq;

namespace TestServer {
    partial class World {
        public sealed class InputSystem_Implementation : TestServer.InputSystem {

            public readonly HashSet<Entity> inputComponents_collection = new HashSet<Entity>();
            protected override IEnumerable<TestServer.IInputComponent> inputComponents => inputComponents_collection.Select(entity => (TestServer.IInputComponent)entity.inputComponent);

        }
    }
}
