using System;
using System.Diagnostics;
using System.IO;

namespace Dullahan {
    public class DebugBinaryReader : BinaryReader {
        private readonly string prefix;

        public DebugBinaryReader(Stream input, string prefix) : base(input) {
            this.prefix = prefix;
        }

        public override int ReadInt32() {
            var stackFrame = new StackTrace(true).GetFrame(1);
            Console.WriteLine($"{prefix}: ReadInt32({BaseStream.Position}) in {stackFrame.GetMethod()} at {stackFrame.GetFileName()}:{stackFrame.GetFileLineNumber()}");
            return base.ReadInt32();
        }

        public override float ReadSingle() {
            var stackFrame = new StackTrace(true).GetFrame(1);
            Console.WriteLine($"{prefix}: ReadSingle({BaseStream.Position}) in {stackFrame.GetMethod()} at {stackFrame.GetFileName()}:{stackFrame.GetFileLineNumber()}");
            return base.ReadSingle();
        }
    }
}
