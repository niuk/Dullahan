/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.IO;

namespace TestServer {
    partial class World {
        partial class Entity {
            partial class InputComponent {
                public class Differ : IDiffer<(InputComponent, int)> {
                    public bool Diff((InputComponent, int) componentAtOldTick, (InputComponent, int) componentAtNewTick, BinaryWriter writer) {
                        if (componentAtOldTick.Item1 != componentAtNewTick.Item1) {
                            throw new InvalidOperationException("Can only diff the same component at different ticks.");
                        }

                        var component = componentAtOldTick.Item1;
                        int oldTick = componentAtOldTick.Item2;
                        int newTick = componentAtNewTick.Item2;
                        bool changed = false;

                        // reserve room
                        byte flag = 0;
                        int flagOffset = writer.GetOffset();
                        writer.Write(flag);

                        /* deltaX */ {
                            int oldIndex = component.deltaX_ticks.BinarySearch(oldTick);
                            if (oldIndex < 0) {
                                oldIndex = ~oldIndex - 1;
                            }

                            int newIndex = component.deltaX_ticks.BinarySearch(newTick);
                            if (newIndex < 0) {
                                newIndex = ~newIndex - 1;
                            }

                            var snapshot = component.deltaX_snapshots[newIndex];
                            (int index, int count) = snapshot.diffs[newIndex - oldIndex];
                            if (count > 0) {
                                changed = true;
                                writer.Write(((MemoryStream)snapshot.diffWriter.BaseStream).GetBuffer(), index, count);
                                flag |= 1 << 0;
                            }
                        }

                        /* deltaY */ {
                            int oldIndex = component.deltaY_ticks.BinarySearch(oldTick);
                            if (oldIndex < 0) {
                                oldIndex = ~oldIndex - 1;
                            }

                            int newIndex = component.deltaY_ticks.BinarySearch(newTick);
                            if (newIndex < 0) {
                                newIndex = ~newIndex - 1;
                            }

                            var snapshot = component.deltaY_snapshots[newIndex];
                            (int index, int count) = snapshot.diffs[newIndex - oldIndex];
                            if (count > 0) {
                                changed = true;
                                writer.Write(((MemoryStream)snapshot.diffWriter.BaseStream).GetBuffer(), index, count);
                                flag |= 1 << 1;
                            }
                        }

                        int savedOffset = writer.GetOffset();
                        writer.SetOffset(flagOffset);
                        writer.Write(flag);
                        writer.SetOffset(savedOffset);

                        return changed;
                    }

                    public void Patch(ref (InputComponent, int) componentAtTick, BinaryReader reader) {
                        var component = componentAtTick.Item1;
                        byte flag = reader.ReadByte();

                        if ((flag >> 0 & 1) != 0) {
                            var value = component.deltaX;
                            Snapshot_deltaX.differ.Patch(ref value, reader);
                            component.deltaX = value;
                        }

                        if ((flag >> 1 & 1) != 0) {
                            var value = component.deltaY;
                            Snapshot_deltaY.differ.Patch(ref value, reader);
                            component.deltaY = value;
                        }

                    }
                }
            }
        }
    }
}
