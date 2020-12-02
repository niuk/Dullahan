/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;

namespace TestServer {
    public class InputComponent : TestServer.Source.Components.IInputComponent {
        public Entity entity { get; private set; }

        public InputComponent(Entity entity) {
            this.entity = entity;
            entity.inputComponent = this;
        }

        private readonly Ring<int> deltaX_ticks = new Ring<int>();
        private readonly Ring<System.Int32> deltaX_states = new Ring<System.Int32>();
        private readonly Ring<Maybe<System.Int32>> deltaX_diffs = new Ring<Maybe<System.Int32>>();
        public System.Int32 deltaX {
            get {
                return deltaX_states.PeekEnd();
            }

            set {
                if (deltaX_ticks.Count > 0 && deltaX_ticks.PeekEnd() == entity.world.tick) {
                    deltaX_states.PopEnd();
                    deltaX_ticks.PopEnd();
                }

                var differ = new PrimitiveDiffer<System.Int32>();
                for (int i = 0; i < deltaX_states.Count; ++i) {
                    int index = deltaX_states.Start + i;
                    deltaX_diffs[index] = differ.Diff(deltaX_states[index], value);
                }

                deltaX_states.PushEnd(value);
                deltaX_ticks.PushEnd(entity.world.tick);

                while (deltaX_diffs.Count > 0 && deltaX_diffs.PeekEnd() == null) {
                    deltaX_ticks.PopEnd();
                    deltaX_states.PopEnd();
                    deltaX_diffs.PopEnd();
                }
            }
        }

        private readonly Ring<int> deltaY_ticks = new Ring<int>();
        private readonly Ring<System.Int32> deltaY_states = new Ring<System.Int32>();
        private readonly Ring<Maybe<System.Int32>> deltaY_diffs = new Ring<Maybe<System.Int32>>();
        public System.Int32 deltaY {
            get {
                return deltaY_states.PeekEnd();
            }

            set {
                if (deltaY_ticks.Count > 0 && deltaY_ticks.PeekEnd() == entity.world.tick) {
                    deltaY_states.PopEnd();
                    deltaY_ticks.PopEnd();
                }

                var differ = new PrimitiveDiffer<System.Int32>();
                for (int i = 0; i < deltaY_states.Count; ++i) {
                    int index = deltaY_states.Start + i;
                    deltaY_diffs[index] = differ.Diff(deltaY_states[index], value);
                }

                deltaY_states.PushEnd(value);
                deltaY_ticks.PushEnd(entity.world.tick);

                while (deltaY_diffs.Count > 0 && deltaY_diffs.PeekEnd() == null) {
                    deltaY_ticks.PopEnd();
                    deltaY_states.PopEnd();
                    deltaY_diffs.PopEnd();
                }
            }
        }

    }
}
