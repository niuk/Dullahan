/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.IO;

namespace TestGame {
    partial class World {
        partial class Entity {
            public sealed class Differ : IDiffer<(Entity, int)> {
                private readonly InputComponent.Differ inputComponentDiffer = new InputComponent.Differ();
                private readonly PositionComponent.Differ positionComponentDiffer = new PositionComponent.Differ();
                public bool Diff((Entity, int) entityAtOldTick, (Entity, int) entityAtNewTick, BinaryWriter writer) {
                    var entity = entityAtOldTick.Item1;
                    if (entity == null) {{
                        entity = entityAtNewTick.Item1;
                        if (entity == null) {{
                            throw new InvalidOperationException("Cannot diff two null entities.");
                        }}
                    }} else {{
                        if (entity != entityAtNewTick.Item1) {{
                            throw new InvalidOperationException("Can only diff the same entity at different ticks.");
                        }}
                    }}

                    int oldTick = entityAtOldTick.Item2;
                    int newTick = entityAtNewTick.Item2;
                    writer.Write(oldTick);
                    Console.WriteLine($"{nameof(oldTick)} -> {writer.GetOffset()}");
                    writer.Write(newTick);
                    Console.WriteLine($"{nameof(newTick)} -> {writer.GetOffset()}");

                    byte dirtyFlags = 0;
                    byte deleteFlags = 0;
                    byte createFlags = 0;
                    int flagOffset = writer.GetOffset();
                    writer.Write(dirtyFlags);
                    writer.Write(createFlags);
                    writer.Write(deleteFlags);
                    Console.WriteLine($"flags -> {writer.GetOffset()}");

                    /* inputComponent */
                    {
                        InputComponent oldSnapshot;
                        int oldSnapshotIndex = entity.inputComponent_ticks.BinarySearch(oldTick);
                        if (oldSnapshotIndex < 0) {
                            oldSnapshotIndex = ~oldSnapshotIndex - 1;
                        }

                        if (oldSnapshotIndex < 0) {
                            oldSnapshot = default;
                        } else {
                            oldSnapshot = entity.inputComponent_snapshots[oldSnapshotIndex];
                        }

                        InputComponent newSnapshot;
                        int newSnapshotIndex = entity.inputComponent_ticks.BinarySearch(newTick);
                        if (newSnapshotIndex < 0) {
                            newSnapshotIndex = ~newSnapshotIndex - 1;
                        }

                        if (newSnapshotIndex < 0) {
                            newSnapshot = default;
                        } else {
                            newSnapshot = entity.inputComponent_snapshots[newSnapshotIndex];
                        }

                        if (oldSnapshot != null) {
                            if (newSnapshot != null) {
                                int componentOffset = writer.GetOffset();
                                if (inputComponentDiffer.Diff((oldSnapshot, oldTick), (newSnapshot, newTick), writer)) {
                                    dirtyFlags |= 1 << 0;
                                } else {
                                    writer.SetOffset(componentOffset);
                                }
                            } else {
                                deleteFlags |= 1 << 0;
                            }
                        } else {
                            if (newSnapshot != null) {
                                inputComponentDiffer.Diff((default, oldTick), (newSnapshot, newTick), writer);
                                createFlags |= 1 << 0;
                            } else {
                                // stayed null
                            }
                        }
                    }

                    /* positionComponent */ {
                        PositionComponent oldSnapshot;
                        int oldSnapshotIndex = entity.positionComponent_ticks.BinarySearch(oldTick);
                        if (oldSnapshotIndex < 0) {
                            oldSnapshotIndex = ~oldSnapshotIndex - 1;
                        }

                        if (oldSnapshotIndex < 0) {
                            oldSnapshot = default;
                        } else {
                            oldSnapshot = entity.positionComponent_snapshots[oldSnapshotIndex];
                        }

                        PositionComponent newSnapshot;
                        int newSnapshotIndex = entity.positionComponent_ticks.BinarySearch(newTick);
                        if (newSnapshotIndex < 0) {
                            newSnapshotIndex = ~newSnapshotIndex - 1;
                        }

                        if (newSnapshotIndex < 0) {
                            newSnapshot = default;
                        } else {
                            newSnapshot = entity.positionComponent_snapshots[newSnapshotIndex];
                        }

                        if (oldSnapshot != null) {
                            if (newSnapshot != null) {
                                int componentOffset = writer.GetOffset();
                                if (positionComponentDiffer.Diff((oldSnapshot, oldTick), (newSnapshot, newTick), writer)) {
                                    dirtyFlags |= 1 << 1;
                                } else {
                                    writer.SetOffset(componentOffset);
                                }
                            } else {
                                deleteFlags |= 1 << 1;
                            }
                        } else {
                            if (newSnapshot != null) {
                                positionComponentDiffer.Diff((default, oldTick), (newSnapshot, newTick), writer);
                                createFlags |= 1 << 1;
                            } else {
                                // stayed null
                            }
                        }
                    }

                    int savedOffset = writer.GetOffset();
                    writer.SetOffset(flagOffset);
                    writer.Write(dirtyFlags);
                    writer.Write(createFlags);
                    writer.Write(deleteFlags);
                    writer.SetOffset(savedOffset);

                    return dirtyFlags != 0 || createFlags != 0 || deleteFlags != 0;
                }

                public void Patch(ref (Entity, int) entityAtTick, BinaryReader reader) {
                    var entity = entityAtTick.Item1;
                    if (entity == null) {{
                        entity = new Entity();
                    }}

                    int tick = entityAtTick.Item2;
                    int oldTick = reader.ReadInt32();
                    Console.WriteLine($"\t{nameof(oldTick)} -> {reader.GetOffset()}");
                    int newTick = reader.ReadInt32();
                    Console.WriteLine($"\t{nameof(newTick)} -> {reader.GetOffset()}");
                    if (tick != oldTick) {
                        throw new InvalidOperationException($"Entity is at tick {tick} but patch is from tick {oldTick} to {newTick}.");
                    }

                    byte dirtyFlags = reader.ReadByte();
                    byte createFlags = reader.ReadByte();
                    byte deleteFlags = reader.ReadByte();
                    Console.WriteLine($"\tflags -> {reader.GetOffset()}");

                    if ((dirtyFlags >> 0 & 1) != 0) {
                        var tuple = (entity.inputComponent, tick);
                        inputComponentDiffer.Patch(ref tuple, reader);
                    } else if ((createFlags >> 0 & 1) != 0) {
                        var tuple = (new InputComponent(entity), tick);
                        inputComponentDiffer.Patch(ref tuple, reader);
                    } else if ((deleteFlags >> 0 & 1) != 0) {
                        entity.inputComponent.Dispose();
                    }

                    if ((dirtyFlags >> 1 & 1) != 0) {
                        var tuple = (entity.positionComponent, tick);
                        positionComponentDiffer.Patch(ref tuple, reader);
                    } else if ((createFlags >> 1 & 1) != 0) {
                        var tuple = (new PositionComponent(entity), tick);
                        positionComponentDiffer.Patch(ref tuple, reader);
                    } else if ((deleteFlags >> 1 & 1) != 0) {
                        entity.positionComponent.Dispose();
                    }

                    entityAtTick.Item2 = newTick;
                }
            }
        }
    }
}
