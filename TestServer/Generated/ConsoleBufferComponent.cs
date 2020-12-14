/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace TestGame {
    partial class World {
        partial class Entity {
            public sealed partial class ConsoleBufferComponent : TestGame.IConsoleBufferComponent {
                public readonly Entity entity;
                public readonly int constructionTick;
                public int disposalTick { get; private set; }

                public ConsoleBufferComponent(Entity entity) {
                    this.entity = entity;
                    constructionTick = entity.world.currentTick;
                    disposalTick = int.MaxValue;
                    entity.consoleBufferComponent = this;

                    if (((VisualizationSystem_Implementation)entity.world.visualizationSystem).consoleBuffer_entity != null) {
                        throw new InvalidOperationException("Multiple consoleBuffer singletons for TestGame.VisualizationSystem!");
                    }

                    ((VisualizationSystem_Implementation)entity.world.visualizationSystem).consoleBuffer_entity = entity;

                }

                private class Snapshot_consoleBuffer {
                    public static readonly ConcurrentBag<Snapshot_consoleBuffer> pool = new ConcurrentBag<Snapshot_consoleBuffer>();
                    public static readonly Dullahan.Rank2BufferDiffer differ = new Dullahan.Rank2BufferDiffer();

                    // diffTicks and diffEnds form an associative array
                    public readonly Ring<int> diffTicks = new Ring<int>();
                    public readonly Ring<(int, int)> diffSpans = new Ring<(int, int)>();
                    public readonly BinaryWriter diffWriter = new BinaryWriter(new MemoryStream(), Encoding.UTF8, leaveOpen: true);
                    public System.Byte[,] state = default;
                }

                // consoleBuffer_ticks and consoleBuffer_snapshots form an associative array
                private readonly Ring<int> consoleBuffer_ticks = new Ring<int> { 0 };
                private readonly Ring<Snapshot_consoleBuffer> consoleBuffer_snapshots = new Ring<Snapshot_consoleBuffer> {new Snapshot_consoleBuffer() };
                public System.Byte[,] consoleBuffer {
                    get {
                        if (consoleBuffer_ticks.Count == 0) {
                            return default;
                        }

                        int index = consoleBuffer_ticks.BinarySearch(entity.world.currentTick);
                        if (index < 0) {
                            return consoleBuffer_snapshots[~index - 1].state;
                        } else {
                            return consoleBuffer_snapshots[index].state;
                        }
                    }

                    set {
                        Snapshot_consoleBuffer snapshot;

                        int tick = entity.world.currentTick;
                        int index = consoleBuffer_ticks.BinarySearch(tick);
                        if (index < 0) {
                            if (!Snapshot_consoleBuffer.pool.TryTake(out snapshot)) {
                                snapshot = new Snapshot_consoleBuffer();
                            }
                        } else {
                            snapshot = consoleBuffer_snapshots[index];
                            consoleBuffer_snapshots.RemoveAt(index);
                            consoleBuffer_ticks.RemoveAt(index);
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
                        for (int i = start; i >= consoleBuffer_ticks.Start; --i) {
                            int savedOffset = snapshot.diffWriter.GetOffset();
                            if (!Snapshot_consoleBuffer.differ.Diff(consoleBuffer_snapshots[i].state, snapshot.state, snapshot.diffWriter)) {
                                if (i == start) {
                                    // value didn't change
                                    Snapshot_consoleBuffer.pool.Add(snapshot);
                                    return;
                                }

                                snapshot.diffWriter.SetOffset(savedOffset);
                            }

                            snapshot.diffTicks.PushEnd(tick - consoleBuffer_ticks[i]);
                            snapshot.diffSpans.PushEnd((savedOffset, snapshot.diffWriter.GetOffset() - savedOffset));
                        }

                        consoleBuffer_ticks.Insert(index, tick);
                        consoleBuffer_snapshots.Insert(index, snapshot);

                        // now that we have a new or modified snapshot, later snapshots need to diff with it
                        for (int i = index + 1; i < consoleBuffer_ticks.End; ++i) {
                            int savedOffset = consoleBuffer_snapshots[i].diffWriter.GetOffset();
                            if (!Snapshot_consoleBuffer.differ.Diff(snapshot.state, consoleBuffer_snapshots[i].state, consoleBuffer_snapshots[i].diffWriter)) {
                                consoleBuffer_snapshots[i].diffWriter.SetOffset(savedOffset);
                            }

                            int diffTick = consoleBuffer_ticks[i] - tick;
                            var diffSpan = (savedOffset, consoleBuffer_snapshots[i].diffWriter.GetOffset() - savedOffset);
                            int diffIndex = consoleBuffer_snapshots[i].diffTicks.BinarySearch(diffTick);
                            if (diffIndex < 0) {
                                consoleBuffer_snapshots[i].diffTicks.Insert(~diffIndex, diffTick);
                                consoleBuffer_snapshots[i].diffSpans.Insert(~diffIndex, diffSpan);
                            } else {
                                //consoleBuffer_snapshots[i].diffTick[diffIndex] = diffTick; // diffTick was found; no need to change it
                                consoleBuffer_snapshots[i].diffSpans[diffIndex] = diffSpan;
                            }
                        }
                    }
                }

                private void Dispose(bool disposing) {
                    if (disposalTick == int.MaxValue) {
                        if (disposing) {
                            ((VisualizationSystem_Implementation)entity.world.visualizationSystem).consoleBuffer_entity = null;
                        }

                        entity.consoleBufferComponent = null;
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
