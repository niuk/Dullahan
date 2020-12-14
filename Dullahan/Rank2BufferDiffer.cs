using Dullahan;
using System.IO;

namespace Dullahan {
    public class Rank2BufferDiffer : IDiffer<byte[,]> {
        public bool Diff(byte[,] oldItem, byte[,] newItem, BinaryWriter writer) {
            return false; // TODO
        }

        public void Patch(ref byte[,] item, BinaryReader reader) {
            // TODO
        }
    }
}
