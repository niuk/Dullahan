/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;

namespace TestServer {
    public class PositionComponent : TestServer.Source.Components.IPositionComponent {
        public Entity entity { get; private set; }

        public PositionComponent(Entity entity) {
            this.entity = entity;
            entity.positionComponent = this;
        }

        private readonly Ring<int> x_ticks = new Ring<int>();
        private readonly Ring<System.Int32> x_states = new Ring<System.Int32>();
        private readonly Ring<Maybe<System.Int32>> x_diffs = new Ring<Maybe<System.Int32>>();
        public System.Int32 x {
            get {
                return x_states.PeekEnd();
            }

            set {
                if (x_ticks.Count > 0 && x_ticks.PeekEnd() == entity.world.tick) {
                    x_states.PopEnd();
                    x_ticks.PopEnd();
                }

                var differ = new PrimitiveDiffer<System.Int32>();
                for (int i = 0; i < x_states.Count; ++i) {
                    int index = x_states.Start + i;
                    if (differ.Diff(x_states[index], value, out System.Int32 diff)) {
                        x_diffs[index] = new Maybe<System.Int32>.Just(diff);
                    } else {
                        x_diffs[index] = new Maybe<System.Int32>.Nothing();
                    }
                }

                x_states.PushEnd(value);
                x_ticks.PushEnd(entity.world.tick);

                while (x_diffs.Count > 0 && x_diffs.PeekEnd() == null) {
                    x_ticks.PopEnd();
                    x_states.PopEnd();
                    x_diffs.PopEnd();
                }
            }
        }

        private readonly Ring<int> y_ticks = new Ring<int>();
        private readonly Ring<System.Int32> y_states = new Ring<System.Int32>();
        private readonly Ring<Maybe<System.Int32>> y_diffs = new Ring<Maybe<System.Int32>>();
        public System.Int32 y {
            get {
                return y_states.PeekEnd();
            }

            set {
                if (y_ticks.Count > 0 && y_ticks.PeekEnd() == entity.world.tick) {
                    y_states.PopEnd();
                    y_ticks.PopEnd();
                }

                var differ = new PrimitiveDiffer<System.Int32>();
                for (int i = 0; i < y_states.Count; ++i) {
                    int index = y_states.Start + i;
                    if (differ.Diff(y_states[index], value, out System.Int32 diff)) {
                        y_diffs[index] = new Maybe<System.Int32>.Just(diff);
                    } else {
                        y_diffs[index] = new Maybe<System.Int32>.Nothing();
                    }
                }

                y_states.PushEnd(value);
                y_ticks.PushEnd(entity.world.tick);

                while (y_diffs.Count > 0 && y_diffs.PeekEnd() == null) {
                    y_ticks.PopEnd();
                    y_states.PopEnd();
                    y_diffs.PopEnd();
                }
            }
        }

    }
}
