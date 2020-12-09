/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.Collections.Generic;
using System.IO;

namespace TestServer {
    partial class World {
        public class Differ : IDiffer<(World, int)> {
            private readonly Entity.Differ entityDiffer = new Entity.Differ();

            public bool Diff((World, int) worldAtOldTick, (World, int) worldAtNewTick, BinaryWriter writer) {
                if (worldAtOldTick.Item1 != worldAtNewTick.Item1) {
                    throw new InvalidOperationException("Can only diff the same world at different ticks or a null world with a new world.");
                }

                var world = worldAtOldTick.Item1;
                int oldTick = worldAtOldTick.Item2;
                int newTick = worldAtNewTick.Item2;

                writer.Write(oldTick);
                writer.Write(newTick);

                // reserve room for count of changed entities
                int startOffset = writer.GetOffset();
                writer.Write(0);
                int changedCount = 0;
                var disposed = new HashSet<Guid>();
                var constructed = new HashSet<Entity>();
                foreach (var entity in world.entitiesById.Values) {
                    if (entity.constructionTick <= oldTick && oldTick < entity.disposalTick) {
                        // entity exists in old world
                        if (entity.constructionTick <= newTick && newTick < entity.disposalTick) {
                            // entity also exists in new world
                            int keyOffset = writer.GetOffset(); // preemptively write the key; erase when entities don't differ
                            writer.Write(entity.id.ToByteArray());
                            if (entityDiffer.Diff((entity, oldTick), (entity, newTick), writer)) {
                                ++changedCount;
                            } else {
                                writer.SetOffset(keyOffset);
                            }
                        } else {
                            // entity was disposed
                            disposed.Add(entity.id);
                        }
                    } else {
                        // entity does not exist in old world
                        if (entity.constructionTick <= newTick && newTick < entity.disposalTick) {
                            // entity exists in new world
                            constructed.Add(entity);
                        } else {
                            // entity existed at some point but not in either world
                        }
                    }
                }

                int savedOffset = writer.GetOffset();
                writer.SetOffset(startOffset);
                writer.Write(changedCount);
                writer.SetOffset(savedOffset);

                writer.Write(disposed.Count);
                foreach (var id in disposed) {
                    writer.Write(id.ToByteArray());
                }

                writer.Write(constructed.Count);
                foreach (var entity in constructed) {
                    entityDiffer.Diff((default, oldTick), (entity, newTick), writer);
                }

                return changedCount > 0 || disposed.Count > 0 || constructed.Count > 0;
            }

            public void Patch(ref (World, int) worldAtTick, BinaryReader reader) {
                var world = worldAtTick.Item1;
                var tick = worldAtTick.Item2;

                int oldTick = reader.ReadInt32();
                int newTick = reader.ReadInt32();
                if (tick != oldTick) {
                    throw new InvalidOperationException($"World is at tick {tick} but patch is from tick {oldTick} to {newTick}.");
                }

                world.AddTick(newTick);

                int changedCount = reader.ReadInt32();
                for (int i = 0; i < changedCount; ++i) {
                    var id = new Guid(reader.ReadBytes(16));
                    var entityAtTick = (world.entitiesById[id], tick);
                    entityDiffer.Patch(ref entityAtTick, reader);
                    world.entitiesById[id] = entityAtTick.Item1;
                }

                int disposedCount = reader.ReadInt32();
                for (int i = 0; i < disposedCount; ++i) {
                    world.entitiesById.Remove(new Guid(reader.ReadBytes(16)));
                }

                int constructedCount = reader.ReadInt32();
                for (int i = 0; i < constructedCount; ++i) {
                    var entityAtTick = (new Entity(world, new Guid(reader.ReadBytes(16))), tick);
                    entityDiffer.Patch(ref entityAtTick, reader);
                }

                worldAtTick.Item2 = newTick;
            }
        }
    }
}
