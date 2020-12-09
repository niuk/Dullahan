/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;

namespace TestServer {
    partial class World {
        public sealed partial class Entity : IDisposable {
            public readonly Guid id;

            public readonly World world;
            public readonly int constructionTick;
            public int disposalTick { get; private set; }

            public Entity(World world) : this(world, Guid.NewGuid()) { }

            public Entity(World world, Guid id) {
                this.id = id;
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

            private Ring<int> inputComponent_ticks = new Ring<int>();
            private Ring<InputComponent> inputComponent_snapshots = new Ring<InputComponent>();
            public InputComponent inputComponent {
                get {
                    return inputComponent_snapshots.PeekEnd();
                }

                private set {
                    if (inputComponent_ticks.PeekEnd() == world.tick) {
                        inputComponent_ticks.PopEnd();
                        inputComponent_snapshots.PopEnd();
                    }

                    if (inputComponent != value) {
                        inputComponent_ticks.PushEnd(world.tick);
                        inputComponent_snapshots.PushEnd(value);
                    }
                }
            }

            public int inputComponent_disposalTick { get; private set; } = -1;

            private Ring<int> positionComponent_ticks = new Ring<int>();
            private Ring<PositionComponent> positionComponent_snapshots = new Ring<PositionComponent>();
            public PositionComponent positionComponent {
                get {
                    return positionComponent_snapshots.PeekEnd();
                }

                private set {
                    if (positionComponent_ticks.PeekEnd() == world.tick) {
                        positionComponent_ticks.PopEnd();
                        positionComponent_snapshots.PopEnd();
                    }

                    if (positionComponent != value) {
                        positionComponent_ticks.PushEnd(world.tick);
                        positionComponent_snapshots.PushEnd(value);
                    }
                }
            }

            public int positionComponent_disposalTick { get; private set; } = -1;

        }
    }
}