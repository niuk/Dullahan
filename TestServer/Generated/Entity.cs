/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;

namespace TestGame {
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
                constructionTick = world.nextTick;
                disposalTick = int.MaxValue;
            }

            private Entity() { }

            private void Dispose(bool disposing) {
                if (disposalTick == int.MaxValue) {
                    if (disposing) {
                        inputComponent.Dispose();

                        positionComponent.Dispose();

                    }

                    disposalTick = world.nextTick;
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
                    if (inputComponent_ticks.Count == 0) {
                        return default;
                    }

                    int index = inputComponent_ticks.BinarySearch(world.previousTick);
                    if (index < 0) {
                        return inputComponent_snapshots[~index - 1];
                    } else {
                        return inputComponent_snapshots[index];
                    }
                }

                private set {
                    if (inputComponent != value) {
                        int index = inputComponent_ticks.BinarySearch(world.nextTick);
                        if (index < 0) {
                            inputComponent_ticks.Insert(~index, world.nextTick);
                            inputComponent_snapshots.Insert(~index, value);
                        } else {
                            inputComponent_ticks[index] = world.nextTick;
                            inputComponent_snapshots[index] = value;
                        }
                    }
                }
            }

            private Ring<int> positionComponent_ticks = new Ring<int>();
            private Ring<PositionComponent> positionComponent_snapshots = new Ring<PositionComponent>();
            public PositionComponent positionComponent {
                get {
                    if (positionComponent_ticks.Count == 0) {
                        return default;
                    }

                    int index = positionComponent_ticks.BinarySearch(world.previousTick);
                    if (index < 0) {
                        return positionComponent_snapshots[~index - 1];
                    } else {
                        return positionComponent_snapshots[index];
                    }
                }

                private set {
                    if (positionComponent != value) {
                        int index = positionComponent_ticks.BinarySearch(world.nextTick);
                        if (index < 0) {
                            positionComponent_ticks.Insert(~index, world.nextTick);
                            positionComponent_snapshots.Insert(~index, value);
                        } else {
                            positionComponent_ticks[index] = world.nextTick;
                            positionComponent_snapshots[index] = value;
                        }
                    }
                }
            }

        }
    }
}