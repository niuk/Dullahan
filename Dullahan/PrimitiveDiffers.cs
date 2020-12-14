using System.IO;

namespace Dullahan {
    public class ByteDiffer : IDiffer<byte> {
        public bool Diff(byte oldItem, byte newItem, BinaryWriter writer) {
            writer.Write(newItem);
            return oldItem != newItem;
        }

        public void Patch(ref byte item, BinaryReader reader) {
            item = reader.ReadByte();
        }
    }

    public class IntDiffer : IDiffer<int> {
        public bool Diff(int oldItem, int newItem, BinaryWriter writer) {
            writer.Write(newItem);
            return oldItem != newItem;
        }

        public void Patch(ref int item, BinaryReader reader) {
            item = reader.ReadInt32();
        }
    }

    public class FloatDiffer : IDiffer<float> {
        public bool Diff(float oldItem, float newItem, BinaryWriter writer) {
            writer.Write(newItem);
            return oldItem != newItem;
        }

        public void Patch(ref float item, BinaryReader reader) {
            item = reader.ReadSingle();
        }
    }

    public class DoubleDiffer : IDiffer<double> {
        public bool Diff(double oldItem, double newItem, BinaryWriter writer) {
            writer.Write(newItem);
            return oldItem != newItem;
        }

        public void Patch(ref double item, BinaryReader reader) {
            item = reader.ReadDouble();
        }
    }

    public class CharDiffer : IDiffer<char> {
        public bool Diff(char oldItem, char newItem, BinaryWriter writer) {
            writer.Write(newItem);
            return oldItem != newItem;
        }

        public void Patch(ref char item, BinaryReader reader) {
            item = reader.ReadChar();
        }
    }
}