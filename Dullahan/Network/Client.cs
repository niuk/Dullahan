using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Dullahan.Network {
    public class Client<TLocalState, TRemoteState> : IDisposable {
        public bool Connected => connection.Connected;

        private int localTick = 0;
        public int LocalTick {
            set {
                lock (localTickMutex) {
                    localTick = value;
                }
            }
        }

        public int AckedLocalTick { get; private set; }
        // we use this mutex to guarantee that the localTick, the ackedLocalTick, and the diff we send is consistent in each message (see sendThread)
        private readonly object localTickMutex = new object();

        public int AckingRemoteTick { get; private set; }

        public IReadOnlyDictionary<int, TRemoteState> RemoteStatesByTick => remoteStatesByTick;
        private readonly ConcurrentDictionary<int, TRemoteState> remoteStatesByTick = new ConcurrentDictionary<int, TRemoteState>();
        // we use a ConcurrentDictionary because the onMessageReceived callback happens in a separate thread

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly Connection connection;
        private readonly Thread sendThread;
        private bool disposedValue;

        public Client(
            IReadOnlyDictionary<int, TLocalState> localStatesByTick,
            IDiffer<TLocalState> localStateDiffer,
            IDiffer<TRemoteState> remoteStateDiffer,
            EndPoint localEndPoint,
            EndPoint remoteEndPoint,
            TimeSpan sendInterval
        ) {
            connection = new Connection(
                localEndPoint,
                remoteEndPoint,
                (buffer, index, size) => {
                    Console.WriteLine($"\tReceived {size} bytes in a message from {remoteEndPoint}.");
                    using (var reader = new BinaryReader(new MemoryStream(buffer, index, size, writable: false))) {
                        AckedLocalTick = reader.ReadInt32();
                        Console.WriteLine($"\t{nameof(AckingRemoteTick)} -> {reader.GetOffset()}");

                        int oldRemoteTick = reader.ReadInt32();
                        Console.WriteLine($"\t{nameof(oldRemoteTick)} -> {reader.GetOffset()}");
                        int newRemoteTick = reader.ReadInt32();
                        Console.WriteLine($"\t{nameof(newRemoteTick)} -> {reader.GetOffset()}");

                        //Console.WriteLine($"Received: ackedLocalTick = {ackedLocalTick}, oldRemoteTick = {oldRemoteTick}, newRemoteTick = {newRemoteTick}, ackingRemoteTick = {ackingRemoteTick}");

                        // only patch remote state if newer
                        if (AckingRemoteTick < newRemoteTick) {
                            remoteStatesByTick.TryGetValue(oldRemoteTick, out TRemoteState remoteState);
                            remoteStateDiffer.Patch(ref remoteState, reader);
                            Console.WriteLine($"\t{nameof(remoteStateDiffer)} -> {reader.GetOffset()}");
                            remoteStatesByTick.TryAdd(newRemoteTick, remoteState);
                            AckingRemoteTick = newRemoteTick;
                        }
                    }
                });

            sendThread = new Thread(() => {
                var memoryStream = new MemoryStream();
                var stopwatch = new Stopwatch();
                while (!cancellationTokenSource.IsCancellationRequested) {
                    stopwatch.Restart();

                    if (connection.Connected) {
                        using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true)) {
                            writer.SetOffset(0);
                            writer.Write(AckingRemoteTick);
                            Console.WriteLine($"{nameof(AckingRemoteTick)} -> {writer.GetOffset()}");

                            TLocalState ackedLocalState;
                            TLocalState localState;
                            lock (localTickMutex) {
                                writer.Write(AckedLocalTick);
                                Console.WriteLine($"{nameof(AckedLocalTick)} -> {writer.GetOffset()}");
                                writer.Write(localTick);
                                Console.WriteLine($"{nameof(localTick)} -> {writer.GetOffset()}");

                                //Console.WriteLine($"Sending: ackingRemoteTick = {ackingRemoteTick}, ackedLocalTick = {ackedLocalTick}, localTick = {localTick}");

                                ackedLocalState = localStatesByTick[AckedLocalTick];
                                localState = localStatesByTick[localTick];
                            }

                            localStateDiffer.Diff(ackedLocalState, localState, writer);
                            Console.WriteLine($"{nameof(localStateDiffer)} -> {writer.GetOffset()}");

                            if (memoryStream.Position > int.MaxValue) {
                                throw new OverflowException();
                            }

                            connection.SendMessage(memoryStream.GetBuffer(), 0, writer.GetOffset());
                            Console.WriteLine($"Sent {writer.GetOffset()} bytes in a message to {remoteEndPoint}.");
                        }
                    }

                    var elapsed = stopwatch.Elapsed;
                    if (sendInterval > elapsed) {
                        Thread.Sleep(sendInterval - elapsed);
                    }
                }
            });
            sendThread.Start();
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    connection.Dispose();

                    cancellationTokenSource.Cancel();
                    try {
                        sendThread.Join();
                    } finally {
                        cancellationTokenSource.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
