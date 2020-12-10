using Dullahan;
using Dullahan.Network;
using System;
using System.Diagnostics;
using System.Threading;

namespace TestGame {
    class ServerProgram {
        static void Main(string[] args) {
            int portStart = int.Parse(args[0]);
            int capacity = int.Parse(args[1]);
            var world = new World();
            var server = new Server<(World, int), byte>(
                serverStatesByTick: world,
                serverStateDiffer: new World.Differ(),
                clientStateDiffer: new ByteDiffer(),
                portStart: portStart,
                capacity: capacity,
                TimeSpan.FromSeconds(0.1));

            var inputComponents = new IInputComponent[capacity];

            var stopwatch = new Stopwatch();
            var tickRate = TimeSpan.FromMilliseconds(10); // 100 ticks per second
            for (int tick = 0; ; ++tick) {
                stopwatch.Restart();

                lock (world) {
                    for (int i = 0; i < capacity; ++i) {
                        int port = portStart + i;
                        if (server.GetClientConnected(port)) {
                            if (inputComponents[i] == default) {
                                inputComponents[i] = new World.Entity.InputComponent(new World.Entity(world));
                            }

                            var input = server.GetClientState(port);
                            unchecked {
                                inputComponents[i].deltaX = 0xf & input >> 4;
                                inputComponents[i].deltaY = 0xf & input;
                            }
                        } else {
                            if (inputComponents[i] != default) {
                                inputComponents[i].Dispose();
                                inputComponents[i] = default;
                            }
                        }
                    }
                }
                
                ((IServerWorld)world).Tick(tick, tick + 1);

                server.serverTick = tick + 1;

                var elapsed = stopwatch.Elapsed;
                if (tickRate > elapsed) {
                    Thread.Sleep(tickRate - elapsed);
                }
            }
        }
    }
}
