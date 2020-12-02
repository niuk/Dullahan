/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System;
using System.Collections.Generic;
using Dullahan;

namespace TestServer {
    public class World {
        public int tick { get; private set; }

        public readonly Dictionary<Guid, Entity> entitiesById = new Dictionary<Guid, Entity>();

        public readonly TestServer.Source.Systems.InputSystem inputSystem = new InputSystem_Implementation();

        public readonly TestServer.Source.Systems.MovementSystem movementSystem = new MovementSystem_Implementation();

        public void Tick() {
            ++tick;

            // no mutual dependencies:

            inputSystem.Tick();

            // no mutual dependencies:

            movementSystem.Tick();

        }
    }
}
