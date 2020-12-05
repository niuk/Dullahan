/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System.Collections.Generic;
using System.Linq;

namespace TestServer {
    partial class World {
        public sealed class MovementSystem_Implementation : TestServer.MovementSystem {

            public readonly HashSet<Entity> controllables_collection = new HashSet<Entity>();
            protected override IEnumerable<(TestServer.IInputComponent, TestServer.IPositionComponent)> controllables => controllables_collection.Select(entity => ((TestServer.IInputComponent)entity.inputComponent, (TestServer.IPositionComponent)entity.positionComponent));

        }
    }
}
