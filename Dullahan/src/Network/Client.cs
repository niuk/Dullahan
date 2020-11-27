using System;
using System.Net;

using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;

namespace Dullahan.Network {
    public class Client<TServerState, TServerDiff, TClientState, TClientDiff> : IDisposable {
        private readonly DtlsTransport dtlsTransport;
        private readonly DatagramTransportImplementation datagramTransport;

        private TClientState state;
        private bool disposedValue;

        public Client(
            TClientState state,
            IDiffer<TServerState, TServerDiff> serverStateDiffer,
            IDiffer<TClientState, TClientDiff> clientStateDiffer,
            int localPort,
            EndPoint remoteEndPoint
        ) {
            this.state = state;

            datagramTransport = new DatagramTransportImplementation(
                new IPEndPoint(IPAddress.Any, localPort),
                remoteEndPoint);

            Console.WriteLine($"Connecting to {datagramTransport.RemoteEndPoint}");
            dtlsTransport = new DtlsClientProtocol(new SecureRandom()).Connect(new TlsClientImplementation(), datagramTransport);
            Console.WriteLine($"Connected to {datagramTransport.RemoteEndPoint}");
        }

        public void Send(byte[] buffer, int offset, int length) {
            dtlsTransport.Send(buffer, offset, length);
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    dtlsTransport.Close();
                    datagramTransport.Close();
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
