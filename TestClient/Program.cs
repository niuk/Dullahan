using Dullahan;
using Dullahan.Network;
using System;
using System.Net;
using System.Threading;

namespace TestClient {
    class Program {
        static void Main(string[] args) {
            var client = new Client<(int, (int, int)), (int, TestServer.World)>(
                readLocalState: null,
                writeRemoteState: null,
                localStateDiffer: null,
                remoteStateDiffer: null,
                localEndPoint: new IPEndPoint(IPAddress.Any, 0),
                remoteEndPoint: new IPEndPoint(IPAddress.Parse(args[0]), int.Parse(args[1])),
                TimeSpan.FromSeconds(0.1));

            for (int i = 0; ; ++i) {
                int deltaX = 0;
                int deltaY = 0;
                var key = Console.ReadKey(true).Key;
                switch (key) {
                    case ConsoleKey.LeftArrow:
                        deltaX = -1;
                        break;
                    case ConsoleKey.RightArrow:
                        deltaX = 1;
                        break;
                    case ConsoleKey.UpArrow:
                        deltaY = 1;
                        break;
                    case ConsoleKey.DownArrow:
                        deltaY = -1;
                        break;
                }

                client.localState = (i, (deltaX, deltaY));
                Thread.Sleep(10);
            }
        }
    }
}
