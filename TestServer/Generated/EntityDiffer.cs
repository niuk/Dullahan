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

                    bool anyComponentsChanged = false;
                    anyComponentsChanged = inputComponentDiffer.Diff(
                        (entity.inputComponent, oldTick),
                        (entity.inputComponent, newTick),
                        writer
                    ) || anyComponentsChanged;
                    anyComponentsChanged = positionComponentDiffer.Diff(
                        (entity.positionComponent, oldTick),
                        (entity.positionComponent, newTick),
                        writer
                    ) || anyComponentsChanged;
                    return anyComponentsChanged;
                }


                public void Patch(ref (Entity, int) entityAtTick, BinaryReader reader) {

                }
            }
        }
    }
}
