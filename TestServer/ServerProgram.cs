using Dullahan;
using Dullahan.Network;
using System;
using System.Diagnostics;
using System.Threading;

namespace TestServer {
    class ServerProgram {
        static void Main(string[] args) {
            int portStart = int.Parse(args[0]);
            int capacity = int.Parse(args[1]);
            var world = new World();
            var server = new Server<(World, int), byte>(
                serverStatesByTick: world,
                serverStateDiffer: new WorldDiffer(),
                clientStateDiffer: new ByteDiffer(),
                portStart: portStart,
                capacity: capacity,
                TimeSpan.FromSeconds(0.1));

            var stopwatch = new Stopwatch();
            var tickRate = TimeSpan.FromMilliseconds(10); // 100 ticks per second
            while (true) {
                stopwatch.Restart();

                for (int i = 0; i < capacity; ++i) {
                    if (server.GetClientConnected(portStart + i)) {
                        var input = server.GetClientState(portStart + i);

                        unchecked {
                            int deltaX = 0xf & input >> 4;
                            int deltaY = 0xf & input;
                            world.inputSystem.inputsById[i] = (
                                (deltaX & 0x8) != 0 ? (int)(0xfffffff0 | deltaX) : deltaX,
                                (deltaY & 0x8) != 0 ? (int)(0xfffffff0 | deltaY) : deltaY);
                        }

                        Console.Clear();
                        Console.SetCursorPosition(0, 0);
                        Console.Write($"{server.GetClientTick(portStart + i)}:\t{world.inputSystem.inputsById[i]}");
                    }
                }
                
                world.Tick();

                server.serverTick = world.tick;

                var elapsed = stopwatch.Elapsed;
                if (tickRate > elapsed) {
                    Thread.Sleep(tickRate - elapsed);
                }
            }
        }
    }
}
