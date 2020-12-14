using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Dullahan {
    public class DebugBinaryWriter : BinaryWriter {
        private readonly string prefix;
        public DebugBinaryWriter(Stream output, Encoding encoding, bool leaveOpen, string prefix) : base(output, encoding, leaveOpen) {
            this.prefix = prefix;
        }

        public override void Write(byte[] buffer, int index, int count) {
            var stackFrame = new StackTrace(true).GetFrame(1);
            Console.WriteLine($"{prefix}: Write(byte[], {index}, {count}, {BaseStream.Position}) in {stackFrame.GetMethod()} at {stackFrame.GetFileName()}:{stackFrame.GetFileLineNumber()}");
            base.Write(buffer, index, count);
        }

        public override void Write(int value) {
            var stackFrame = new StackTrace(true).GetFrame(1);
            Console.WriteLine($"{prefix}: Write((int){value}, {BaseStream.Position}) in {stackFrame.GetMethod()} at {stackFrame.GetFileName()}:{stackFrame.GetFileLineNumber()}");
            base.Write(value);
        }

        public override void Write(float value) {
            var stackFrame = new StackTrace(true).GetFrame(1);
            Console.WriteLine($"{prefix}: Write((float){value}, {BaseStream.Position}) in {stackFrame.GetMethod()} at {stackFrame.GetFileName()}:{stackFrame.GetFileLineNumber()}");
            base.Write(value);
        }
    }
}
