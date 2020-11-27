using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;

namespace Dullahan.Network {
    public class Server<TServerState, TServerDiff, TClientState, TClientDiff> : IDisposable {
        private class TlsServerImplementation : DefaultTlsServer {
            protected override ProtocolVersion MinimumVersion => ProtocolVersion.DTLSv10;
            protected override ProtocolVersion MaximumVersion => ProtocolVersion.DTLSv12;

            protected override TlsSignerCredentials GetRsaSignerCredentials() {
                return Credentials.Get(mContext, mSupportedSignatureAlgorithms, "x509-server.pem", "x509-server-key.pem");
            }
        }

        private class Connection : IDisposable {
            private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            private readonly Thread thread;
            private bool disposedValue = false;

            public Connection(int port, CancellationToken serverCancellationToken) {
                thread = new Thread(() => {
                    var datagramTransport = new DatagramTransportImplementation(
                        new IPEndPoint(IPAddress.Any, port),
                        new IPEndPoint(IPAddress.Any, 0));
                    try {
                        Console.WriteLine($"Waiting for connection from {datagramTransport.RemoteEndPoint}");
                        var dtlsTransport = new DtlsServerProtocol(new SecureRandom()).Accept(new TlsServerImplementation(), datagramTransport);
                        Console.WriteLine($"Accepted connection from {datagramTransport.RemoteEndPoint}");
                        try {
                            var buffer = new byte[dtlsTransport.GetReceiveLimit()];
                            using (var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(serverCancellationToken, cancellationTokenSource.Token)) {
                                while (!linkedCancellationTokenSource.IsCancellationRequested) {
                                    int size = dtlsTransport.Receive(buffer, 0, buffer.Length, 1000);
                                    if (size > 0) {
                                        for (int i = 0; i < size; ++i) {
                                            Console.Write("{0:x2}", buffer[i]);
                                        }

                                        Console.WriteLine();
                                    }
                                }
                            }
                        } finally {
                            dtlsTransport.Close();
                        }
                    } finally {
                        datagramTransport.Close();
                    }
                });
                thread.Start();
            }

            protected virtual void Dispose(bool disposing) {
                if (!disposedValue) {
                    if (disposing) {
                        cancellationTokenSource.Cancel();
                        try {
                            thread.Join();
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

        private readonly Dictionary<int, Connection> connectionsByPort = new Dictionary<int, Connection>();
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly TServerState state;
        private bool disposedValue;

        public Server(
            TServerState state,
            IDiffer<TServerState, TServerDiff> serverStateDiffer,
            IDiffer<TClientState, TClientDiff> clientStateDiffer,
            int portStart,
            int connectionCount
        ) {
            this.state = state;
            for (int i = 0; i < connectionCount; ++i) {
                int port = portStart + i;
                connectionsByPort.Add(port, new Connection(port, cancellationTokenSource.Token));
            }
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    cancellationTokenSource.Cancel();
                    try {
                        foreach (var connection in connectionsByPort.Values) {
                            connection.Dispose();
                        }
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