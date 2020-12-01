/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;

namespace TestServer {
    public class Entity {
        public World world { get; private set; }

        private Entity entity => this;

        public Entity(World world) {
            this.world = world;
        }

        private readonly Ring<int> inputComponent_ticks = new Ring<int>();
        private readonly Ring<TestServer.Source.Components.IInputComponent> inputComponent_states = new Ring<TestServer.Source.Components.IInputComponent>();
        private readonly Ring<Maybe<TestServer.Source.Components.IInputComponent>> inputComponent_diffs = new Ring<Maybe<TestServer.Source.Components.IInputComponent>>();
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

                var differ = new ReferenceDiffer<TestServer.Source.Components.IInputComponent>();
                for (int i = 0; i < inputComponent_states.Count; ++i) {
                    int index = inputComponent_states.Start + i;
                    if (differ.Diff(inputComponent_states[index], value, out TestServer.Source.Components.IInputComponent diff)) {
                        inputComponent_diffs[index] = new Maybe<TestServer.Source.Components.IInputComponent>.Just(diff);
                    } else {
                        inputComponent_diffs[index] = new Maybe<TestServer.Source.Components.IInputComponent>.Nothing();
                    }
                }

                inputComponent_states.PushEnd(value);
                inputComponent_ticks.PushEnd(entity.world.tick);

                while (inputComponent_diffs.Count > 0 && inputComponent_diffs.PeekEnd() == null) {
                    inputComponent_ticks.PopEnd();
                    inputComponent_states.PopEnd();
                    inputComponent_diffs.PopEnd();
                }
            }
        }

        private readonly Ring<int> positionComponent_ticks = new Ring<int>();
        private readonly Ring<TestServer.Source.Components.IPositionComponent> positionComponent_states = new Ring<TestServer.Source.Components.IPositionComponent>();
        private readonly Ring<Maybe<TestServer.Source.Components.IPositionComponent>> positionComponent_diffs = new Ring<Maybe<TestServer.Source.Components.IPositionComponent>>();
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

                var differ = new ReferenceDiffer<TestServer.Source.Components.IPositionComponent>();
                for (int i = 0; i < positionComponent_states.Count; ++i) {
                    int index = positionComponent_states.Start + i;
                    if (differ.Diff(positionComponent_states[index], value, out TestServer.Source.Components.IPositionComponent diff)) {
                        positionComponent_diffs[index] = new Maybe<TestServer.Source.Components.IPositionComponent>.Just(diff);
                    } else {
                        positionComponent_diffs[index] = new Maybe<TestServer.Source.Components.IPositionComponent>.Nothing();
                    }
                }

                positionComponent_states.PushEnd(value);
                positionComponent_ticks.PushEnd(entity.world.tick);

                while (positionComponent_diffs.Count > 0 && positionComponent_diffs.PeekEnd() == null) {
                    positionComponent_ticks.PopEnd();
                    positionComponent_states.PopEnd();
                    positionComponent_diffs.PopEnd();
                }
            }
        }

    }
}
