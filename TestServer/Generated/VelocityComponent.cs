/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace TestGame {
    partial class World {
        partial class Entity {
            public sealed partial class VelocityComponent : TestGame.IVelocityComponent {
                public readonly Entity entity;
                public readonly int constructionTick;
                public int disposalTick { get; private set; }

                public VelocityComponent(Entity entity) {
                    this.entity = entity;
                    constructionTick = entity.world.currentTick;
                    disposalTick = int.MaxValue;
                    entity.velocityComponent = this;

                    if (entity.positionComponent != null) {
                        ((MovementSystem_Implementation)entity.world.movementSystem).controllables_entities.Add(entity);
                    }

                }

                private class Snapshot_deltaX {
                    public static readonly ConcurrentBag<Snapshot_deltaX> pool = new ConcurrentBag<Snapshot_deltaX>();
                    public static readonly Dullahan.FloatDiffer differ = new Dullahan.FloatDiffer();

                    // diffTicks and diffEnds form an associative array
                    public readonly Ring<int> diffTicks = new Ring<int>();
                    public readonly Ring<(int, int)> diffSpans = new Ring<(int, int)>();
                    public readonly BinaryWriter diffWriter = new BinaryWriter(new MemoryStream(), Encoding.UTF8, leaveOpen: true);
                    public System.Single state = default;
                }

                // deltaX_ticks and deltaX_snapshots form an associative array
                private readonly Ring<int> deltaX_ticks = new Ring<int> { 0 };
                private readonly Ring<Snapshot_deltaX> deltaX_snapshots = new Ring<Snapshot_deltaX> {new Snapshot_deltaX() };
                public System.Single deltaX {
                    get {
                        if (deltaX_ticks.Count == 0) {
                            return default;
                        }

                        int index = deltaX_ticks.BinarySearch(entity.world.currentTick);
                        if (index < 0) {
                            return deltaX_snapshots[~index - 1].state;
                        } else {
                            return deltaX_snapshots[index].state;
                        }
                    }

                    set {
                        Snapshot_deltaX snapshot;

                        int tick = entity.world.currentTick;
                        int index = deltaX_ticks.BinarySearch(tick);
                        if (index < 0) {
                            if (!Snapshot_deltaX.pool.TryTake(out snapshot)) {
                                snapshot = new Snapshot_deltaX();
                            }
                        } else {
                            snapshot = deltaX_snapshots[index];
                            deltaX_snapshots.RemoveAt(index);
                            deltaX_ticks.RemoveAt(index);
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

                            snapshot.diffTicks.PushEnd(tick - deltaX_ticks[i]);
                            snapshot.diffSpans.PushEnd((savedOffset, snapshot.diffWriter.GetOffset() - savedOffset));
                        }

                        deltaX_ticks.Insert(index, tick);
                        deltaX_snapshots.Insert(index, snapshot);

                        // now that we have a new or modified snapshot, later snapshots need to diff with it
                        for (int i = index + 1; i < deltaX_ticks.End; ++i) {
                            int savedOffset = deltaX_snapshots[i].diffWriter.GetOffset();
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
                                //deltaX_snapshots[i].diffTick[diffIndex] = diffTick; // diffTick was found; no need to change it
                                deltaX_snapshots[i].diffSpans[diffIndex] = diffSpan;
                            }
                        }
                    }
                }

                private class Snapshot_deltaY {
                    public static readonly ConcurrentBag<Snapshot_deltaY> pool = new ConcurrentBag<Snapshot_deltaY>();
                    public static readonly Dullahan.FloatDiffer differ = new Dullahan.FloatDiffer();

                    // diffTicks and diffEnds form an associative array
                    public readonly Ring<int> diffTicks = new Ring<int>();
                    public readonly Ring<(int, int)> diffSpans = new Ring<(int, int)>();
                    public readonly BinaryWriter diffWriter = new BinaryWriter(new MemoryStream(), Encoding.UTF8, leaveOpen: true);
                    public System.Single state = default;
                }

                // deltaY_ticks and deltaY_snapshots form an associative array
                private readonly Ring<int> deltaY_ticks = new Ring<int> { 0 };
                private readonly Ring<Snapshot_deltaY> deltaY_snapshots = new Ring<Snapshot_deltaY> {new Snapshot_deltaY() };
                public System.Single deltaY {
                    get {
                        if (deltaY_ticks.Count == 0) {
                            return default;
                        }

                        int index = deltaY_ticks.BinarySearch(entity.world.currentTick);
                        if (index < 0) {
                            return deltaY_snapshots[~index - 1].state;
                        } else {
                            return deltaY_snapshots[index].state;
                        }
                    }

                    set {
                        Snapshot_deltaY snapshot;

                        int tick = entity.world.currentTick;
                        int index = deltaY_ticks.BinarySearch(tick);
                        if (index < 0) {
                            if (!Snapshot_deltaY.pool.TryTake(out snapshot)) {
                                snapshot = new Snapshot_deltaY();
                            }
                        } else {
                            snapshot = deltaY_snapshots[index];
                            deltaY_snapshots.RemoveAt(index);
                            deltaY_ticks.RemoveAt(index);
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

                            snapshot.diffTicks.PushEnd(tick - deltaY_ticks[i]);
                            snapshot.diffSpans.PushEnd((savedOffset, snapshot.diffWriter.GetOffset() - savedOffset));
                        }

                        deltaY_ticks.Insert(index, tick);
                        deltaY_snapshots.Insert(index, snapshot);

                        // now that we have a new or modified snapshot, later snapshots need to diff with it
                        for (int i = index + 1; i < deltaY_ticks.End; ++i) {
                            int savedOffset = deltaY_snapshots[i].diffWriter.GetOffset();
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
                                //deltaY_snapshots[i].diffTick[diffIndex] = diffTick; // diffTick was found; no need to change it
                                deltaY_snapshots[i].diffSpans[diffIndex] = diffSpan;
                            }
                        }
                    }
                }

                private class Snapshot_speed {
                    public static readonly ConcurrentBag<Snapshot_speed> pool = new ConcurrentBag<Snapshot_speed>();
                    public static readonly Dullahan.FloatDiffer differ = new Dullahan.FloatDiffer();

                    // diffTicks and diffEnds form an associative array
                    public readonly Ring<int> diffTicks = new Ring<int>();
                    public readonly Ring<(int, int)> diffSpans = new Ring<(int, int)>();
                    public readonly BinaryWriter diffWriter = new BinaryWriter(new MemoryStream(), Encoding.UTF8, leaveOpen: true);
                    public System.Single state = default;
                }

                // speed_ticks and speed_snapshots form an associative array
                private readonly Ring<int> speed_ticks = new Ring<int> { 0 };
                private readonly Ring<Snapshot_speed> speed_snapshots = new Ring<Snapshot_speed> {new Snapshot_speed() };
                public System.Single speed {
                    get {
                        if (speed_ticks.Count == 0) {
                            return default;
                        }

                        int index = speed_ticks.BinarySearch(entity.world.currentTick);
                        if (index < 0) {
                            return speed_snapshots[~index - 1].state;
                        } else {
                            return speed_snapshots[index].state;
                        }
                    }

                    set {
                        Snapshot_speed snapshot;

                        int tick = entity.world.currentTick;
                        int index = speed_ticks.BinarySearch(tick);
                        if (index < 0) {
                            if (!Snapshot_speed.pool.TryTake(out snapshot)) {
                                snapshot = new Snapshot_speed();
                            }
                        } else {
                            snapshot = speed_snapshots[index];
                            speed_snapshots.RemoveAt(index);
                            speed_ticks.RemoveAt(index);
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
                        for (int i = start; i >= speed_ticks.Start; --i) {
                            int savedOffset = snapshot.diffWriter.GetOffset();
                            if (!Snapshot_speed.differ.Diff(speed_snapshots[i].state, snapshot.state, snapshot.diffWriter)) {
                                if (i == start) {
                                    // value didn't change
                                    Snapshot_speed.pool.Add(snapshot);
                                    return;
                                }

                                snapshot.diffWriter.SetOffset(savedOffset);
                            }

                            snapshot.diffTicks.PushEnd(tick - speed_ticks[i]);
                            snapshot.diffSpans.PushEnd((savedOffset, snapshot.diffWriter.GetOffset() - savedOffset));
                        }

                        speed_ticks.Insert(index, tick);
                        speed_snapshots.Insert(index, snapshot);

                        // now that we have a new or modified snapshot, later snapshots need to diff with it
                        for (int i = index + 1; i < speed_ticks.End; ++i) {
                            int savedOffset = speed_snapshots[i].diffWriter.GetOffset();
                            if (!Snapshot_speed.differ.Diff(snapshot.state, speed_snapshots[i].state, speed_snapshots[i].diffWriter)) {
                                speed_snapshots[i].diffWriter.SetOffset(savedOffset);
                            }

                            int diffTick = speed_ticks[i] - tick;
                            var diffSpan = (savedOffset, speed_snapshots[i].diffWriter.GetOffset() - savedOffset);
                            int diffIndex = speed_snapshots[i].diffTicks.BinarySearch(diffTick);
                            if (diffIndex < 0) {
                                speed_snapshots[i].diffTicks.Insert(~diffIndex, diffTick);
                                speed_snapshots[i].diffSpans.Insert(~diffIndex, diffSpan);
                            } else {
                                //speed_snapshots[i].diffTick[diffIndex] = diffTick; // diffTick was found; no need to change it
                                speed_snapshots[i].diffSpans[diffIndex] = diffSpan;
                            }
                        }
                    }
                }

                private void Dispose(bool disposing) {
                    if (disposalTick == int.MaxValue) {
                        if (disposing) {
                            ((MovementSystem_Implementation)entity.world.movementSystem).controllables_entities.Remove(entity);
                        }

                        entity.velocityComponent = null;
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
