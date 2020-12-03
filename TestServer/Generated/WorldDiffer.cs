/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TestServer {
    public class WorldDiffer : IDiffer<(World, int)> {
        private readonly EntityDiffer entityDiffer = new EntityDiffer();


        public bool Diff((World, int) worldAtOldTick, (World, int) worldAtNewTick, BinaryWriter writer) {
            var oldWorld = worldAtOldTick.Item1;
            var newWorld = worldAtNewTick.Item1;
            if (oldWorld != null && oldWorld != newWorld) {
                throw new InvalidOperationException("Can only diff the same world at different ticks.");
            }

            int oldTick = worldAtOldTick.Item2;
            int newTick = worldAtNewTick.Item2;

            // reserve room for count of changed entities
            int startPosition = writer.GetPosition();
            writer.Write(0);
            int changedCount = 0;
            var entitiesById = oldWorld != null ? oldWorld.entitiesById : new Dictionary<Guid, Entity>();
            var disposed = new HashSet<Guid>();
            var constructed = new HashSet<Entity>();
            foreach (var entity in entitiesById.Values) {
                if (entity.constructionTick <= oldTick && oldTick < entity.disposalTick) {
                    // entity exists in old world
                    if (entity.constructionTick <= newTick && newTick < entity.disposalTick) {
                        // entity also exists in new world
                        int keyPosition = writer.GetPosition(); // preemptively write the key; erase when entities don't differ
                        writer.Write(entity.id.ToByteArray());
                        if (entityDiffer.Diff((entity, oldTick), (entity, newTick), writer)) {
                            ++changedCount;
                        } else {
                            writer.SetPosition(keyPosition);
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

            int savedPosition = writer.GetPosition();
            writer.SetPosition(startPosition);
            writer.Write(changedCount);
            writer.SetPosition(savedPosition);

            writer.Write(disposed.Count);
            foreach (var id in disposed) {
                writer.Write(id.ToByteArray());
            }

            writer.Write(constructed.Count);
            foreach (var entity in constructed) {
                entityDiffer.Diff((null, -1), (entity, newTick), writer);
            }

            return changedCount > 0 || disposed.Count > 0 || constructed.Count > 0;

        }

        public void Patch(ref (World, int) worldAtTick, BinaryReader reader) {

        }
    }
}
