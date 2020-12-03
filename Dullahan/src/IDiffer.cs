using System.IO;

namespace Dullahan {
    public interface IDiffer<T> {
        bool Diff(T oldItem, T newItem, BinaryWriter writer);
        void Patch(ref T item, BinaryReader reader); // ref parameter allows us to update structs in place
    }
}