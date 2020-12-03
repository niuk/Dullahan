/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System.IO;
using System.Text;

namespace TestServer {
    public class InputComponent : TestServer.Source.Components.IInputComponent {
        public Entity entity { get; private set; }

        private readonly Ring<int> deltaX_ticks = new Ring<int>();
        private readonly Ring<System.Int32> deltaX_states = new Ring<System.Int32>();
        private readonly Ring<bool> deltaX_diffs = new Ring<bool>();
        private readonly MemoryStream deltaX_diffBuffer = new MemoryStream();
        private readonly BinaryWriter deltaX_diffWriter;
        private readonly Dullahan.IntDiffer deltaX_differ = new Dullahan.IntDiffer();
        public System.Int32 deltaX {
            get {
                return deltaX_states.PeekEnd();
            }

            set {
                if (deltaX_ticks.Count > 0 && deltaX_ticks.PeekEnd() == entity.world.tick) {
                    deltaX_ticks.PopEnd();
                    deltaX_states.PopEnd();
                }

                deltaX_diffWriter.SetPosition(0);
                for (int i = 0; i < deltaX_states.Count; ++i) {
                    int index = deltaX_states.Start + i;
                    deltaX_diffs[index] = deltaX_differ.Diff(deltaX_states[index], (System.Int32)value, deltaX_diffWriter);
                }

                deltaX_states.PushEnd((System.Int32)value);
                deltaX_ticks.PushEnd(entity.world.tick);

                while (deltaX_diffs.Count > 0 && !deltaX_diffs.PeekEnd()) {
                    deltaX_diffs.PopEnd();
                    deltaX_ticks.PopEnd();
                    deltaX_states.PopEnd();
                }
            }
        }

        private readonly Ring<int> deltaY_ticks = new Ring<int>();
        private readonly Ring<System.Int32> deltaY_states = new Ring<System.Int32>();
        private readonly Ring<bool> deltaY_diffs = new Ring<bool>();
        private readonly MemoryStream deltaY_diffBuffer = new MemoryStream();
        private readonly BinaryWriter deltaY_diffWriter;
        private readonly Dullahan.IntDiffer deltaY_differ = new Dullahan.IntDiffer();
        public System.Int32 deltaY {
            get {
                return deltaY_states.PeekEnd();
            }

            set {
                if (deltaY_ticks.Count > 0 && deltaY_ticks.PeekEnd() == entity.world.tick) {
                    deltaY_ticks.PopEnd();
                    deltaY_states.PopEnd();
                }

                deltaY_diffWriter.SetPosition(0);
                for (int i = 0; i < deltaY_states.Count; ++i) {
                    int index = deltaY_states.Start + i;
                    deltaY_diffs[index] = deltaY_differ.Diff(deltaY_states[index], (System.Int32)value, deltaY_diffWriter);
                }

                deltaY_states.PushEnd((System.Int32)value);
                deltaY_ticks.PushEnd(entity.world.tick);

                while (deltaY_diffs.Count > 0 && !deltaY_diffs.PeekEnd()) {
                    deltaY_diffs.PopEnd();
                    deltaY_ticks.PopEnd();
                    deltaY_states.PopEnd();
                }
            }
        }



        public InputComponent(Entity entity) {
            this.entity = entity;
            entity.inputComponent = this;

            deltaX_diffWriter = new BinaryWriter(deltaX_diffBuffer, Encoding.UTF8, leaveOpen: true);

            deltaY_diffWriter = new BinaryWriter(deltaY_diffBuffer, Encoding.UTF8, leaveOpen: true);

        }
    }
}
