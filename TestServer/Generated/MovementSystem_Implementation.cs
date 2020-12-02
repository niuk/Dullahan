/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System;
using System.Collections.Generic;
using System.Linq;

namespace TestServer {
    public class MovementSystem_Implementation : TestServer.Source.Systems.MovementSystem {

        public readonly HashSet<Entity> controllables_collection = new HashSet<Entity>();
        protected override IEnumerable<Tuple<TestServer.Source.Components.IInputComponent, TestServer.Source.Components.IPositionComponent>> controllables => controllables_collection.Select(entity => Tuple.Create(entity.inputComponent, entity.positionComponent));

    }
}
