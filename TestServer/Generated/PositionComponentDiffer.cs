/* THIS IS A GENERATED FILE. DO NOT EDIT. */
using Dullahan;
using System.IO;

namespace TestServer {
    public class PositionComponentDiffer : IDiffer<(PositionComponent, int)> {


        public bool Diff((PositionComponent, int) componentAtOldTick, (PositionComponent, int) componentAtNewTick, BinaryWriter writer) {
            return true;

        }

        public void Patch(ref (PositionComponent, int) component, BinaryReader reader) {

        }
    }
}
