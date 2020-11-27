using System;
using System.Net;
using System.Threading;

using Dullahan;
using Dullahan.Network;

namespace Tests {
    class Program {
        static void Main(string[] args) {
            var server = new Server<int, int, int, int>(0, new PrimitiveDiffer<int>(), new PrimitiveDiffer<int>(), 9000, 1);

            Thread.Sleep(1000);

            var client = new Client<int, int, int, int>(0, new PrimitiveDiffer<int>(), new PrimitiveDiffer<int>(), 8000, new IPEndPoint(IPAddress.Loopback, 9000));

            while (true) {
                Thread.Sleep(1000);

                var message = BitConverter.GetBytes(0xdeadbeef);
                client.Send(message, 0, message.Length);
            }
        }
    }
}
