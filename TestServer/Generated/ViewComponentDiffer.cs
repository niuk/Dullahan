/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.IO;

namespace TestGame {
    partial class World {
        partial class Entity {
            partial class ViewComponent {
                public class Differ : IDiffer<(ViewComponent, int)> {
                    public bool Diff((ViewComponent, int) componentAtOldTick, (ViewComponent, int) componentAtNewTick, BinaryWriter writer) {
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
                        if (oldTick >= newTick) {
                            throw new InvalidOperationException($"Old tick {oldTick} must precede new tick {newTick}.");
                        }

                        writer.Write(oldTick);
                        writer.Write(newTick);

                        // reserve room
                        byte dirtyFlags = 0;
                        int dirtyFlagsOffset = writer.GetOffset();
                        writer.Write(dirtyFlags);

                        /* avatar */ {
                            int snapshotIndex = component.avatar_ticks.BinarySearch(newTick);
                            if (snapshotIndex < 0) {
                                snapshotIndex = ~snapshotIndex - 1;
                            }

                            if (snapshotIndex < 0) {
                                throw new InvalidOperationException($"Tick {newTick} is too old to diff.");
                            }

                            int snapshotTick = component.avatar_ticks[snapshotIndex];
                            int diffTick = snapshotTick - oldTick;
                            if (diffTick > 0) {
                                var snapshot = component.avatar_snapshots[snapshotIndex];

                                int diffIndex = snapshot.diffTicks.BinarySearch(diffTick);
                                if (diffIndex < 0) {
                                    diffIndex = ~diffIndex;
                                }

                                if (diffIndex == snapshot.diffTicks.Count) {
                                    throw new InvalidOperationException($"Tick {oldTick} is too old to diff.");
                                }

                                var (offset, size) = snapshot.diffSpans[diffIndex];
                                if (size > 0) {
                                    writer.Write(((MemoryStream)snapshot.diffWriter.BaseStream).GetBuffer(), offset, size);
                                    dirtyFlags |= 1 << 0;
                                }
                            }
                        }

                        int savedOffset = writer.GetOffset();
                        writer.SetOffset(dirtyFlagsOffset);
                        writer.Write(dirtyFlags);
                        writer.SetOffset(savedOffset);

                        return dirtyFlags != 0;
                    }

                    public void Patch(ref (ViewComponent, int) componentAtTick, BinaryReader reader) {
                        var component = componentAtTick.Item1;
                        var tick = componentAtTick.Item2;
                        var oldTick = reader.ReadInt32();
                        var newTick = reader.ReadInt32();
                        if (tick != oldTick) {
                            throw new InvalidOperationException($"Component is at tick {tick} but patch is from tick {oldTick} to {newTick}.");
                        }

                        byte dirtyFlags = reader.ReadByte();

                        if ((dirtyFlags >> 0 & 1) != 0) {
                            var value = component.avatar;
                            Snapshot_avatar.differ.Patch(ref value, reader);
                            component.avatar = value;
                        }

                        componentAtTick.Item2 = newTick;
                    }
                }
            }
        }
    }
}
