using Dullahan;
using Dullahan.Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace TestGame {
    class ClientProgram {
        static void Main(string[] args) {
            var deltasByTick = new SortedList<int, byte>();
            var client = new Client<byte, (World, int)>(
                localStatesByTick: deltasByTick,
                localStateDiffer: new ByteDiffer(),
                remoteStateDiffer: new World.Differ(),
                localEndPoint: new IPEndPoint(IPAddress.Any, 0),
                remoteEndPoint: new IPEndPoint(IPAddress.Parse(args[0]), int.Parse(args[1])),
                TimeSpan.FromSeconds(0.1));

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

            var stopwatch = new Stopwatch();
            var tickRate = TimeSpan.FromMilliseconds(10); // 100 ticks per second
            for (int i = 0; ; ++i) {
                stopwatch.Restart();

                int deltaX = (
                    keyPressStopwatches.TryGetValue(ConsoleKey.LeftArrow, out Stopwatch leftArrowStopwatch) &&
                        leftArrowStopwatch.ElapsedMilliseconds < 100 ?
                            -1 : 0) + (
                    keyPressStopwatches.TryGetValue(ConsoleKey.RightArrow, out Stopwatch rightArrowStopwatch) &&
                        rightArrowStopwatch.ElapsedMilliseconds < 100 ?
                            1 : 0);
                int deltaY = (
                    keyPressStopwatches.TryGetValue(ConsoleKey.DownArrow, out Stopwatch downArrowStopwatch) &&
                        downArrowStopwatch.ElapsedMilliseconds < 100 ?
                            -1 : 0) + (
                    keyPressStopwatches.TryGetValue(ConsoleKey.UpArrow, out Stopwatch upArrowStopwatch) &&
                        upArrowStopwatch.ElapsedMilliseconds < 100 ?
                            1 : 0);

                deltasByTick.Add(i, (byte)((deltaX << 4) & 0xf0 | deltaY & 0xf));
                client.LocalTick = i;

                if (client.RemoteStatesByTick.TryGetValue(client.AckingRemoteTick, out (World, int) worldAtTick)) {
                    ((IClientWorld)worldAtTick.Item1).Tick(client.AckingRemoteTick, client.AckingRemoteTick + 1);
                }

                var elapsed = stopwatch.Elapsed;
                if (tickRate > elapsed) {
                    Thread.Sleep(tickRate - elapsed);
                }
            }
        }
    }
}
