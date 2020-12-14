using Dullahan.Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;

using static Dullahan.Utilities;

namespace TestGame {
    class ClientProgram {
        static void Main(string[] args) {
            var address = IPAddress.Parse(args[0]);
            var port = int.Parse(args[1]);

            var tickRate = TimeSpan.FromMilliseconds(100); // 100 ticks per second

            var world = new World();
            _ = new World.Entity.TimeComponent(new World.Entity(world)) {
                deltaTime = tickRate.TotalSeconds
            };
            _ = new World.Entity.ConsoleBufferComponent(new World.Entity(world)) {
                consoleBuffer = new byte[120, 32]
            };

            var deltasByTick = new SortedList<int, (float, float)> { { 0, (0f, 0f) } };
            var client = new Client<(float, float), (World, int)>(
                initialRemoteState: (world, 0),
                localStatesByTick: deltasByTick,
                localStateDiffer: new Vector2Differ(),
                remoteStateDiffer: new World.Differ(),
                localEndPoint: new IPEndPoint(IPAddress.Any, 0),
                remoteEndPoint: new IPEndPoint(address, port),
                sendInterval: TimeSpan.FromSeconds(0.1));

            var keyPressStopwatches = new ConcurrentDictionary<ConsoleKey, Stopwatch>();
            var keyPressThread = new Thread(() => {
                while (true) {
                    var key = Console.ReadKey(true).Key;
                    keyPressStopwatches.AddOrUpdate(
                        key,
                        key => {
                            var stopwatch = new Stopwatch();
                            stopwatch.Start();
                            return stopwatch;
                        },
                        (key, stopwatch) => {
                            stopwatch.Restart();
                            return stopwatch;
                        });
                }
            });
            keyPressThread.Start();

            FixedTimer(tick => {
                float deltaX = (
                    keyPressStopwatches.TryGetValue(ConsoleKey.LeftArrow, out Stopwatch leftArrowStopwatch) &&
                        leftArrowStopwatch.ElapsedMilliseconds < 100 ?
                            -1f : 0f) + (
                    keyPressStopwatches.TryGetValue(ConsoleKey.RightArrow, out Stopwatch rightArrowStopwatch) &&
                        rightArrowStopwatch.ElapsedMilliseconds < 100 ?
                            1f : 0f);
                float deltaY = (
                    keyPressStopwatches.TryGetValue(ConsoleKey.DownArrow, out Stopwatch downArrowStopwatch) &&
                        downArrowStopwatch.ElapsedMilliseconds < 100 ?
                            1f : 0f) + (
                    keyPressStopwatches.TryGetValue(ConsoleKey.UpArrow, out Stopwatch upArrowStopwatch) &&
                        upArrowStopwatch.ElapsedMilliseconds < 100 ?
                            -1f : 0f);

                deltasByTick.Add(tick, (deltaX, deltaY));
                client.LocalTick = tick;

                var world = (IClientWorld)client.RemoteStatesByTick[client.AckingRemoteTick].Item1;
                lock (world) {
                    world.Tick(client.AckingRemoteTick + 1);
                }
            }, tickRate, new CancellationToken());
        }
    }
}
