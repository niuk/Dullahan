using Dullahan;
using Dullahan.Network;
using System;
using System.Diagnostics;
using System.Threading;

namespace TestServer {
    class Program {
        static void Main(string[] args) {
            int portStart = int.Parse(args[0]);
            int capacity = int.Parse(args[1]);
            var world = new World();
            var server = new Server<(World, int), ((int, int), int)>(
                readServerState: null,
                writeClientState: null,
                serverStateDiffer: null,
                clientStateDiffer: null,
                portStart: portStart,
                capacity: capacity,
                TimeSpan.FromSeconds(0.1));
            var stopwatch = new Stopwatch();
            var tickRate = TimeSpan.FromMilliseconds(10); // 100 ticks per second
            while (true) {
                stopwatch.Restart();

                for (int i = 0; i < capacity; ++i) {
                    world.inputSystem.inputsById[i] = server[portStart + i].Item1;
                }
                
                world.Tick();

                server.serverState = (world, world.tick);

                var elapsed = stopwatch.Elapsed;
                if (tickRate > elapsed) {
                    Thread.Sleep(tickRate - elapsed);
                }
            }
        }
    }
}
