using Dullahan;
using Dullahan.Network;
using System;
using System.Diagnostics;
using System.Threading;

using static Dullahan.Utilities;

namespace TestGame {
    class ServerProgram {
        static void Main(string[] args) {
            int portStart = int.Parse(args[0]);
            int capacity = int.Parse(args[1]);

            var tickRate = TimeSpan.FromMilliseconds(10); // 100 ticks per second

            var world = new World();
            var timeComponent = new World.Entity.TimeComponent(new World.Entity(world)) {
                deltaTime = tickRate.TotalSeconds
            };
            _ = new World.Entity.ConsoleBufferComponent(new World.Entity(world)) {
                consoleBuffer = new byte[120, 32]
            };

            var server = new Server<(World, int), (float, float)>(
                initialClientState: (0f, 0f),
                serverStatesByTick: world,
                serverStateDiffer: new World.Differ(),
                clientStateDiffer: new Vector2Differ(),
                portStart: portStart,
                capacity: capacity,
                sendInterval: TimeSpan.FromSeconds(0.1));

            var velocityComponents = new IVelocityComponent[capacity];
            var positionComponents = new IPositionComponent[capacity];
            var viewComponents = new IViewComponent[capacity];

            FixedTimer(tick => {
                lock (world) {
                    for (int i = 0; i < capacity; ++i) {
                        int port = portStart + i;

                        if (server.GetClientConnected(port)) {
                            if (velocityComponents[i] == null) {
                                var entity = new World.Entity(world);
                                velocityComponents[i] = new World.Entity.VelocityComponent(entity) {
                                    speed = 10f
                                };
                                positionComponents[i] = new World.Entity.PositionComponent(entity) {
                                    x = Console.WindowWidth / 2,
                                    y = Console.WindowHeight / 2
                                };
                                viewComponents[i] = new World.Entity.ViewComponent(entity) {
                                    avatar = new[] { '@', '#', '$', '%' }[i]
                                };
                            }

                            var input = server.GetClientState(port);
                            velocityComponents[i].deltaX = input.Item1;
                            velocityComponents[i].deltaY = input.Item2;

                            Console.SetCursorPosition(0, i * 2);
                            Console.Write($@"velocity: ({velocityComponents[i].deltaX}, {velocityComponents[i].deltaY})
position: ({positionComponents[i].x}, {positionComponents[i].y})");
                        } else {
                            if (velocityComponents[i] != null) {
                                velocityComponents[i].Dispose();
                                velocityComponents[i] = null;
                            }

                            if (positionComponents[i] != null) {
                                positionComponents[i].Dispose();
                                positionComponents[i] = null;
                            }

                            if (viewComponents[i] != null) {
                                viewComponents[i].Dispose();
                                viewComponents[i] = null;
                            }
                        }
                    }
                    
                    ((IServerWorld)world).Tick(tick);
                    server.serverTick = tick;
                }
            }, tickRate, new CancellationToken());
        }
    }
}
