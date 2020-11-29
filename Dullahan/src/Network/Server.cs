using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Dullahan.Network {
    public class Server<TServerState, TServerDiff, TClientState, TClientDiff> : IDisposable {
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
                connectionsByPort.Add(port, new Connection(
                    () => new DatagramTransportImplementation(new IPEndPoint(IPAddress.Any, port), new IPEndPoint(IPAddress.Any, 0)),
                    datagramTransport => new DtlsServerProtocol(new SecureRandom()).Accept(new TlsServerImplementation(), datagramTransport),
                    (buffer, start, count) => Console.WriteLine(BitConverter.ToString(buffer, start, count)),
                    cancellationTokenSource.Token));
            }
        }

        public void Send(byte[] buffer, int offset, int length) {
            foreach (var connection in connectionsByPort.Values) {
                connection.Send(buffer, offset, length);
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