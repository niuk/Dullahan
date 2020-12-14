using Dullahan;
using System.IO;

namespace TestGame {
    public class Vector2Differ : IDiffer<(float, float)> {
        public bool Diff((float, float) oldItem, (float, float) newItem, BinaryWriter writer) {
            writer.Write(newItem.Item1 - oldItem.Item1);
            writer.Write(newItem.Item2 - oldItem.Item2);
            return oldItem.Item1 != newItem.Item1 || oldItem.Item2 != newItem.Item2;
        }

        public void Patch(ref (float, float) item, BinaryReader reader) {
            float deltaItem1 = reader.ReadSingle();
            item.Item1 += deltaItem1;
            float deltaItem2 = reader.ReadSingle();
            item.Item2 += deltaItem2;
        }
    }
}
