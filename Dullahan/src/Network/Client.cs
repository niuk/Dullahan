using System;
using System.Net;

using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;

namespace Dullahan.Network {
    public class Client<TServerState, TServerDiff, TClientState, TClientDiff> : IDisposable {
        private class TlsClientImplementation : DefaultTlsClient {
            public override ProtocolVersion ClientVersion => ProtocolVersion.DTLSv12;
            public override ProtocolVersion MinimumVersion => ProtocolVersion.DTLSv12;

            private class TlsAuthenticationImplementation : TlsAuthentication {
                private readonly TlsContext context;

                public TlsAuthenticationImplementation(TlsContext context) {
                    this.context = context;
                }

                public TlsCredentials GetClientCredentials(CertificateRequest certificateRequest) {
                    var certificateTypes = certificateRequest.CertificateTypes;
                    if (certificateTypes != null && Arrays.Contains(certificateTypes, ClientCertificateType.rsa_sign)) {
                        return Credentials.Get(context, certificateRequest.SupportedSignatureAlgorithms, "x509-client.pem", "x509-client-key.pem");
                    }

                    return null;
                }

                public void NotifyServerCertificate(Certificate serverCertificate) {
                    // TODO
                }
            }

            public override TlsAuthentication GetAuthentication() {
                return new TlsAuthenticationImplementation(mContext);
            }
        }

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
