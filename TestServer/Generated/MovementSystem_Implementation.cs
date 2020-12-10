/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System.Collections.Generic;
using System.Linq;

namespace TestGame {
    partial class World {
        public sealed class MovementSystem_Implementation : TestGame.MovementSystem {

            public readonly HashSet<Entity> controllables_collection = new HashSet<Entity>();
            protected override IEnumerable<(TestGame.IInputComponent, TestGame.IPositionComponent)> controllables => controllables_collection.Select(entity => ((TestGame.IInputComponent)entity.inputComponent, (TestGame.IPositionComponent)entity.positionComponent));

        }
    }
}
