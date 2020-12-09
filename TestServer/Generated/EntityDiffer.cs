/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.IO;

namespace TestServer {
    partial class World {
        partial class Entity {
            public sealed class Differ : IDiffer<(Entity, int)> {
                private readonly InputComponent.Differ inputComponentDiffer = new InputComponent.Differ();
                private readonly PositionComponent.Differ positionComponentDiffer = new PositionComponent.Differ();
                public bool Diff((Entity, int) entityAtOldTick, (Entity, int) entityAtNewTick, BinaryWriter writer) {
                    if (entityAtOldTick.Item1 != entityAtNewTick.Item1) {
                        throw new InvalidOperationException("Can only diff the same entity at different ticks.");
                    }

                    var entity = entityAtOldTick.Item1;
                    int oldTick = entityAtOldTick.Item2;
                    int newTick = entityAtNewTick.Item2;
                    writer.Write(oldTick);
                    writer.Write(newTick);

                    byte dirtyFlags = 0;
                    byte deleteFlags = 0;
                    byte createFlags = 0;
                    int flagOffset = writer.GetOffset();
                    writer.Write(dirtyFlags);
                    writer.Write(createFlags);
                    writer.Write(deleteFlags);

                    /* inputComponent */ {
                        int oldSnapshotIndex = entity.inputComponent_ticks.BinarySearch(oldTick);
                        if (oldSnapshotIndex < 0) {
                            oldSnapshotIndex = ~oldSnapshotIndex - 1;
                        }

                        if (oldSnapshotIndex < 0) {
                            throw new InvalidOperationException($"Tick {oldTick} is too old to diff.");
                        }

                        int newSnapshotIndex = entity.inputComponent_ticks.BinarySearch(newTick);
                        if (newSnapshotIndex < 0) {
                            newSnapshotIndex = ~newSnapshotIndex - 1;
                        }

                        if (newSnapshotIndex < 0) {
                            throw new InvalidOperationException($"Tick {newTick} is too old to diff.");
                        }

                        var oldSnapshot = entity.inputComponent_snapshots[oldSnapshotIndex];
                        var newSnapshot = entity.inputComponent_snapshots[newSnapshotIndex];
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
                        int oldSnapshotIndex = entity.positionComponent_ticks.BinarySearch(oldTick);
                        if (oldSnapshotIndex < 0) {
                            oldSnapshotIndex = ~oldSnapshotIndex - 1;
                        }

                        if (oldSnapshotIndex < 0) {
                            throw new InvalidOperationException($"Tick {oldTick} is too old to diff.");
                        }

                        int newSnapshotIndex = entity.positionComponent_ticks.BinarySearch(newTick);
                        if (newSnapshotIndex < 0) {
                            newSnapshotIndex = ~newSnapshotIndex - 1;
                        }

                        if (newSnapshotIndex < 0) {
                            throw new InvalidOperationException($"Tick {newTick} is too old to diff.");
                        }

                        var oldSnapshot = entity.positionComponent_snapshots[oldSnapshotIndex];
                        var newSnapshot = entity.positionComponent_snapshots[newSnapshotIndex];
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
                    int tick = entityAtTick.Item2;

                    int oldTick = reader.ReadInt32();
                    int newTick = reader.ReadInt32();
                    if (tick != oldTick) {
                            throw new InvalidOperationException($"Entity is at tick {tick} but patch is from tick {oldTick} to {newTick}.");
                    }

                    byte dirtyFlags = reader.ReadByte();
                    byte createFlags = reader.ReadByte();
                    byte deleteFlags = reader.ReadByte();

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
