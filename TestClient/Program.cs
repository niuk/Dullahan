using Dullahan;
using Dullahan.Network;
using System.Net;
using System.Threading;

namespace TestClient {
    class Program {
        static void Main(string[] args) {
            var client = new Client<int, int, int, int>(0, new PrimitiveDiffer<int>(), new PrimitiveDiffer<int>(), new IPEndPoint(IPAddress.Loopback, 9000));
            while (!client.Connected) {
                Thread.Sleep(1000);
            }

            for (int i = 0; ; ++i) {
                var b = System.Text.Encoding.UTF8.GetBytes($"{i}: Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.");
                client.Send(b, 0, b.Length);

                Thread.Sleep(1000);
            }
        }
    }
}
