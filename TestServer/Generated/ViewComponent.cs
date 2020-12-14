/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace TestGame {
    partial class World {
        partial class Entity {
            public sealed partial class ViewComponent : TestGame.IViewComponent {
                public readonly Entity entity;
                public readonly int constructionTick;
                public int disposalTick { get; private set; }

                public ViewComponent(Entity entity) {
                    this.entity = entity;
                    constructionTick = entity.world.currentTick;
                    disposalTick = int.MaxValue;
                    entity.viewComponent = this;

                    if (entity.positionComponent != null) {
                        ((VisualizationSystem_Implementation)entity.world.visualizationSystem).avatars_entities.Add(entity);
                    }

                }

                private class Snapshot_avatar {
                    public static readonly ConcurrentBag<Snapshot_avatar> pool = new ConcurrentBag<Snapshot_avatar>();
                    public static readonly Dullahan.CharDiffer differ = new Dullahan.CharDiffer();

                    // diffTicks and diffEnds form an associative array
                    public readonly Ring<int> diffTicks = new Ring<int>();
                    public readonly Ring<(int, int)> diffSpans = new Ring<(int, int)>();
                    public readonly BinaryWriter diffWriter = new BinaryWriter(new MemoryStream(), Encoding.UTF8, leaveOpen: true);
                    public System.Char state = default;
                }

                // avatar_ticks and avatar_snapshots form an associative array
                private readonly Ring<int> avatar_ticks = new Ring<int> { 0 };
                private readonly Ring<Snapshot_avatar> avatar_snapshots = new Ring<Snapshot_avatar> {new Snapshot_avatar() };
                public System.Char avatar {
                    get {
                        if (avatar_ticks.Count == 0) {
                            return default;
                        }

                        int index = avatar_ticks.BinarySearch(entity.world.currentTick);
                        if (index < 0) {
                            return avatar_snapshots[~index - 1].state;
                        } else {
                            return avatar_snapshots[index].state;
                        }
                    }

                    set {
                        Snapshot_avatar snapshot;

                        int tick = entity.world.currentTick;
                        int index = avatar_ticks.BinarySearch(tick);
                        if (index < 0) {
                            if (!Snapshot_avatar.pool.TryTake(out snapshot)) {
                                snapshot = new Snapshot_avatar();
                            }
                        } else {
                            snapshot = avatar_snapshots[index];
                            avatar_snapshots.RemoveAt(index);
                            avatar_ticks.RemoveAt(index);
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
                        for (int i = start; i >= avatar_ticks.Start; --i) {
                            int savedOffset = snapshot.diffWriter.GetOffset();
                            if (!Snapshot_avatar.differ.Diff(avatar_snapshots[i].state, snapshot.state, snapshot.diffWriter)) {
                                if (i == start) {
                                    // value didn't change
                                    Snapshot_avatar.pool.Add(snapshot);
                                    return;
                                }

                                snapshot.diffWriter.SetOffset(savedOffset);
                            }

                            snapshot.diffTicks.PushEnd(tick - avatar_ticks[i]);
                            snapshot.diffSpans.PushEnd((savedOffset, snapshot.diffWriter.GetOffset() - savedOffset));
                        }

                        avatar_ticks.Insert(index, tick);
                        avatar_snapshots.Insert(index, snapshot);

                        // now that we have a new or modified snapshot, later snapshots need to diff with it
                        for (int i = index + 1; i < avatar_ticks.End; ++i) {
                            int savedOffset = avatar_snapshots[i].diffWriter.GetOffset();
                            if (!Snapshot_avatar.differ.Diff(snapshot.state, avatar_snapshots[i].state, avatar_snapshots[i].diffWriter)) {
                                avatar_snapshots[i].diffWriter.SetOffset(savedOffset);
                            }

                            int diffTick = avatar_ticks[i] - tick;
                            var diffSpan = (savedOffset, avatar_snapshots[i].diffWriter.GetOffset() - savedOffset);
                            int diffIndex = avatar_snapshots[i].diffTicks.BinarySearch(diffTick);
                            if (diffIndex < 0) {
                                avatar_snapshots[i].diffTicks.Insert(~diffIndex, diffTick);
                                avatar_snapshots[i].diffSpans.Insert(~diffIndex, diffSpan);
                            } else {
                                //avatar_snapshots[i].diffTick[diffIndex] = diffTick; // diffTick was found; no need to change it
                                avatar_snapshots[i].diffSpans[diffIndex] = diffSpan;
                            }
                        }
                    }
                }

                private void Dispose(bool disposing) {
                    if (disposalTick == int.MaxValue) {
                        if (disposing) {
                            ((VisualizationSystem_Implementation)entity.world.visualizationSystem).avatars_entities.Remove(entity);
                        }

                        entity.viewComponent = null;
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
