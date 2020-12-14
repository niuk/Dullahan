/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;

namespace TestGame {
    partial class World {
        public sealed partial class Entity : IDisposable {
            public readonly int id;
            public readonly World world;
            public readonly int constructionTick;
            public int disposalTick { get; private set; }

            public Entity(World world) : this(world, world.nextEntityId++) { }

            public Entity(World world, int id) {
                this.id = id;
                this.world = world;
                world.entitiesById.Add(id, this);
                constructionTick = world.currentTick;
                disposalTick = int.MaxValue;
            }

            private Entity() { }

            private void Dispose(bool disposing) {
                if (disposalTick == int.MaxValue) {
                    if (disposing) {
                        consoleBufferComponent?.Dispose();

                        positionComponent?.Dispose();

                        timeComponent?.Dispose();

                        velocityComponent?.Dispose();

                        viewComponent?.Dispose();

                    }

                    disposalTick = world.currentTick;
                }
            }

            public void Dispose() {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }

            private readonly Ring<int> consoleBufferComponent_ticks = new Ring<int> { 0 };
            private readonly Ring<ConsoleBufferComponent> consoleBufferComponent_snapshots = new Ring<ConsoleBufferComponent> { null };
            public ConsoleBufferComponent consoleBufferComponent {
                get {
                    if (consoleBufferComponent_ticks.Count == 0) {
                        return default;
                    }

                    int index = consoleBufferComponent_ticks.BinarySearch(world.currentTick);
                    if (index < 0) {
                        index = ~index - 1;
                    }

                    if (index < 0) {
                        return default;
                    }

                    return consoleBufferComponent_snapshots[index];
                }

                private set {
                    if (consoleBufferComponent != value) {
                        int index = consoleBufferComponent_ticks.BinarySearch(world.currentTick);
                        if (index < 0) {
                            consoleBufferComponent_ticks.Insert(~index, world.currentTick);
                            consoleBufferComponent_snapshots.Insert(~index, value);
                        } else {
                            consoleBufferComponent_ticks[index] = world.currentTick;
                            consoleBufferComponent_snapshots[index] = value;
                        }
                    }
                }
            }

            private readonly Ring<int> positionComponent_ticks = new Ring<int> { 0 };
            private readonly Ring<PositionComponent> positionComponent_snapshots = new Ring<PositionComponent> { null };
            public PositionComponent positionComponent {
                get {
                    if (positionComponent_ticks.Count == 0) {
                        return default;
                    }

                    int index = positionComponent_ticks.BinarySearch(world.currentTick);
                    if (index < 0) {
                        index = ~index - 1;
                    }

                    if (index < 0) {
                        return default;
                    }

                    return positionComponent_snapshots[index];
                }

                private set {
                    if (positionComponent != value) {
                        int index = positionComponent_ticks.BinarySearch(world.currentTick);
                        if (index < 0) {
                            positionComponent_ticks.Insert(~index, world.currentTick);
                            positionComponent_snapshots.Insert(~index, value);
                        } else {
                            positionComponent_ticks[index] = world.currentTick;
                            positionComponent_snapshots[index] = value;
                        }
                    }
                }
            }

            private readonly Ring<int> timeComponent_ticks = new Ring<int> { 0 };
            private readonly Ring<TimeComponent> timeComponent_snapshots = new Ring<TimeComponent> { null };
            public TimeComponent timeComponent {
                get {
                    if (timeComponent_ticks.Count == 0) {
                        return default;
                    }

                    int index = timeComponent_ticks.BinarySearch(world.currentTick);
                    if (index < 0) {
                        index = ~index - 1;
                    }

                    if (index < 0) {
                        return default;
                    }

                    return timeComponent_snapshots[index];
                }

                private set {
                    if (timeComponent != value) {
                        int index = timeComponent_ticks.BinarySearch(world.currentTick);
                        if (index < 0) {
                            timeComponent_ticks.Insert(~index, world.currentTick);
                            timeComponent_snapshots.Insert(~index, value);
                        } else {
                            timeComponent_ticks[index] = world.currentTick;
                            timeComponent_snapshots[index] = value;
                        }
                    }
                }
            }

            private readonly Ring<int> velocityComponent_ticks = new Ring<int> { 0 };
            private readonly Ring<VelocityComponent> velocityComponent_snapshots = new Ring<VelocityComponent> { null };
            public VelocityComponent velocityComponent {
                get {
                    if (velocityComponent_ticks.Count == 0) {
                        return default;
                    }

                    int index = velocityComponent_ticks.BinarySearch(world.currentTick);
                    if (index < 0) {
                        index = ~index - 1;
                    }

                    if (index < 0) {
                        return default;
                    }

                    return velocityComponent_snapshots[index];
                }

                private set {
                    if (velocityComponent != value) {
                        int index = velocityComponent_ticks.BinarySearch(world.currentTick);
                        if (index < 0) {
                            velocityComponent_ticks.Insert(~index, world.currentTick);
                            velocityComponent_snapshots.Insert(~index, value);
                        } else {
                            velocityComponent_ticks[index] = world.currentTick;
                            velocityComponent_snapshots[index] = value;
                        }
                    }
                }
            }

            private readonly Ring<int> viewComponent_ticks = new Ring<int> { 0 };
            private readonly Ring<ViewComponent> viewComponent_snapshots = new Ring<ViewComponent> { null };
            public ViewComponent viewComponent {
                get {
                    if (viewComponent_ticks.Count == 0) {
                        return default;
                    }

                    int index = viewComponent_ticks.BinarySearch(world.currentTick);
                    if (index < 0) {
                        index = ~index - 1;
                    }

                    if (index < 0) {
                        return default;
                    }

                    return viewComponent_snapshots[index];
                }

                private set {
                    if (viewComponent != value) {
                        int index = viewComponent_ticks.BinarySearch(world.currentTick);
                        if (index < 0) {
                            viewComponent_ticks.Insert(~index, world.currentTick);
                            viewComponent_snapshots.Insert(~index, value);
                        } else {
                            viewComponent_ticks[index] = world.currentTick;
                            viewComponent_snapshots[index] = value;
                        }
                    }
                }
            }

        }
    }
}