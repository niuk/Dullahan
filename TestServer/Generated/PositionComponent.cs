/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System.IO;
using System.Text;

namespace TestServer {
    public class PositionComponent : TestServer.Source.Components.IPositionComponent {
        public Entity entity { get; private set; }

        private readonly Ring<int> x_ticks = new Ring<int>();
        private readonly Ring<System.Int32> x_states = new Ring<System.Int32>();
        private readonly Ring<bool> x_diffs = new Ring<bool>();
        private readonly MemoryStream x_diffBuffer = new MemoryStream();
        private readonly BinaryWriter x_diffWriter;
        private readonly Dullahan.IntDiffer x_differ = new Dullahan.IntDiffer();
        public System.Int32 x {
            get {
                return x_states.PeekEnd();
            }

            set {
                if (x_ticks.Count > 0 && x_ticks.PeekEnd() == entity.world.tick) {
                    x_states.PopEnd();
                    x_ticks.PopEnd();
                }

                for (int i = 0; i < x_states.Count; ++i) {
                    int index = x_states.Start + i;
                    x_diffs[index] = x_differ.Diff(x_states[index], (System.Int32)value, x_diffWriter);
                }

                x_states.PushEnd((System.Int32)value);
                x_ticks.PushEnd(entity.world.tick);

                while (x_diffs.Count > 0 && !x_diffs.PeekEnd()) {
                    x_ticks.PopEnd();
                    x_states.PopEnd();
                    x_diffs.PopEnd();
                }
            }
        }

        private readonly Ring<int> y_ticks = new Ring<int>();
        private readonly Ring<System.Int32> y_states = new Ring<System.Int32>();
        private readonly Ring<bool> y_diffs = new Ring<bool>();
        private readonly MemoryStream y_diffBuffer = new MemoryStream();
        private readonly BinaryWriter y_diffWriter;
        private readonly Dullahan.IntDiffer y_differ = new Dullahan.IntDiffer();
        public System.Int32 y {
            get {
                return y_states.PeekEnd();
            }

            set {
                if (y_ticks.Count > 0 && y_ticks.PeekEnd() == entity.world.tick) {
                    y_states.PopEnd();
                    y_ticks.PopEnd();
                }

                for (int i = 0; i < y_states.Count; ++i) {
                    int index = y_states.Start + i;
                    y_diffs[index] = y_differ.Diff(y_states[index], (System.Int32)value, y_diffWriter);
                }

                y_states.PushEnd((System.Int32)value);
                y_ticks.PushEnd(entity.world.tick);

                while (y_diffs.Count > 0 && !y_diffs.PeekEnd()) {
                    y_ticks.PopEnd();
                    y_states.PopEnd();
                    y_diffs.PopEnd();
                }
            }
        }



        public PositionComponent(Entity entity) {
            this.entity = entity;
            entity.positionComponent = this;

            x_diffWriter = new BinaryWriter(x_diffBuffer, Encoding.UTF8, leaveOpen: true);

            y_diffWriter = new BinaryWriter(y_diffBuffer, Encoding.UTF8, leaveOpen: true);

        }
    }
}
