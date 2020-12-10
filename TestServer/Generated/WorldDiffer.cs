/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.Collections.Generic;
using System.IO;

namespace TestGame {
    partial class World {
        public class Differ : IDiffer<(World, int)> {
            private readonly Entity.Differ entityDiffer = new Entity.Differ();

            public bool Diff((World, int) worldAtOldTick, (World, int) worldAtNewTick, BinaryWriter writer) {
                var world = worldAtOldTick.Item1;
                if (world == null) {
                    world = worldAtNewTick.Item1;
                    if (world == null) {
                        throw new InvalidOperationException("Cannot diff two null worlds.");
                    }
                } else {
                    if (world != worldAtNewTick.Item1) {
                        throw new InvalidOperationException("Can only diff the same world at different ticks.");
                    }
                }

                int oldTick = worldAtOldTick.Item2;
                int newTick = worldAtNewTick.Item2;

                writer.Write(oldTick);
                Console.WriteLine($"{nameof(oldTick)} -> {writer.GetOffset()}");
                writer.Write(newTick);
                Console.WriteLine($"{nameof(newTick)} -> {writer.GetOffset()}");

                // reserve room for count of changed entities
                int startOffset = writer.GetOffset();
                writer.Write(0);
                Console.WriteLine($"changedCount -> {writer.GetOffset()}");
                int changedCount = 0;
                var disposed = new HashSet<Guid>();
                var constructed = new HashSet<Entity>();
                lock (world) {
                    foreach (var entity in world.entitiesById.Values) {
                        if (entity.constructionTick <= oldTick && oldTick < entity.disposalTick) {
                            // entity exists in old world
                            if (entity.constructionTick <= newTick && newTick < entity.disposalTick) {
                                // entity also exists in new world
                                int keyOffset = writer.GetOffset(); // preemptively write the key; erase when entities don't differ
                                writer.Write(entity.id.ToByteArray());
                                Console.WriteLine($"Guid -> {writer.GetOffset()}");
                                if (entityDiffer.Diff((entity, oldTick), (entity, newTick), writer)) {
                                    ++changedCount;
                                } else {
                                    writer.SetOffset(keyOffset);
                                }
                                Console.WriteLine($"{nameof(entityDiffer)} -> {writer.GetOffset()}");
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
                    Console.WriteLine($"disposedCount -> {writer.GetOffset()}");
                    foreach (var id in disposed) {
                        writer.Write(id.ToByteArray());
                        Console.WriteLine($"{nameof(id)} -> {writer.GetOffset()}");
                    }

                    writer.Write(constructed.Count);
                    Console.WriteLine($"constructedCount -> {writer.GetOffset()}");
                    foreach (var entity in constructed) {
                        writer.Write(entity.id.ToByteArray());
                        Console.WriteLine($"Guid -> {writer.GetOffset()}");
                        entityDiffer.Diff((entity, oldTick), (entity, newTick), writer);
                        Console.WriteLine($"{nameof(entityDiffer)} -> {writer.GetOffset()}");
                    }
                }

                return changedCount > 0 || disposed.Count > 0 || constructed.Count > 0;
            }

            public void Patch(ref (World, int) worldAtTick, BinaryReader reader) {
                var world = worldAtTick.Item1;
                if (world == null) {
                    world = new World();
                }

                var tick = worldAtTick.Item2;
                int oldTick = reader.ReadInt32();
                Console.WriteLine($"\t{nameof(oldTick)} -> {reader.GetOffset()}");
                int newTick = reader.ReadInt32();
                Console.WriteLine($"\t{nameof(newTick)} -> {reader.GetOffset()}");
                if (tick != oldTick) {
                    throw new InvalidOperationException($"World is at tick {tick} but patch is from tick {oldTick} to {newTick}.");
                }

                lock (world) {
                    world.AddTick(newTick);

                    int changedCount = reader.ReadInt32();
                    Console.WriteLine($"\t{nameof(changedCount)} -> {reader.GetOffset()}");
                    for (int i = 0; i < changedCount; ++i) {
                        var id = new Guid(reader.ReadBytes(16));
                        Console.WriteLine($"\tGuid -> {reader.GetOffset()}");
                        var entityAtTick = (world.entitiesById[id], tick);
                        entityDiffer.Patch(ref entityAtTick, reader);
                        Console.WriteLine($"\t{nameof(entityDiffer)} -> {reader.GetOffset()}");
                        world.entitiesById[id] = entityAtTick.Item1;
                    }

                    int disposedCount = reader.ReadInt32();
                    Console.WriteLine($"\t{nameof(disposedCount)} -> {reader.GetOffset()}");
                    for (int i = 0; i < disposedCount; ++i) {
                        world.entitiesById.Remove(new Guid(reader.ReadBytes(16)));
                    }

                    int constructedCount = reader.ReadInt32();
                    Console.WriteLine($"\t{nameof(constructedCount)} -> {reader.GetOffset()}");
                    for (int i = 0; i < constructedCount; ++i) {
                        var entityAtTick = (new Entity(world, new Guid(reader.ReadBytes(16))), tick);
                        Console.WriteLine($"\tGuid -> {reader.GetOffset()}");
                        entityDiffer.Patch(ref entityAtTick, reader);
                        Console.WriteLine($"\t{nameof(entityDiffer)} -> {reader.GetOffset()}");
                    }
                }

                worldAtTick.Item1 = world;
                worldAtTick.Item2 = newTick;
            }
        }
    }
}
