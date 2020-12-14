/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System.Collections.Generic;
using System.Linq;

namespace TestGame {
    partial class World {
        public sealed class VisualizationSystem_Implementation : TestGame.VisualizationSystem {

            public readonly HashSet<Entity> avatars_entities = new HashSet<Entity>();
            protected override IEnumerable<(TestGame.IPositionComponent, TestGame.IViewComponent)> avatars => avatars_entities.Select(avatars_entity => ((TestGame.IPositionComponent)avatars_entity.positionComponent, (TestGame.IViewComponent)avatars_entity.viewComponent));

            public Entity consoleBuffer_entity = null;
            protected override TestGame.IConsoleBufferComponent consoleBuffer => (TestGame.IConsoleBufferComponent)consoleBuffer_entity.consoleBufferComponent;

        }
    }
}
