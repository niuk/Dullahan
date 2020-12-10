/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System.Collections.Generic;
using System.Linq;

namespace TestGame {
    partial class World {
        public sealed class VisualizationSystem_Implementation : TestGame.VisualizationSystem {

            public readonly HashSet<Entity> positionComponents_collection = new HashSet<Entity>();
            protected override IEnumerable<TestGame.IPositionComponent> positionComponents => positionComponents_collection.Select(entity => (TestGame.IPositionComponent)entity.positionComponent);

        }
    }
}
