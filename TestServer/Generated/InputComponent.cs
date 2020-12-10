/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace TestGame {
    partial class World {
        partial class Entity {
            public sealed partial class InputComponent : TestGame.IInputComponent {
                public readonly Entity entity;
                public readonly int constructionTick;
                public int disposalTick { get; private set; }

                public InputComponent(Entity entity) {
                    this.entity = entity;
                    constructionTick = entity.world.nextTick;
                    disposalTick = int.MaxValue;
                    entity.inputComponent = this;

                    if (entity.positionComponent != null) {
                        ((MovementSystem_Implementation)entity.world.movementSystem).controllables_collection.Add(entity);
                    }

                }

                private class Snapshot_deltaX {
                    public static readonly ConcurrentBag<Snapshot_deltaX> pool = new ConcurrentBag<Snapshot_deltaX>();
                    public static readonly Dullahan.IntDiffer differ = new Dullahan.IntDiffer();

                    // diffTicks and diffEnds form an associative array
                    public readonly Ring<int> diffTicks = new Ring<int>();
                    public readonly Ring<(int, int)> diffSpans = new Ring<(int, int)>();
                    public readonly BinaryWriter diffWriter = new BinaryWriter(new MemoryStream(), Encoding.UTF8, leaveOpen: true);
                    public System.Int32 state;

                    public Snapshot_deltaX(System.Int32 state) {
                        this.state = state;
                    }
                }

                // deltaX_ticks and deltaX_snapshots form an associative array
                private readonly Ring<int> deltaX_ticks = new Ring<int>();
                private readonly Ring<Snapshot_deltaX> deltaX_snapshots = new Ring<Snapshot_deltaX>();
                public System.Int32 deltaX {
                    get {
                        if (deltaX_ticks.Count == 0) {
                            return default;
                        }

                        int index = deltaX_ticks.BinarySearch(entity.world.previousTick);
                        if (index < 0) {
                            return deltaX_snapshots[~index - 1].state;
                        } else {
                            return deltaX_snapshots[index].state;
                        }
                    }

                    set {
                        Snapshot_deltaX snapshot;

                        int tick = entity.world.nextTick;
                        int index = deltaX_ticks.BinarySearch(tick);
                        if (index < 0) {
                            if (!Snapshot_deltaX.pool.TryTake(out snapshot)) {
                                snapshot = new Snapshot_deltaX(value);
                            }
                        } else {
                            snapshot = deltaX_snapshots[index];
                            deltaX_snapshots.RemoveAt(index);
                        }

                        snapshot.diffTicks.Clear();
                        snapshot.diffSpans.Clear();
                        snapshot.diffWriter.SetOffset(0);

                        if (index < 0) {
                            index = ~index;
                        }

                        // iterate backwards because we might terminate on finding no diffs w.r.t. the immediately preceding tick
                        int savedOffset;
                        int start = index - 1;
                        for (int i = start; i >= deltaX_ticks.Start; --i) {
                            savedOffset = snapshot.diffWriter.GetOffset();
                            if (!Snapshot_deltaX.differ.Diff(deltaX_snapshots[i].state, snapshot.state, snapshot.diffWriter)) {
                                if (i == start) {
                                    // value didn't change
                                    Snapshot_deltaX.pool.Add(snapshot);
                                    return;
                                }

                                snapshot.diffWriter.SetOffset(savedOffset);
                            }

                            snapshot.diffTicks.PushEnd(tick - deltaX_ticks[i]);
                            snapshot.diffSpans.PushEnd((savedOffset, snapshot.diffWriter.GetOffset() - savedOffset));
                        }

                        // diff with tick 0
                        savedOffset = snapshot.diffWriter.GetOffset();
                        if (!Snapshot_deltaX.differ.Diff(default, snapshot.state, snapshot.diffWriter)) {
                            snapshot.diffWriter.SetOffset(savedOffset);
                        }

                        snapshot.diffTicks.PushEnd(tick);
                        snapshot.diffSpans.PushEnd((savedOffset, snapshot.diffWriter.GetOffset() - savedOffset));

                        deltaX_ticks.Insert(index, tick);
                        deltaX_snapshots.Insert(index, snapshot);

                        // now that we have a new or modified snapshot, later snapshots need to diff with it
                        for (int i = index + 1; i < deltaX_ticks.End; ++i) {
                            savedOffset = deltaX_snapshots[i].diffWriter.GetOffset();
                            if (!Snapshot_deltaX.differ.Diff(snapshot.state, deltaX_snapshots[i].state, deltaX_snapshots[i].diffWriter)) {
                                deltaX_snapshots[i].diffWriter.SetOffset(savedOffset);
                            }

                            int diffTick = deltaX_ticks[i] - tick;
                            var diffSpan = (savedOffset, deltaX_snapshots[i].diffWriter.GetOffset() - savedOffset);
                            int diffIndex = deltaX_snapshots[i].diffTicks.BinarySearch(diffTick);
                            if (diffIndex < 0) {
                                deltaX_snapshots[i].diffTicks.Insert(~diffIndex, diffTick);
                                deltaX_snapshots[i].diffSpans.Insert(~diffIndex, diffSpan);
                            } else {
                                deltaX_snapshots[i].diffSpans[diffIndex] = diffSpan;
                            }
                        }
                    }
                }

                private class Snapshot_deltaY {
                    public static readonly ConcurrentBag<Snapshot_deltaY> pool = new ConcurrentBag<Snapshot_deltaY>();
                    public static readonly Dullahan.IntDiffer differ = new Dullahan.IntDiffer();

                    // diffTicks and diffEnds form an associative array
                    public readonly Ring<int> diffTicks = new Ring<int>();
                    public readonly Ring<(int, int)> diffSpans = new Ring<(int, int)>();
                    public readonly BinaryWriter diffWriter = new BinaryWriter(new MemoryStream(), Encoding.UTF8, leaveOpen: true);
                    public System.Int32 state;

                    public Snapshot_deltaY(System.Int32 state) {
                        this.state = state;
                    }
                }

                // deltaY_ticks and deltaY_snapshots form an associative array
                private readonly Ring<int> deltaY_ticks = new Ring<int>();
                private readonly Ring<Snapshot_deltaY> deltaY_snapshots = new Ring<Snapshot_deltaY>();
                public System.Int32 deltaY {
                    get {
                        if (deltaY_ticks.Count == 0) {
                            return default;
                        }

                        int index = deltaY_ticks.BinarySearch(entity.world.previousTick);
                        if (index < 0) {
                            return deltaY_snapshots[~index - 1].state;
                        } else {
                            return deltaY_snapshots[index].state;
                        }
                    }

                    set {
                        Snapshot_deltaY snapshot;

                        int tick = entity.world.nextTick;
                        int index = deltaY_ticks.BinarySearch(tick);
                        if (index < 0) {
                            if (!Snapshot_deltaY.pool.TryTake(out snapshot)) {
                                snapshot = new Snapshot_deltaY(value);
                            }
                        } else {
                            snapshot = deltaY_snapshots[index];
                            deltaY_snapshots.RemoveAt(index);
                        }

                        snapshot.diffTicks.Clear();
                        snapshot.diffSpans.Clear();
                        snapshot.diffWriter.SetOffset(0);

                        if (index < 0) {
                            index = ~index;
                        }

                        // iterate backwards because we might terminate on finding no diffs w.r.t. the immediately preceding tick
                        int savedOffset;
                        int start = index - 1;
                        for (int i = start; i >= deltaY_ticks.Start; --i) {
                            savedOffset = snapshot.diffWriter.GetOffset();
                            if (!Snapshot_deltaY.differ.Diff(deltaY_snapshots[i].state, snapshot.state, snapshot.diffWriter)) {
                                if (i == start) {
                                    // value didn't change
                                    Snapshot_deltaY.pool.Add(snapshot);
                                    return;
                                }

                                snapshot.diffWriter.SetOffset(savedOffset);
                            }

                            snapshot.diffTicks.PushEnd(tick - deltaY_ticks[i]);
                            snapshot.diffSpans.PushEnd((savedOffset, snapshot.diffWriter.GetOffset() - savedOffset));
                        }

                        // diff with tick 0
                        savedOffset = snapshot.diffWriter.GetOffset();
                        if (!Snapshot_deltaY.differ.Diff(default, snapshot.state, snapshot.diffWriter)) {
                            snapshot.diffWriter.SetOffset(savedOffset);
                        }

                        snapshot.diffTicks.PushEnd(tick);
                        snapshot.diffSpans.PushEnd((savedOffset, snapshot.diffWriter.GetOffset() - savedOffset));

                        deltaY_ticks.Insert(index, tick);
                        deltaY_snapshots.Insert(index, snapshot);

                        // now that we have a new or modified snapshot, later snapshots need to diff with it
                        for (int i = index + 1; i < deltaY_ticks.End; ++i) {
                            savedOffset = deltaY_snapshots[i].diffWriter.GetOffset();
                            if (!Snapshot_deltaY.differ.Diff(snapshot.state, deltaY_snapshots[i].state, deltaY_snapshots[i].diffWriter)) {
                                deltaY_snapshots[i].diffWriter.SetOffset(savedOffset);
                            }

                            int diffTick = deltaY_ticks[i] - tick;
                            var diffSpan = (savedOffset, deltaY_snapshots[i].diffWriter.GetOffset() - savedOffset);
                            int diffIndex = deltaY_snapshots[i].diffTicks.BinarySearch(diffTick);
                            if (diffIndex < 0) {
                                deltaY_snapshots[i].diffTicks.Insert(~diffIndex, diffTick);
                                deltaY_snapshots[i].diffSpans.Insert(~diffIndex, diffSpan);
                            } else {
                                deltaY_snapshots[i].diffSpans[diffIndex] = diffSpan;
                            }
                        }
                    }
                }

                private void Dispose(bool disposing) {
                    if (disposalTick == int.MaxValue) {
                        if (disposing) {
                            ((MovementSystem_Implementation)entity.world.movementSystem).controllables_collection.Remove(entity);
                        }

                        entity.inputComponent = null;
                        disposalTick = entity.world.nextTick;
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
