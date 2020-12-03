/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using System;
using System.IO;
using System.Text;
using Dullahan;

namespace TestServer {
    public class Entity {
        public readonly Guid id = Guid.NewGuid();

        public readonly World world;
        public readonly int constructionTick;
        public int disposalTick { get; private set; }

        private Entity entity => this;

        private readonly Ring<int> inputComponent_ticks = new Ring<int>();
        private readonly Ring<InputComponent> inputComponent_states = new Ring<InputComponent>();
        private readonly Ring<bool> inputComponent_diffs = new Ring<bool>();
        private readonly MemoryStream inputComponent_diffBuffer = new MemoryStream();
        private readonly BinaryWriter inputComponent_diffWriter;
        private readonly InputComponentDiffer inputComponent_differ = new InputComponentDiffer();
        public TestServer.Source.Components.IInputComponent inputComponent {
            get {
                return inputComponent_states.PeekEnd();
            }

            set {
                if (value != null) {
                    if (entity.positionComponent != null) {
                        ((MovementSystem_Implementation)entity.world.movementSystem).controllables_collection.Add(entity);
                    }
                } else {
                    ((MovementSystem_Implementation)entity.world.movementSystem).controllables_collection.Remove(entity);
                }

                if (inputComponent_ticks.Count > 0 && inputComponent_ticks.PeekEnd() == entity.world.tick) {
                    inputComponent_states.PopEnd();
                    inputComponent_ticks.PopEnd();
                }

                for (int i = 0; i < inputComponent_states.Count; ++i) {
                    int index = inputComponent_states.Start + i;
                    inputComponent_diffs[index] = inputComponent_differ.Diff(inputComponent_states[index], (InputComponent)value, inputComponent_diffWriter);
                }

                inputComponent_states.PushEnd((InputComponent)value);
                inputComponent_ticks.PushEnd(entity.world.tick);

                while (inputComponent_diffs.Count > 0 && !inputComponent_diffs.PeekEnd()) {
                    inputComponent_ticks.PopEnd();
                    inputComponent_states.PopEnd();
                    inputComponent_diffs.PopEnd();
                }
            }
        }

        private readonly Ring<int> positionComponent_ticks = new Ring<int>();
        private readonly Ring<PositionComponent> positionComponent_states = new Ring<PositionComponent>();
        private readonly Ring<bool> positionComponent_diffs = new Ring<bool>();
        private readonly MemoryStream positionComponent_diffBuffer = new MemoryStream();
        private readonly BinaryWriter positionComponent_diffWriter;
        private readonly PositionComponentDiffer positionComponent_differ = new PositionComponentDiffer();
        public TestServer.Source.Components.IPositionComponent positionComponent {
            get {
                return positionComponent_states.PeekEnd();
            }

            set {
                if (value != null) {
                    if (entity.inputComponent != null) {
                        ((MovementSystem_Implementation)entity.world.movementSystem).controllables_collection.Add(entity);
                    }
                } else {
                    ((MovementSystem_Implementation)entity.world.movementSystem).controllables_collection.Remove(entity);
                }

                if (positionComponent_ticks.Count > 0 && positionComponent_ticks.PeekEnd() == entity.world.tick) {
                    positionComponent_states.PopEnd();
                    positionComponent_ticks.PopEnd();
                }

                for (int i = 0; i < positionComponent_states.Count; ++i) {
                    int index = positionComponent_states.Start + i;
                    positionComponent_diffs[index] = positionComponent_differ.Diff(positionComponent_states[index], (PositionComponent)value, positionComponent_diffWriter);
                }

                positionComponent_states.PushEnd((PositionComponent)value);
                positionComponent_ticks.PushEnd(entity.world.tick);

                while (positionComponent_diffs.Count > 0 && !positionComponent_diffs.PeekEnd()) {
                    positionComponent_ticks.PopEnd();
                    positionComponent_states.PopEnd();
                    positionComponent_diffs.PopEnd();
                }
            }
        }


        public Entity(World world) {
            this.world = world;
            constructionTick = world.tick;
            disposalTick = int.MaxValue;
            world.entitiesById.Add(id, this);

            inputComponent_diffWriter = new BinaryWriter(inputComponent_diffBuffer, Encoding.UTF8, leaveOpen: true);

            positionComponent_diffWriter = new BinaryWriter(positionComponent_diffBuffer, Encoding.UTF8, leaveOpen: true);

        }
    }
}
