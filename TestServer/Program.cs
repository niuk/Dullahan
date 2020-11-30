using Dullahan;
using Dullahan.Network;
using System.Threading;

namespace TestServer {
    class Program {
        static void Main(string[] args) {
            new Server<int, int, int, int>(0, new PrimitiveDiffer<int>(), new PrimitiveDiffer<int>(), 9000, 1);
            while (true) {
                Thread.Sleep(1000);
            }
        }
    }
}
