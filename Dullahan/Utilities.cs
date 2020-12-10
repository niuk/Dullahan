using System.Collections.Generic;
using System.IO;

namespace Dullahan {
    public static class Utilities {
        public static int GetOffset(this BinaryWriter writer) {
            long offset = writer.Seek(0, SeekOrigin.Current);
            if (offset < int.MinValue || int.MaxValue < offset) {
                throw new InternalBufferOverflowException();
            }

            return (int)offset;
        }

        public static int GetOffset(this BinaryReader reader) {
            long offset = reader.BaseStream.Position;
            if (offset < int.MinValue || int.MaxValue < offset) {
                throw new InternalBufferOverflowException();
            }

            return (int)offset;
        }

        public static void SetOffset(this BinaryWriter writer, int offset) {
            writer.Seek(offset, SeekOrigin.Begin);
        }

        public static void SetOffset(this BinaryReader reader, int offset) {
            reader.BaseStream.Position = offset;
        }

        public static TValue PeekEnd<TKey, TValue>(this SortedList<TKey, TValue> sortedList) {
            return sortedList[sortedList.Keys[sortedList.Count - 1]];
        }

        public static TValue PopEnd<TKey, TValue>(this SortedList<TKey, TValue> sortedList) {
            int end = sortedList.Count - 1;
            var item = sortedList.Values[end];
            sortedList.RemoveAt(end);
            return item;
        }
    }
}