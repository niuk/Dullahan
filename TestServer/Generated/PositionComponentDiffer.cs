/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.IO;

namespace TestGame {
    partial class World {
        partial class Entity {
            partial class PositionComponent {
                public class Differ : IDiffer<(PositionComponent, int)> {
                    public bool Diff((PositionComponent, int) componentAtOldTick, (PositionComponent, int) componentAtNewTick, BinaryWriter writer) {
                        var component = componentAtOldTick.Item1;
                        if (component == null) {
                            component = componentAtNewTick.Item1;
                            if (component == null) {
                                throw new InvalidOperationException("Cannot diff two null components.");
                            }
                        } else {
                            if (component != componentAtNewTick.Item1) {
                                throw new InvalidOperationException("Can only diff the same component at different ticks.");
                            }
                        }

                        int oldTick = componentAtOldTick.Item2;
                        int newTick = componentAtNewTick.Item2;
                        writer.Write(oldTick);
                        writer.Write(newTick);

                        // reserve room
                        byte dirtyFlags = 0;
                        int dirtyFlagsOffset = writer.GetOffset();
                        writer.Write(dirtyFlags);

                        /* x */ {
                            int snapshotIndex = component.x_ticks.BinarySearch(newTick);
                            if (snapshotIndex < 0) {
                                snapshotIndex = ~snapshotIndex - 1;
                            }

                            if (snapshotIndex < 0) {
                                throw new InvalidOperationException($"Tick {newTick} is too old to diff.");
                            }

                            int snapshotTick = component.x_ticks[snapshotIndex];
                            int diffTick = snapshotTick - oldTick;
                            if (diffTick > 0) {
                                var snapshot = component.x_snapshots[snapshotIndex];

                                int diffIndex = snapshot.diffTicks.BinarySearch(diffTick);
                                if (diffIndex < 0) {
                                    diffIndex = ~diffIndex;
                                }

                                if (diffIndex == snapshot.diffTicks.Count) {
                                    throw new InvalidOperationException("Tick {oldTick} is too old to diff.");
                                }

                                var (offset, size) = snapshot.diffSpans[diffIndex];
                                if (size > 0) {
                                    writer.Write(((MemoryStream)snapshot.diffWriter.BaseStream).GetBuffer(), offset, size);
                                    dirtyFlags |= 1 << 0;
                                }
                            }
                        }

                        /* y */ {
                            int snapshotIndex = component.y_ticks.BinarySearch(newTick);
                            if (snapshotIndex < 0) {
                                snapshotIndex = ~snapshotIndex - 1;
                            }

                            if (snapshotIndex < 0) {
                                throw new InvalidOperationException($"Tick {newTick} is too old to diff.");
                            }

                            int snapshotTick = component.y_ticks[snapshotIndex];
                            int diffTick = snapshotTick - oldTick;
                            if (diffTick > 0) {
                                var snapshot = component.y_snapshots[snapshotIndex];

                                int diffIndex = snapshot.diffTicks.BinarySearch(diffTick);
                                if (diffIndex < 0) {
                                    diffIndex = ~diffIndex;
                                }

                                if (diffIndex == snapshot.diffTicks.Count) {
                                    throw new InvalidOperationException("Tick {oldTick} is too old to diff.");
                                }

                                var (offset, size) = snapshot.diffSpans[diffIndex];
                                if (size > 0) {
                                    writer.Write(((MemoryStream)snapshot.diffWriter.BaseStream).GetBuffer(), offset, size);
                                    dirtyFlags |= 1 << 1;
                                }
                            }
                        }

                        int savedOffset = writer.GetOffset();
                        writer.SetOffset(dirtyFlagsOffset);
                        writer.Write(dirtyFlags);
                        writer.SetOffset(savedOffset);

                        return dirtyFlags != 0;
                    }

                    public void Patch(ref (PositionComponent, int) componentAtTick, BinaryReader reader) {
                        var component = componentAtTick.Item1;
                        var tick = componentAtTick.Item2;
                        var oldTick = reader.ReadInt32();
                        var newTick = reader.ReadInt32();
                        if (tick != oldTick) {
                            throw new InvalidOperationException($"Component is at tick {tick} but patch is from tick {oldTick} to {newTick}.");
                        }

                        byte dirtyFlags = reader.ReadByte();

                        if ((dirtyFlags >> 0 & 1) != 0) {
                            var value = component.x;
                            Snapshot_x.differ.Patch(ref value, reader);
                            component.x = value;
                        }

                        if ((dirtyFlags >> 1 & 1) != 0) {
                            var value = component.y;
                            Snapshot_y.differ.Patch(ref value, reader);
                            component.y = value;
                        }

                        componentAtTick.Item2 = newTick;
                    }
                }
            }
        }
    }
}
