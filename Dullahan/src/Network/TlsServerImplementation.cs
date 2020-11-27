using Org.BouncyCastle.Crypto.Tls;

namespace Dullahan.Network {
    public class TlsServerImplementation : DefaultTlsServer {
        protected override ProtocolVersion MinimumVersion => ProtocolVersion.DTLSv10;
        protected override ProtocolVersion MaximumVersion => ProtocolVersion.DTLSv12;

        protected override TlsSignerCredentials GetRsaSignerCredentials() {
            return Credentials.Get(mContext, mSupportedSignatureAlgorithms, "x509-server.pem", "x509-server-key.pem");
        }
    }
}
