using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Utilities;

namespace Dullahan.Network {
    public class TlsClientImplementation : DefaultTlsClient {
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
}