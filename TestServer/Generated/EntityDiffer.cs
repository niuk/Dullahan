/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.IO;

namespace TestServer {
    public class EntityDiffer : IDiffer<(Entity, int)> {


        public bool Diff((Entity, int) entityAtOldTick, (Entity, int) entityAtNewTick, BinaryWriter writer) {
            if (entityAtOldTick.Item1 != entityAtNewTick.Item1) {
                throw new InvalidOperationException("Can only diff the same entity at different ticks.");
            }

            return true;

        }

        public void Patch(ref (Entity, int) entityAtTick, BinaryReader reader) {

        }
    }
}
