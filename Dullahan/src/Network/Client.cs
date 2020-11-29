using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;
using System;
using System.Net;
using System.Threading;

namespace Dullahan.Network {
    public class Client<TServerState, TServerDiff, TClientState, TClientDiff> : IDisposable {
        public bool Connected => connection.Connected;

        public TClientState state;

        private readonly Connection connection;
        private bool disposedValue;

        public Client(
            TClientState state,
            IDiffer<TServerState, TServerDiff> serverStateDiffer,
            IDiffer<TClientState, TClientDiff> clientStateDiffer,
            EndPoint remoteEndPoint
        ) {
            this.state = state;
            connection = new Connection(
                () => new DatagramTransportImplementation(new IPEndPoint(IPAddress.Any, 0), remoteEndPoint),
                datagramTransport => new DtlsClientProtocol(new SecureRandom()).Connect(new TlsClientImplementation(), datagramTransport),
                (buffer, start, count) => Console.WriteLine(BitConverter.ToString(buffer, start, count)),
                CancellationToken.None);
        }

        public void Send(byte[] buffer, int offset, int length) {
            connection.Send(buffer, offset, length);
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    connection.Dispose();
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
