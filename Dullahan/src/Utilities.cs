using System.IO;

namespace Dullahan {
    public static class Utilities {
        public static int GetPosition(this BinaryWriter writer) {
            long position = writer.Seek(0, SeekOrigin.Current);
            if (position < int.MinValue || int.MaxValue < position) {
                throw new InternalBufferOverflowException();
            }

            return (int)position;
        }

        public static void SetPosition(this BinaryWriter writer, int position) {
            writer.Seek(position, SeekOrigin.Begin);
        }
    }
}