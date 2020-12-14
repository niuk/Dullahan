/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System.Collections.Generic;
using System.Linq;

namespace TestGame {
    partial class World {
        public sealed class MovementSystem_Implementation : TestGame.MovementSystem {

            public readonly HashSet<Entity> controllables_entities = new HashSet<Entity>();
            protected override IEnumerable<(TestGame.IVelocityComponent, TestGame.IPositionComponent)> controllables => controllables_entities.Select(controllables_entity => ((TestGame.IVelocityComponent)controllables_entity.velocityComponent, (TestGame.IPositionComponent)controllables_entity.positionComponent));

            public Entity timeComponent_entity = null;
            protected override TestGame.ITimeComponent timeComponent => (TestGame.ITimeComponent)timeComponent_entity.timeComponent;

        }
    }
}
