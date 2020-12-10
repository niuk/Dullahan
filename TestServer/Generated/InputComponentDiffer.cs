/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.IO;

namespace TestGame {
    partial class World {
        partial class Entity {
            partial class InputComponent {
                public class Differ : IDiffer<(InputComponent, int)> {
                    public bool Diff((InputComponent, int) componentAtOldTick, (InputComponent, int) componentAtNewTick, BinaryWriter writer) {
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

                        /* deltaX */ {
                            int snapshotIndex = component.deltaX_ticks.BinarySearch(newTick);
                            if (snapshotIndex < 0) {
                                snapshotIndex = ~snapshotIndex - 1;
                            }

                            if (snapshotIndex < 0) {
                                throw new InvalidOperationException($"Tick {newTick} is too old to diff.");
                            }

                            int snapshotTick = component.deltaX_ticks[snapshotIndex];
                            int diffTick = snapshotTick - oldTick;
                            if (diffTick > 0) {
                                var snapshot = component.deltaX_snapshots[snapshotIndex];

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

                        /* deltaY */ {
                            int snapshotIndex = component.deltaY_ticks.BinarySearch(newTick);
                            if (snapshotIndex < 0) {
                                snapshotIndex = ~snapshotIndex - 1;
                            }

                            if (snapshotIndex < 0) {
                                throw new InvalidOperationException($"Tick {newTick} is too old to diff.");
                            }

                            int snapshotTick = component.deltaY_ticks[snapshotIndex];
                            int diffTick = snapshotTick - oldTick;
                            if (diffTick > 0) {
                                var snapshot = component.deltaY_snapshots[snapshotIndex];

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

                    public void Patch(ref (InputComponent, int) componentAtTick, BinaryReader reader) {
                        var component = componentAtTick.Item1;
                        var tick = componentAtTick.Item2;
                        var oldTick = reader.ReadInt32();
                        var newTick = reader.ReadInt32();
                        if (tick != oldTick) {
                            throw new InvalidOperationException($"Component is at tick {tick} but patch is from tick {oldTick} to {newTick}.");
                        }

                        byte dirtyFlags = reader.ReadByte();

                        if ((dirtyFlags >> 0 & 1) != 0) {
                            var value = component.deltaX;
                            Snapshot_deltaX.differ.Patch(ref value, reader);
                            component.deltaX = value;
                        }

                        if ((dirtyFlags >> 1 & 1) != 0) {
                            var value = component.deltaY;
                            Snapshot_deltaY.differ.Patch(ref value, reader);
                            component.deltaY = value;
                        }

                        componentAtTick.Item2 = newTick;
                    }
                }
            }
        }
    }
}
