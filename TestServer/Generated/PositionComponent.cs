/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace TestServer {
    partial class World {
        partial class Entity {
            public sealed partial class PositionComponent : TestServer.IPositionComponent {
                public readonly Entity entity;
                public readonly int constructionTick;

                private class Snapshot_x {
                    public static readonly ConcurrentBag<Snapshot_x> pool = new ConcurrentBag<Snapshot_x>();
                    public static readonly Dullahan.IntDiffer differ = new Dullahan.IntDiffer();

                    public readonly Ring<(int, int)> diffs = new Ring<(int, int)>();
                    public readonly BinaryWriter diffWriter = new BinaryWriter(new MemoryStream(), Encoding.UTF8, leaveOpen: true);
                    public System.Int32 state;

                    public Snapshot_x(System.Int32 state) {
                        this.state = state;
                    }
                }

                private readonly Ring<int> x_ticks = new Ring<int>();
                private readonly Ring<Snapshot_x> x_snapshots = new Ring<Snapshot_x>();
                public System.Int32 x {
                    get {
                        return x_snapshots.PeekEnd().state;
                    }

                    set {
                        if (x_snapshots.Count > 0 && x_ticks.PeekEnd() == entity.world.tick) {
                            x_ticks.PopEnd();
                            Snapshot_x.pool.Add(x_snapshots.PopEnd());
                        }

                        if (!Snapshot_x.pool.TryTake(out Snapshot_x snapshot)) {
                            snapshot = new Snapshot_x(value);
                            snapshot.diffs.Clear();
                            snapshot.diffWriter.SetOffset(0);
                        }

                        int start = x_ticks.Start + x_ticks.Count - 1;
                        for (int i = start; i >= x_ticks.Start; --i) {
                            int savedOffset = snapshot.diffWriter.GetOffset();
                            if (!Snapshot_x.differ.Diff(x_snapshots[i].state, snapshot.state, snapshot.diffWriter)) {
                                if (i == start) {
                                    // value didn't change
                                    Snapshot_x.pool.Add(snapshot);
                                    return;
                                }

                                snapshot.diffWriter.SetOffset(savedOffset);
                            }

                            snapshot.diffs.PushEnd((savedOffset, snapshot.diffWriter.GetOffset() - savedOffset));
                        }

                        x_ticks.PushEnd(entity.world.tick);
                        x_snapshots.PushEnd(snapshot);
                    }
                }

                private class Snapshot_y {
                    public static readonly ConcurrentBag<Snapshot_y> pool = new ConcurrentBag<Snapshot_y>();
                    public static readonly Dullahan.IntDiffer differ = new Dullahan.IntDiffer();

                    public readonly Ring<(int, int)> diffs = new Ring<(int, int)>();
                    public readonly BinaryWriter diffWriter = new BinaryWriter(new MemoryStream(), Encoding.UTF8, leaveOpen: true);
                    public System.Int32 state;

                    public Snapshot_y(System.Int32 state) {
                        this.state = state;
                    }
                }

                private readonly Ring<int> y_ticks = new Ring<int>();
                private readonly Ring<Snapshot_y> y_snapshots = new Ring<Snapshot_y>();
                public System.Int32 y {
                    get {
                        return y_snapshots.PeekEnd().state;
                    }

                    set {
                        if (y_snapshots.Count > 0 && y_ticks.PeekEnd() == entity.world.tick) {
                            y_ticks.PopEnd();
                            Snapshot_y.pool.Add(y_snapshots.PopEnd());
                        }

                        if (!Snapshot_y.pool.TryTake(out Snapshot_y snapshot)) {
                            snapshot = new Snapshot_y(value);
                            snapshot.diffs.Clear();
                            snapshot.diffWriter.SetOffset(0);
                        }

                        int start = y_ticks.Start + y_ticks.Count - 1;
                        for (int i = start; i >= y_ticks.Start; --i) {
                            int savedOffset = snapshot.diffWriter.GetOffset();
                            if (!Snapshot_y.differ.Diff(y_snapshots[i].state, snapshot.state, snapshot.diffWriter)) {
                                if (i == start) {
                                    // value didn't change
                                    Snapshot_y.pool.Add(snapshot);
                                    return;
                                }

                                snapshot.diffWriter.SetOffset(savedOffset);
                            }

                            snapshot.diffs.PushEnd((savedOffset, snapshot.diffWriter.GetOffset() - savedOffset));
                        }

                        y_ticks.PushEnd(entity.world.tick);
                        y_snapshots.PushEnd(snapshot);
                    }
                }


                public PositionComponent(Entity entity) {
                    this.entity = entity;
                    constructionTick = entity.world.tick;
                    entity.positionComponent = this;
                    entity.positionComponent_disposalTick = int.MaxValue;

                    if (entity.inputComponent != null) {
                        ((MovementSystem_Implementation)entity.world.movementSystem).controllables_collection.Add(entity);
                    }

                }


                private void Dispose(bool disposing) {
                    if (entity.positionComponent_disposalTick == int.MaxValue) {
                        if (disposing) {
                            ((MovementSystem_Implementation)entity.world.movementSystem).controllables_collection.Remove(entity);
                        }

                        entity.positionComponent = null;
                        entity.positionComponent_disposalTick = entity.world.tick;
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
