using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

using static Dullahan.Utilities;

namespace Dullahan.Network {
    public class Client<TLocalState, TRemoteState> : IDisposable {
        public bool Connected => connection.Connected;

        public long totalBytesSent { get; private set; } = 0;
        public long totalBytesReceived { get; private set; } = 0;

        private int localTick = 0;
        public int LocalTick {
            set {
                lock (localTickMutex) {
                    localTick = value;
                }
            }
        }

        public int AckedLocalTick { get; private set; } = 0;
        // we use this mutex to guarantee that the localTick, the ackedLocalTick, and the diff we send is consistent in each message (see sendThread)
        private readonly object localTickMutex = new object();

        public int AckingRemoteTick { get; private set; } = 0;

        public IReadOnlyDictionary<int, TRemoteState> RemoteStatesByTick => remoteStatesByTick;
        private readonly ConcurrentDictionary<int, TRemoteState> remoteStatesByTick = new ConcurrentDictionary<int, TRemoteState>();
        // we use a ConcurrentDictionary because the onMessageReceived callback happens in a separate thread

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly Connection connection;
        private readonly Thread sendThread;
        private bool disposedValue;

        public Client(
            TRemoteState initialRemoteState,
            IReadOnlyDictionary<int, TLocalState> localStatesByTick,
            IDiffer<TLocalState> localStateDiffer,
            IDiffer<TRemoteState> remoteStateDiffer,
            EndPoint localEndPoint,
            EndPoint remoteEndPoint,
            TimeSpan sendInterval
        ) {
            if (!remoteStatesByTick.TryAdd(0, initialRemoteState)) {
                throw new InvalidOperationException("Could not add initial tick.");
            }

            connection = new Connection(
                localEndPoint,
                remoteEndPoint,
                (buffer, index, size) => {
                    using (var reader = new BinaryReader(new MemoryStream(buffer, index, size, writable: false))) {
                        totalBytesReceived += size;
                        AckedLocalTick = reader.ReadInt32();

                        int oldRemoteTick = reader.ReadInt32();
                        int newRemoteTick = reader.ReadInt32();

                        //Console.WriteLine($"Received: ackedLocalTick = {ackedLocalTick}, oldRemoteTick = {oldRemoteTick}, newRemoteTick = {newRemoteTick}, ackingRemoteTick = {ackingRemoteTick}");

                        // only patch remote state if newer
                        if (AckingRemoteTick < newRemoteTick) {
                            var remoteState = remoteStatesByTick[oldRemoteTick];
                            remoteStateDiffer.Patch(ref remoteState, reader);
                            if (!remoteStatesByTick.TryAdd(newRemoteTick, remoteState)) {
                                throw new InvalidOperationException($"Could not add new tick {newRemoteTick}.");
                            }

                            AckingRemoteTick = newRemoteTick;
                        }
                    }
                });

            sendThread = new Thread(() => {
                var memoryStream = new MemoryStream();
                FixedTimer(_ => {
                    if (!Connected) {
                        return;
                    }

                    using (var writer = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true)) {
                        writer.SetOffset(0);
                        writer.Write(AckingRemoteTick);

                        TLocalState ackedLocalState;
                        TLocalState localState;
                        bool ticked;
                        lock (localTickMutex) {
                            writer.Write(AckedLocalTick);
                            writer.Write(localTick);

                            //Console.WriteLine($"Sending: ackingRemoteTick = {ackingRemoteTick}, ackedLocalTick = {ackedLocalTick}, localTick = {localTick}");
                            ticked = AckedLocalTick < localTick;

                            ackedLocalState = localStatesByTick[AckedLocalTick];
                            localState = localStatesByTick[localTick];
                        }

                        if (ticked) {
                            localStateDiffer.Diff(ackedLocalState, localState, writer);
                        }

                        if (memoryStream.Position > int.MaxValue) {
                            throw new OverflowException();
                        }

                        connection.SendMessage(memoryStream.GetBuffer(), 0, writer.GetOffset());
                        totalBytesSent += writer.GetOffset();
                    }
                }, sendInterval, cancellationTokenSource.Token);
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
