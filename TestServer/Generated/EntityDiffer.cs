/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System;
using System.IO;

namespace TestServer {
    public class EntityDiffer : IDiffer<(Entity, int)> {

        private readonly InputComponentDiffer inputComponentDiffer = new InputComponentDiffer();

        private readonly PositionComponentDiffer positionComponentDiffer = new PositionComponentDiffer();


        public bool Diff((Entity, int) entityAtOldTick, (Entity, int) entityAtNewTick, BinaryWriter writer) {
            var oldEntity = entityAtOldTick.Item1;
            var newEntity = entityAtNewTick.Item1;

            if (oldEntity != newEntity) {
                throw new InvalidOperationException("Can only diff the same entity at different ticks.");
            }

            int oldTick = entityAtOldTick.Item2;
            int newTick = entityAtNewTick.Item2;

            inputComponentDiffer.Diff((oldEntity.inputComponent, oldTick), (newEntity.inputComponent, newTick), writer);

            positionComponentDiffer.Diff((oldEntity.positionComponent, oldTick), (newEntity.positionComponent, newTick), writer);

        }

        public void Patch(ref (Entity, int) entityAtTick, BinaryReader reader) {

        }
    }
}
