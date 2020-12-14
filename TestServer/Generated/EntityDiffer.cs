/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.IO;

namespace TestGame {
    partial class World {
        partial class Entity {
            public sealed class Differ : IDiffer<(Entity, int)> {
                private readonly ConsoleBufferComponent.Differ consoleBufferComponentDiffer = new ConsoleBufferComponent.Differ();
                private readonly PositionComponent.Differ positionComponentDiffer = new PositionComponent.Differ();
                private readonly TimeComponent.Differ timeComponentDiffer = new TimeComponent.Differ();
                private readonly VelocityComponent.Differ velocityComponentDiffer = new VelocityComponent.Differ();
                private readonly ViewComponent.Differ viewComponentDiffer = new ViewComponent.Differ();
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
                    if (oldTick >= newTick) {{
                        throw new InvalidOperationException($"Old tick {{oldTick}} must precede new tick {{newTick}}.");
                    }}

                    writer.Write(oldTick);
                    writer.Write(newTick);

                    byte dirtyFlags = 0;
                    byte deleteFlags = 0;
                    byte createFlags = 0;
                    int flagOffset = writer.GetOffset();
                    writer.Write(dirtyFlags);
                    writer.Write(createFlags);
                    writer.Write(deleteFlags);

                    /* consoleBufferComponent */ {
                        ConsoleBufferComponent oldSnapshot;
                        int oldSnapshotIndex = entity.consoleBufferComponent_ticks.BinarySearch(oldTick);
                        if (oldSnapshotIndex < 0) {
                            oldSnapshotIndex = ~oldSnapshotIndex - 1;
                        }

                        if (oldSnapshotIndex < 0) {
                            oldSnapshot = default;
                        } else {
                            oldSnapshot = entity.consoleBufferComponent_snapshots[oldSnapshotIndex];
                        }

                        ConsoleBufferComponent newSnapshot;
                        int newSnapshotIndex = entity.consoleBufferComponent_ticks.BinarySearch(newTick);
                        if (newSnapshotIndex < 0) {
                            newSnapshotIndex = ~newSnapshotIndex - 1;
                        }

                        if (newSnapshotIndex < 0) {
                            newSnapshot = default;
                        } else {
                            newSnapshot = entity.consoleBufferComponent_snapshots[newSnapshotIndex];
                        }

                        if (oldSnapshot != null) {
                            if (newSnapshot != null) {
                                int componentOffset = writer.GetOffset();
                                if (consoleBufferComponentDiffer.Diff((oldSnapshot, oldTick), (newSnapshot, newTick), writer)) {
                                    dirtyFlags |= 1 << 0;
                                } else {
                                    writer.SetOffset(componentOffset);
                                }
                            } else {
                                deleteFlags |= 1 << 0;
                            }
                        } else {
                            if (newSnapshot != null) {
                                consoleBufferComponentDiffer.Diff((default, oldTick), (newSnapshot, newTick), writer);
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

                    /* timeComponent */ {
                        TimeComponent oldSnapshot;
                        int oldSnapshotIndex = entity.timeComponent_ticks.BinarySearch(oldTick);
                        if (oldSnapshotIndex < 0) {
                            oldSnapshotIndex = ~oldSnapshotIndex - 1;
                        }

                        if (oldSnapshotIndex < 0) {
                            oldSnapshot = default;
                        } else {
                            oldSnapshot = entity.timeComponent_snapshots[oldSnapshotIndex];
                        }

                        TimeComponent newSnapshot;
                        int newSnapshotIndex = entity.timeComponent_ticks.BinarySearch(newTick);
                        if (newSnapshotIndex < 0) {
                            newSnapshotIndex = ~newSnapshotIndex - 1;
                        }

                        if (newSnapshotIndex < 0) {
                            newSnapshot = default;
                        } else {
                            newSnapshot = entity.timeComponent_snapshots[newSnapshotIndex];
                        }

                        if (oldSnapshot != null) {
                            if (newSnapshot != null) {
                                int componentOffset = writer.GetOffset();
                                if (timeComponentDiffer.Diff((oldSnapshot, oldTick), (newSnapshot, newTick), writer)) {
                                    dirtyFlags |= 1 << 2;
                                } else {
                                    writer.SetOffset(componentOffset);
                                }
                            } else {
                                deleteFlags |= 1 << 2;
                            }
                        } else {
                            if (newSnapshot != null) {
                                timeComponentDiffer.Diff((default, oldTick), (newSnapshot, newTick), writer);
                                createFlags |= 1 << 2;
                            } else {
                                // stayed null
                            }
                        }
                    }

                    /* velocityComponent */ {
                        VelocityComponent oldSnapshot;
                        int oldSnapshotIndex = entity.velocityComponent_ticks.BinarySearch(oldTick);
                        if (oldSnapshotIndex < 0) {
                            oldSnapshotIndex = ~oldSnapshotIndex - 1;
                        }

                        if (oldSnapshotIndex < 0) {
                            oldSnapshot = default;
                        } else {
                            oldSnapshot = entity.velocityComponent_snapshots[oldSnapshotIndex];
                        }

                        VelocityComponent newSnapshot;
                        int newSnapshotIndex = entity.velocityComponent_ticks.BinarySearch(newTick);
                        if (newSnapshotIndex < 0) {
                            newSnapshotIndex = ~newSnapshotIndex - 1;
                        }

                        if (newSnapshotIndex < 0) {
                            newSnapshot = default;
                        } else {
                            newSnapshot = entity.velocityComponent_snapshots[newSnapshotIndex];
                        }

                        if (oldSnapshot != null) {
                            if (newSnapshot != null) {
                                int componentOffset = writer.GetOffset();
                                if (velocityComponentDiffer.Diff((oldSnapshot, oldTick), (newSnapshot, newTick), writer)) {
                                    dirtyFlags |= 1 << 3;
                                } else {
                                    writer.SetOffset(componentOffset);
                                }
                            } else {
                                deleteFlags |= 1 << 3;
                            }
                        } else {
                            if (newSnapshot != null) {
                                velocityComponentDiffer.Diff((default, oldTick), (newSnapshot, newTick), writer);
                                createFlags |= 1 << 3;
                            } else {
                                // stayed null
                            }
                        }
                    }

                    /* viewComponent */ {
                        ViewComponent oldSnapshot;
                        int oldSnapshotIndex = entity.viewComponent_ticks.BinarySearch(oldTick);
                        if (oldSnapshotIndex < 0) {
                            oldSnapshotIndex = ~oldSnapshotIndex - 1;
                        }

                        if (oldSnapshotIndex < 0) {
                            oldSnapshot = default;
                        } else {
                            oldSnapshot = entity.viewComponent_snapshots[oldSnapshotIndex];
                        }

                        ViewComponent newSnapshot;
                        int newSnapshotIndex = entity.viewComponent_ticks.BinarySearch(newTick);
                        if (newSnapshotIndex < 0) {
                            newSnapshotIndex = ~newSnapshotIndex - 1;
                        }

                        if (newSnapshotIndex < 0) {
                            newSnapshot = default;
                        } else {
                            newSnapshot = entity.viewComponent_snapshots[newSnapshotIndex];
                        }

                        if (oldSnapshot != null) {
                            if (newSnapshot != null) {
                                int componentOffset = writer.GetOffset();
                                if (viewComponentDiffer.Diff((oldSnapshot, oldTick), (newSnapshot, newTick), writer)) {
                                    dirtyFlags |= 1 << 4;
                                } else {
                                    writer.SetOffset(componentOffset);
                                }
                            } else {
                                deleteFlags |= 1 << 4;
                            }
                        } else {
                            if (newSnapshot != null) {
                                viewComponentDiffer.Diff((default, oldTick), (newSnapshot, newTick), writer);
                                createFlags |= 1 << 4;
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
                        var tuple = (entity.consoleBufferComponent, tick);
                        consoleBufferComponentDiffer.Patch(ref tuple, reader);
                    } else if ((createFlags >> 0 & 1) != 0) {
                        if (entity.consoleBufferComponent != null) {
                            entity.consoleBufferComponent.Dispose();
                            entity.consoleBufferComponent = null;
                        }

                        var tuple = (new ConsoleBufferComponent(entity), tick);
                        consoleBufferComponentDiffer.Patch(ref tuple, reader);
                    } else if ((deleteFlags >> 0 & 1) != 0) {
                        entity.consoleBufferComponent.Dispose();
                    }

                    if ((dirtyFlags >> 1 & 1) != 0) {
                        var tuple = (entity.positionComponent, tick);
                        positionComponentDiffer.Patch(ref tuple, reader);
                    } else if ((createFlags >> 1 & 1) != 0) {
                        if (entity.positionComponent != null) {
                            entity.positionComponent.Dispose();
                            entity.positionComponent = null;
                        }

                        var tuple = (new PositionComponent(entity), tick);
                        positionComponentDiffer.Patch(ref tuple, reader);
                    } else if ((deleteFlags >> 1 & 1) != 0) {
                        entity.positionComponent.Dispose();
                    }

                    if ((dirtyFlags >> 2 & 1) != 0) {
                        var tuple = (entity.timeComponent, tick);
                        timeComponentDiffer.Patch(ref tuple, reader);
                    } else if ((createFlags >> 2 & 1) != 0) {
                        if (entity.timeComponent != null) {
                            entity.timeComponent.Dispose();
                            entity.timeComponent = null;
                        }

                        var tuple = (new TimeComponent(entity), tick);
                        timeComponentDiffer.Patch(ref tuple, reader);
                    } else if ((deleteFlags >> 2 & 1) != 0) {
                        entity.timeComponent.Dispose();
                    }

                    if ((dirtyFlags >> 3 & 1) != 0) {
                        var tuple = (entity.velocityComponent, tick);
                        velocityComponentDiffer.Patch(ref tuple, reader);
                    } else if ((createFlags >> 3 & 1) != 0) {
                        if (entity.velocityComponent != null) {
                            entity.velocityComponent.Dispose();
                            entity.velocityComponent = null;
                        }

                        var tuple = (new VelocityComponent(entity), tick);
                        velocityComponentDiffer.Patch(ref tuple, reader);
                    } else if ((deleteFlags >> 3 & 1) != 0) {
                        entity.velocityComponent.Dispose();
                    }

                    if ((dirtyFlags >> 4 & 1) != 0) {
                        var tuple = (entity.viewComponent, tick);
                        viewComponentDiffer.Patch(ref tuple, reader);
                    } else if ((createFlags >> 4 & 1) != 0) {
                        if (entity.viewComponent != null) {
                            entity.viewComponent.Dispose();
                            entity.viewComponent = null;
                        }

                        var tuple = (new ViewComponent(entity), tick);
                        viewComponentDiffer.Patch(ref tuple, reader);
                    } else if ((deleteFlags >> 4 & 1) != 0) {
                        entity.viewComponent.Dispose();
                    }

                    entityAtTick.Item2 = newTick;
                }
            }
        }
    }
}
