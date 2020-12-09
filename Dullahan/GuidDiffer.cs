using System;
using System.IO;

namespace Dullahan {
    public class GuidDiffer : IDiffer<Guid> {
        public bool Diff(Guid oldItem, Guid newItem, BinaryWriter writer) {
            if (oldItem != newItem) {
                writer.Write(newItem.ToByteArray());
                return true;
            } else {
                return false;
            }
        }

        public void Patch(ref Guid item, BinaryReader reader) {
            item = new Guid(reader.ReadBytes(16));
        }
    }
}
