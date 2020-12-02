using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;

namespace Dullahan.Network {
    public class Client<TLocalState, TRemoteState> : IDisposable {
        public bool Connected => connection.Connected;

        public TLocalState localState { private get; set; }
        private TLocalState lastAckedLocalState;

        public TRemoteState remoteState { get; private set; }

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly Connection connection;
        private readonly Thread sendThread;
        private bool disposedValue;

        public Client(
            Func<BinaryReader, TLocalState> readLocalState,
            Action<TRemoteState, BinaryWriter> writeRemoteState,
            IDiffer<(BinaryWriter, TLocalState), BinaryReader> localStateDiffer,
            IDiffer<(BinaryWriter, TRemoteState), BinaryReader> remoteStateDiffer,
            EndPoint localEndPoint,
            EndPoint remoteEndPoint,
            TimeSpan sendInterval
        ) {
            connection = new Connection(
                localEndPoint,
                remoteEndPoint,
                (buffer, index, size) => {
                    using (var reader = new BinaryReader(new MemoryStream(buffer, index, size, false))) {
                        lastAckedLocalState = readLocalState(reader);
                        remoteState = remoteStateDiffer.Patch((null, remoteState), reader).Item2;
                    }
                });

            sendThread = new Thread(() => {
                var memoryStream = new MemoryStream();
                var stopwatch = new Stopwatch();
                while (!cancellationTokenSource.IsCancellationRequested) {
                    stopwatch.Restart();

                    memoryStream.Position = 0;
                    using (var writer = new BinaryWriter(memoryStream)) {
                        writeRemoteState(remoteState, writer);
                        localStateDiffer.Diff((writer, localState), (writer, lastAckedLocalState));
                        if (memoryStream.Position > int.MaxValue) {
                            throw new OverflowException();
                        }

                        connection.SendMessage(memoryStream.GetBuffer(), 0, (int)memoryStream.Position);
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
