/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace TestGame {
    partial class World {
        partial class Entity {
            public sealed partial class PositionComponent : TestGame.IPositionComponent {
                public readonly Entity entity;
                public readonly int constructionTick;
                public int disposalTick { get; private set; }

                public PositionComponent(Entity entity) {
                    this.entity = entity;
                    constructionTick = entity.world.currentTick;
                    disposalTick = int.MaxValue;
                    entity.positionComponent = this;

                    if (entity.velocityComponent != null) {
                        ((MovementSystem_Implementation)entity.world.movementSystem).controllables_entities.Add(entity);
                    }

                    if (entity.viewComponent != null) {
                        ((VisualizationSystem_Implementation)entity.world.visualizationSystem).avatars_entities.Add(entity);
                    }

                }

                private class Snapshot_x {
                    public static readonly ConcurrentBag<Snapshot_x> pool = new ConcurrentBag<Snapshot_x>();
                    public static readonly Dullahan.FloatDiffer differ = new Dullahan.FloatDiffer();

                    // diffTicks and diffEnds form an associative array
                    public readonly Ring<int> diffTicks = new Ring<int>();
                    public readonly Ring<(int, int)> diffSpans = new Ring<(int, int)>();
                    public readonly BinaryWriter diffWriter = new BinaryWriter(new MemoryStream(), Encoding.UTF8, leaveOpen: true);
                    public System.Single state = default;
                }

                // x_ticks and x_snapshots form an associative array
                private readonly Ring<int> x_ticks = new Ring<int> { 0 };
                private readonly Ring<Snapshot_x> x_snapshots = new Ring<Snapshot_x> {new Snapshot_x() };
                public System.Single x {
                    get {
                        if (x_ticks.Count == 0) {
                            return default;
                        }

                        int index = x_ticks.BinarySearch(entity.world.currentTick);
                        if (index < 0) {
                            return x_snapshots[~index - 1].state;
                        } else {
                            return x_snapshots[index].state;
                        }
                    }

                    set {
                        Snapshot_x snapshot;

                        int tick = entity.world.currentTick;
                        int index = x_ticks.BinarySearch(tick);
                        if (index < 0) {
                            if (!Snapshot_x.pool.TryTake(out snapshot)) {
                                snapshot = new Snapshot_x();
                            }
                        } else {
                            snapshot = x_snapshots[index];
                            x_snapshots.RemoveAt(index);
                            x_ticks.RemoveAt(index);
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
                            snapshot.diffSpans.PushEnd((savedOffset, snapshot.diffWriter.GetOffset() - savedOffset));
                        }

                        x_ticks.Insert(index, tick);
                        x_snapshots.Insert(index, snapshot);

                        // now that we have a new or modified snapshot, later snapshots need to diff with it
                        for (int i = index + 1; i < x_ticks.End; ++i) {
                            int savedOffset = x_snapshots[i].diffWriter.GetOffset();
                            if (!Snapshot_x.differ.Diff(snapshot.state, x_snapshots[i].state, x_snapshots[i].diffWriter)) {
                                x_snapshots[i].diffWriter.SetOffset(savedOffset);
                            }

                            int diffTick = x_ticks[i] - tick;
                            var diffSpan = (savedOffset, x_snapshots[i].diffWriter.GetOffset() - savedOffset);
                            int diffIndex = x_snapshots[i].diffTicks.BinarySearch(diffTick);
                            if (diffIndex < 0) {
                                x_snapshots[i].diffTicks.Insert(~diffIndex, diffTick);
                                x_snapshots[i].diffSpans.Insert(~diffIndex, diffSpan);
                            } else {
                                //x_snapshots[i].diffTick[diffIndex] = diffTick; // diffTick was found; no need to change it
                                x_snapshots[i].diffSpans[diffIndex] = diffSpan;
                            }
                        }
                    }
                }

                private class Snapshot_y {
                    public static readonly ConcurrentBag<Snapshot_y> pool = new ConcurrentBag<Snapshot_y>();
                    public static readonly Dullahan.FloatDiffer differ = new Dullahan.FloatDiffer();

                    // diffTicks and diffEnds form an associative array
                    public readonly Ring<int> diffTicks = new Ring<int>();
                    public readonly Ring<(int, int)> diffSpans = new Ring<(int, int)>();
                    public readonly BinaryWriter diffWriter = new BinaryWriter(new MemoryStream(), Encoding.UTF8, leaveOpen: true);
                    public System.Single state = default;
                }

                // y_ticks and y_snapshots form an associative array
                private readonly Ring<int> y_ticks = new Ring<int> { 0 };
                private readonly Ring<Snapshot_y> y_snapshots = new Ring<Snapshot_y> {new Snapshot_y() };
                public System.Single y {
                    get {
                        if (y_ticks.Count == 0) {
                            return default;
                        }

                        int index = y_ticks.BinarySearch(entity.world.currentTick);
                        if (index < 0) {
                            return y_snapshots[~index - 1].state;
                        } else {
                            return y_snapshots[index].state;
                        }
                    }

                    set {
                        Snapshot_y snapshot;

                        int tick = entity.world.currentTick;
                        int index = y_ticks.BinarySearch(tick);
                        if (index < 0) {
                            if (!Snapshot_y.pool.TryTake(out snapshot)) {
                                snapshot = new Snapshot_y();
                            }
                        } else {
                            snapshot = y_snapshots[index];
                            y_snapshots.RemoveAt(index);
                            y_ticks.RemoveAt(index);
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
                            snapshot.diffSpans.PushEnd((savedOffset, snapshot.diffWriter.GetOffset() - savedOffset));
                        }

                        y_ticks.Insert(index, tick);
                        y_snapshots.Insert(index, snapshot);

                        // now that we have a new or modified snapshot, later snapshots need to diff with it
                        for (int i = index + 1; i < y_ticks.End; ++i) {
                            int savedOffset = y_snapshots[i].diffWriter.GetOffset();
                            if (!Snapshot_y.differ.Diff(snapshot.state, y_snapshots[i].state, y_snapshots[i].diffWriter)) {
                                y_snapshots[i].diffWriter.SetOffset(savedOffset);
                            }

                            int diffTick = y_ticks[i] - tick;
                            var diffSpan = (savedOffset, y_snapshots[i].diffWriter.GetOffset() - savedOffset);
                            int diffIndex = y_snapshots[i].diffTicks.BinarySearch(diffTick);
                            if (diffIndex < 0) {
                                y_snapshots[i].diffTicks.Insert(~diffIndex, diffTick);
                                y_snapshots[i].diffSpans.Insert(~diffIndex, diffSpan);
                            } else {
                                //y_snapshots[i].diffTick[diffIndex] = diffTick; // diffTick was found; no need to change it
                                y_snapshots[i].diffSpans[diffIndex] = diffSpan;
                            }
                        }
                    }
                }

                private void Dispose(bool disposing) {
                    if (disposalTick == int.MaxValue) {
                        if (disposing) {
                            ((MovementSystem_Implementation)entity.world.movementSystem).controllables_entities.Remove(entity);
                            ((VisualizationSystem_Implementation)entity.world.visualizationSystem).avatars_entities.Remove(entity);
                        }

                        entity.positionComponent = null;
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
