using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;
using System;
using System.Net;
using System.Threading;

namespace Dullahan.Network {
    public class Connection : IDisposable {
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
}
