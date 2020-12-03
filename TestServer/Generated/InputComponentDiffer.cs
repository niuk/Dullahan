/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System.IO;

namespace TestServer {
    public class InputComponentDiffer : IDiffer<(InputComponent, int)> {


        public bool Diff((InputComponent, int) componentAtOldTick, (InputComponent, int) componentAtNewTick, BinaryWriter writer) {
            return true;

        }

        public void Patch(ref (InputComponent, int) component, BinaryReader reader) {

        }
    }
}
