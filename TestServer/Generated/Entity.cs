/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System;

namespace TestServer {
    partial class World {
        public sealed partial class Entity : IDisposable {
            public readonly Guid id = Guid.NewGuid();

            public readonly World world;
            public readonly int constructionTick;
            public int disposalTick { get; private set; }

            public Entity(World world) {
                this.world = world;
                world.entitiesById.Add(id, this);
                constructionTick = world.tick;
                disposalTick = int.MaxValue;
            }

            private void Dispose(bool disposing) {
                if (disposalTick == int.MaxValue) {
                    if (disposing) {
                        inputComponent.Dispose();

                        positionComponent.Dispose();

                    }

                    disposalTick = world.tick;
                }
            }

            public void Dispose() {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }

            public InputComponent inputComponent { get; private set; }
            public int inputComponent_disposalTick { get; private set; } = -1;

            public PositionComponent positionComponent { get; private set; }
            public int positionComponent_disposalTick { get; private set; } = -1;

        }
    }
}