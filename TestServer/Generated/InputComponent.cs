/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace TestServer {
    partial class World {
        partial class Entity {
            public sealed partial class InputComponent : TestServer.IInputComponent {
                public readonly Entity entity;
                public readonly int constructionTick;

                private class Snapshot_deltaX {
                    public static readonly ConcurrentBag<Snapshot_deltaX> pool = new ConcurrentBag<Snapshot_deltaX>();
                    public static readonly Dullahan.IntDiffer differ = new Dullahan.IntDiffer();

                    public readonly Ring<(int, int)> diffs = new Ring<(int, int)>();
                    public readonly BinaryWriter diffWriter = new BinaryWriter(new MemoryStream(), Encoding.UTF8, leaveOpen: true);
                    public System.Int32 state;

                    public Snapshot_deltaX(System.Int32 state) {
                        this.state = state;
                    }
                }

                private readonly Ring<int> deltaX_ticks = new Ring<int>();
                private readonly Ring<Snapshot_deltaX> deltaX_snapshots = new Ring<Snapshot_deltaX>();
                public System.Int32 deltaX {
                    get {
                        return deltaX_snapshots.PeekEnd().state;
                    }

                    set {
                        if (deltaX_snapshots.Count > 0 && deltaX_ticks.PeekEnd() == entity.world.tick) {
                            deltaX_ticks.PopEnd();
                            Snapshot_deltaX.pool.Add(deltaX_snapshots.PopEnd());
                        }

                        if (Snapshot_deltaX.pool.TryTake(out Snapshot_deltaX snapshot)) {
                            snapshot.diffs.Clear();
                            snapshot.diffWriter.SetOffset(0);
                        } else {
                            snapshot = new Snapshot_deltaX(value);
                        }

                        int start = deltaX_ticks.Start + deltaX_ticks.Count - 1;
                        for (int i = start; i >= deltaX_ticks.Start; --i) {
                            int savedOffset = snapshot.diffWriter.GetOffset();
                            if (!Snapshot_deltaX.differ.Diff(deltaX_snapshots[i].state, snapshot.state, snapshot.diffWriter)) {
                                if (i == start) {
                                    // value didn't change
                                    Snapshot_deltaX.pool.Add(snapshot);
                                    return;
                                }

                                snapshot.diffWriter.SetOffset(savedOffset);
                            }

                            snapshot.diffs.PushEnd((savedOffset, snapshot.diffWriter.GetOffset() - savedOffset));
                        }

                        deltaX_ticks.PushEnd(entity.world.tick);
                        deltaX_snapshots.PushEnd(snapshot);
                    }
                }

                private class Snapshot_deltaY {
                    public static readonly ConcurrentBag<Snapshot_deltaY> pool = new ConcurrentBag<Snapshot_deltaY>();
                    public static readonly Dullahan.IntDiffer differ = new Dullahan.IntDiffer();

                    public readonly Ring<(int, int)> diffs = new Ring<(int, int)>();
                    public readonly BinaryWriter diffWriter = new BinaryWriter(new MemoryStream(), Encoding.UTF8, leaveOpen: true);
                    public System.Int32 state;

                    public Snapshot_deltaY(System.Int32 state) {
                        this.state = state;
                    }
                }

                private readonly Ring<int> deltaY_ticks = new Ring<int>();
                private readonly Ring<Snapshot_deltaY> deltaY_snapshots = new Ring<Snapshot_deltaY>();
                public System.Int32 deltaY {
                    get {
                        return deltaY_snapshots.PeekEnd().state;
                    }

                    set {
                        if (deltaY_snapshots.Count > 0 && deltaY_ticks.PeekEnd() == entity.world.tick) {
                            deltaY_ticks.PopEnd();
                            Snapshot_deltaY.pool.Add(deltaY_snapshots.PopEnd());
                        }

                        if (Snapshot_deltaY.pool.TryTake(out Snapshot_deltaY snapshot)) {
                            snapshot.diffs.Clear();
                            snapshot.diffWriter.SetOffset(0);
                        } else {
                            snapshot = new Snapshot_deltaY(value);
                        }

                        int start = deltaY_ticks.Start + deltaY_ticks.Count - 1;
                        for (int i = start; i >= deltaY_ticks.Start; --i) {
                            int savedOffset = snapshot.diffWriter.GetOffset();
                            if (!Snapshot_deltaY.differ.Diff(deltaY_snapshots[i].state, snapshot.state, snapshot.diffWriter)) {
                                if (i == start) {
                                    // value didn't change
                                    Snapshot_deltaY.pool.Add(snapshot);
                                    return;
                                }

                                snapshot.diffWriter.SetOffset(savedOffset);
                            }

                            snapshot.diffs.PushEnd((savedOffset, snapshot.diffWriter.GetOffset() - savedOffset));
                        }

                        deltaY_ticks.PushEnd(entity.world.tick);
                        deltaY_snapshots.PushEnd(snapshot);
                    }
                }


                public InputComponent(Entity entity) {
                    this.entity = entity;
                    constructionTick = entity.world.tick;
                    entity.inputComponent = this;
                    entity.inputComponent_disposalTick = int.MaxValue;

                    ((InputSystem_Implementation)entity.world.inputSystem).inputComponents_collection.Add(entity);

                    if (entity.positionComponent != null) {
                        ((MovementSystem_Implementation)entity.world.movementSystem).controllables_collection.Add(entity);
                    }

                }


                private void Dispose(bool disposing) {
                    if (entity.inputComponent_disposalTick == int.MaxValue) {
                        if (disposing) {
                            ((InputSystem_Implementation)entity.world.inputSystem).inputComponents_collection.Remove(entity);
                            ((MovementSystem_Implementation)entity.world.movementSystem).controllables_collection.Remove(entity);
                        }

                        entity.inputComponent = null;
                        entity.inputComponent_disposalTick = entity.world.tick;
                    }
                }

                public void Dispose() {
                    // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                    Dispose(disposing: true);
                    System.GC.SuppressFinalize(this);
                }

            }
        }
    }
}
