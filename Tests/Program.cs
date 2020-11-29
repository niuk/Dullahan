using System;
using System.Net;
using System.Threading;

using Dullahan;
using Dullahan.Network;

namespace Tests {
    class Program {
        static void Main(string[] args) {
            var server = new Server<int, int, int, int>(0, new PrimitiveDiffer<int>(), new PrimitiveDiffer<int>(), 9000, 1);

            var client = new Client<int, int, int, int>(0, new PrimitiveDiffer<int>(), new PrimitiveDiffer<int>(), new IPEndPoint(IPAddress.Loopback, 9000));

            var random = new Random();
            while (true) {
                if (client.Connected) {
                    var buffer = new byte[random.Next(0, 15000)];
                    random.NextBytes(buffer);
                    client.Send(buffer, 0, buffer.Length);
                }

                Thread.Sleep(1000);
            }
        }
    }
}
