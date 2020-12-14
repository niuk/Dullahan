/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace TestGame {
    partial class World {
        partial class Entity {
            public sealed partial class TimeComponent : TestGame.ITimeComponent {
                public readonly Entity entity;
                public readonly int constructionTick;
                public int disposalTick { get; private set; }

                public TimeComponent(Entity entity) {
                    this.entity = entity;
                    constructionTick = entity.world.currentTick;
                    disposalTick = int.MaxValue;
                    entity.timeComponent = this;

                    if (((MovementSystem_Implementation)entity.world.movementSystem).timeComponent_entity != null) {
                        throw new InvalidOperationException("Multiple timeComponent singletons for TestGame.MovementSystem!");
                    }

                    ((MovementSystem_Implementation)entity.world.movementSystem).timeComponent_entity = entity;

                }

                private class Snapshot_deltaTime {
                    public static readonly ConcurrentBag<Snapshot_deltaTime> pool = new ConcurrentBag<Snapshot_deltaTime>();
                    public static readonly Dullahan.DoubleDiffer differ = new Dullahan.DoubleDiffer();

                    // diffTicks and diffEnds form an associative array
                    public readonly Ring<int> diffTicks = new Ring<int>();
                    public readonly Ring<(int, int)> diffSpans = new Ring<(int, int)>();
                    public readonly BinaryWriter diffWriter = new BinaryWriter(new MemoryStream(), Encoding.UTF8, leaveOpen: true);
                    public System.Double state = default;
                }

                // deltaTime_ticks and deltaTime_snapshots form an associative array
                private readonly Ring<int> deltaTime_ticks = new Ring<int> { 0 };
                private readonly Ring<Snapshot_deltaTime> deltaTime_snapshots = new Ring<Snapshot_deltaTime> {new Snapshot_deltaTime() };
                public System.Double deltaTime {
                    get {
                        if (deltaTime_ticks.Count == 0) {
                            return default;
                        }

                        int index = deltaTime_ticks.BinarySearch(entity.world.currentTick);
                        if (index < 0) {
                            return deltaTime_snapshots[~index - 1].state;
                        } else {
                            return deltaTime_snapshots[index].state;
                        }
                    }

                    set {
                        Snapshot_deltaTime snapshot;

                        int tick = entity.world.currentTick;
                        int index = deltaTime_ticks.BinarySearch(tick);
                        if (index < 0) {
                            if (!Snapshot_deltaTime.pool.TryTake(out snapshot)) {
                                snapshot = new Snapshot_deltaTime();
                            }
                        } else {
                            snapshot = deltaTime_snapshots[index];
                            deltaTime_snapshots.RemoveAt(index);
                            deltaTime_ticks.RemoveAt(index);
                        }

                        snapshot.diffTicks.Clear();
                        snapshot.diffSpans.Clear();
                        snapshot.diffWriter.SetOffset(0);
                        snapshot.state = value;

                        if (index < 0) {
                            index = ~index;
                        }

                        // iterate backwards because we might terminate on finding no diffs w.r.t. the immediately preceding tick
                        int start = index - 1;
                        for (int i = start; i >= deltaTime_ticks.Start; --i) {
                            int savedOffset = snapshot.diffWriter.GetOffset();
                            if (!Snapshot_deltaTime.differ.Diff(deltaTime_snapshots[i].state, snapshot.state, snapshot.diffWriter)) {
                                if (i == start) {
                                    // value didn't change
                                    Snapshot_deltaTime.pool.Add(snapshot);
                                    return;
                                }

                                snapshot.diffWriter.SetOffset(savedOffset);
                            }

                            snapshot.diffTicks.PushEnd(tick - deltaTime_ticks[i]);
                            snapshot.diffSpans.PushEnd((savedOffset, snapshot.diffWriter.GetOffset() - savedOffset));
                        }

                        deltaTime_ticks.Insert(index, tick);
                        deltaTime_snapshots.Insert(index, snapshot);

                        // now that we have a new or modified snapshot, later snapshots need to diff with it
                        for (int i = index + 1; i < deltaTime_ticks.End; ++i) {
                            int savedOffset = deltaTime_snapshots[i].diffWriter.GetOffset();
                            if (!Snapshot_deltaTime.differ.Diff(snapshot.state, deltaTime_snapshots[i].state, deltaTime_snapshots[i].diffWriter)) {
                                deltaTime_snapshots[i].diffWriter.SetOffset(savedOffset);
                            }

                            int diffTick = deltaTime_ticks[i] - tick;
                            var diffSpan = (savedOffset, deltaTime_snapshots[i].diffWriter.GetOffset() - savedOffset);
                            int diffIndex = deltaTime_snapshots[i].diffTicks.BinarySearch(diffTick);
                            if (diffIndex < 0) {
                                deltaTime_snapshots[i].diffTicks.Insert(~diffIndex, diffTick);
                                deltaTime_snapshots[i].diffSpans.Insert(~diffIndex, diffSpan);
                            } else {
                                //deltaTime_snapshots[i].diffTick[diffIndex] = diffTick; // diffTick was found; no need to change it
                                deltaTime_snapshots[i].diffSpans[diffIndex] = diffSpan;
                            }
                        }
                    }
                }

                private void Dispose(bool disposing) {
                    if (disposalTick == int.MaxValue) {
                        if (disposing) {
                            ((MovementSystem_Implementation)entity.world.movementSystem).timeComponent_entity = null;
                        }

                        entity.timeComponent = null;
                        disposalTick = entity.world.currentTick;
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
