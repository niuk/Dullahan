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
                public int disposalTick { get; private set; }

                public PositionComponent(Entity entity) {
                    this.entity = entity;
                    constructionTick = entity.world.tick;
                    disposalTick = int.MaxValue;
                    entity.positionComponent = this;

                    if (entity.inputComponent != null) {
                        ((MovementSystem_Implementation)entity.world.movementSystem).controllables_collection.Add(entity);
                    }

                }

                private class Snapshot_x {
                    public static readonly ConcurrentBag<Snapshot_x> pool = new ConcurrentBag<Snapshot_x>();
                    public static readonly Dullahan.IntDiffer differ = new Dullahan.IntDiffer();

                    // diffTicks and diffEnds form an associative array
                    public readonly Ring<int> diffTicks = new Ring<int>();
                    public readonly Ring<int> diffOffsets = new Ring<int>();
                    public readonly BinaryWriter diffWriter = new BinaryWriter(new MemoryStream(), Encoding.UTF8, leaveOpen: true);
                    public System.Int32 state;

                    public Snapshot_x(System.Int32 state) {
                        this.state = state;
                    }
                }

                // x_ticks and x_snapshots form an associative array
                private readonly Ring<int> x_ticks = new Ring<int>();
                private readonly Ring<Snapshot_x> x_snapshots = new Ring<Snapshot_x>();
                public System.Int32 x {
                    get {
                        return x_snapshots.PeekEnd().state;
                    }

                    set {
                        int tick = entity.world.tick;
                        if (x_snapshots.Count > 0 && x_ticks.PeekEnd() == tick) {
                            x_ticks.PopEnd();
                            Snapshot_x.pool.Add(x_snapshots.PopEnd());
                        }

                        if (Snapshot_x.pool.TryTake(out Snapshot_x snapshot)) {
                            snapshot.diffTicks.Clear();
                            snapshot.diffOffsets.Clear();
                            snapshot.diffWriter.SetOffset(0);
                        } else {
                            snapshot = new Snapshot_x(value);
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

                            snapshot.diffTicks.PushEnd(tick - x_ticks[i]);
                            snapshot.diffOffsets.PushEnd(snapshot.diffWriter.GetOffset());
                        }

                        x_ticks.PushEnd(tick);
                        x_snapshots.PushEnd(snapshot);
                    }
                }

                private class Snapshot_y {
                    public static readonly ConcurrentBag<Snapshot_y> pool = new ConcurrentBag<Snapshot_y>();
                    public static readonly Dullahan.IntDiffer differ = new Dullahan.IntDiffer();

                    // diffTicks and diffEnds form an associative array
                    public readonly Ring<int> diffTicks = new Ring<int>();
                    public readonly Ring<int> diffOffsets = new Ring<int>();
                    public readonly BinaryWriter diffWriter = new BinaryWriter(new MemoryStream(), Encoding.UTF8, leaveOpen: true);
                    public System.Int32 state;

                    public Snapshot_y(System.Int32 state) {
                        this.state = state;
                    }
                }

                // y_ticks and y_snapshots form an associative array
                private readonly Ring<int> y_ticks = new Ring<int>();
                private readonly Ring<Snapshot_y> y_snapshots = new Ring<Snapshot_y>();
                public System.Int32 y {
                    get {
                        return y_snapshots.PeekEnd().state;
                    }

                    set {
                        int tick = entity.world.tick;
                        if (y_snapshots.Count > 0 && y_ticks.PeekEnd() == tick) {
                            y_ticks.PopEnd();
                            Snapshot_y.pool.Add(y_snapshots.PopEnd());
                        }

                        if (Snapshot_y.pool.TryTake(out Snapshot_y snapshot)) {
                            snapshot.diffTicks.Clear();
                            snapshot.diffOffsets.Clear();
                            snapshot.diffWriter.SetOffset(0);
                        } else {
                            snapshot = new Snapshot_y(value);
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

                            snapshot.diffTicks.PushEnd(tick - y_ticks[i]);
                            snapshot.diffOffsets.PushEnd(snapshot.diffWriter.GetOffset());
                        }

                        y_ticks.PushEnd(tick);
                        y_snapshots.PushEnd(snapshot);
                    }
                }

                private void Dispose(bool disposing) {
                    if (entity.positionComponent_disposalTick == int.MaxValue) {
                        if (disposing) {
                            ((MovementSystem_Implementation)entity.world.movementSystem).controllables_collection.Remove(entity);
                        }

                        entity.positionComponent = null;
                        disposalTick = entity.world.tick;
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
