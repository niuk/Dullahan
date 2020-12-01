using Dullahan;
using Dullahan.Network;
using System.Threading;

namespace TestServer {
    class Program {
        static void Main(string[] args) {
            var server = new Server<int, int, (int, int), (int, int)>(0, new PrimitiveDiffer<int>(), new PrimitiveDiffer<(int, int)>(), 9000, 1);

            var world = new World();

            while (true) {
                //server.state = world;

                Thread.Sleep(100);
            }
        }
    }
}
