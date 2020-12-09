using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.Collections;
using System.IO;

namespace Dullahan.Network {
    public static class Credentials {
        public static TlsSignerCredentials Get(
            TlsContext tlsContext,
            IList supportedSignatureAlgorithms,
            string certificateResource,
            string keyResource
        ) {
            foreach (SignatureAndHashAlgorithm signatureAndHashAlgorithm in supportedSignatureAlgorithms) {
                if (signatureAndHashAlgorithm.Signature == SignatureAlgorithm.rsa) {
                    var certificateResources = new[] { certificateResource, "x509-ca.pem" };
                    var certificateStructures = new X509CertificateStructure[certificateResources.Length];
                    for (int i = 0; i < certificateResources.Length; ++i) {
                        using (var stream = new StreamReader(certificateResources[i])) {
                            var certificatePemObject = new PemReader(stream).ReadPemObject();
                            if (certificatePemObject.Type.EndsWith("CERTIFICATE")) {
                                certificateStructures[i] = X509CertificateStructure.GetInstance(certificatePemObject.Content);
                            }
                        }
                    }

                    var certificate = new Certificate(certificateStructures);

                    AsymmetricKeyParameter privateKey;
                    using (var stream = new StreamReader(keyResource)) {
                        var privateKeyPemObject = new PemReader(stream).ReadPemObject();
                        if (privateKeyPemObject.Type.EndsWith("RSA PRIVATE KEY")) {
                            var rsa = RsaPrivateKeyStructure.GetInstance(privateKeyPemObject.Content);
                            privateKey = new RsaPrivateCrtKeyParameters(
                                rsa.Modulus,
                                rsa.PublicExponent,
                                rsa.PrivateExponent,
                                rsa.Prime1,
                                rsa.Prime2,
                                rsa.Exponent1,
                                rsa.Exponent2,
                                rsa.Coefficient);
                        } else if (privateKeyPemObject.Type.EndsWith("PRIVATE KEY")) {
                            privateKey = PrivateKeyFactory.CreateKey(privateKeyPemObject.Content);
                        } else {
                            throw new InvalidOperationException($"\"{keyResource}\" doesn't specify a valid private key");
                        }
                    }

                    return new DefaultTlsSignerCredentials(tlsContext, certificate, privateKey, signatureAndHashAlgorithm);
                }
            }

            return null;
        }
    }
}