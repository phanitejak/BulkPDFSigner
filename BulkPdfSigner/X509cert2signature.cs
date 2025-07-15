using iText.Signatures;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace BulkPdfSigner
{
    public class X509Certificate2Signature : IExternalSignature
    {
        private readonly X509Certificate2 certificate;
        private readonly string hashAlgorithm;
        private readonly string encryptionAlgorithm;

        public X509Certificate2Signature(X509Certificate2 certificate, string hashAlgorithm)
        {
            if (!certificate.HasPrivateKey)
                throw new ArgumentException("Certificate does not have a private key.");

            this.certificate = certificate;
            this.hashAlgorithm = DigestAlgorithms.GetDigest(DigestAlgorithms.GetAllowedDigest(hashAlgorithm));

            if (certificate.PrivateKey is RSA)
                encryptionAlgorithm = "RSA";
            else if (certificate.PrivateKey is DSA)
                encryptionAlgorithm = "DSA";
            else
                throw new ArgumentException("Unsupported private key algorithm: " + certificate.PrivateKey.GetType().Name);
        }

        public string GetEncryptionAlgorithm() => encryptionAlgorithm;

        public string GetHashAlgorithm() => hashAlgorithm;

        public byte[] Sign(byte[] message)
        {
            if (certificate.PrivateKey is RSA rsa)
            {
                var hash = HashData(message, hashAlgorithm);
                return rsa.SignHash(hash, new HashAlgorithmName(hashAlgorithm), RSASignaturePadding.Pkcs1);
            }
            else if (certificate.PrivateKey is DSA dsa)
            {
                var hash = HashData(message, hashAlgorithm);
                return dsa.CreateSignature(hash);
            }
            else
            {
                throw new CryptographicException("Unsupported key type");
            }
        }

        private static byte[] HashData(byte[] message, string hashAlgorithm)
        {
            using HashAlgorithm hasher = HashAlgorithm.Create(hashAlgorithm) ?? throw new InvalidOperationException("Unsupported hash algorithm");
            return hasher.ComputeHash(message);
        }
    }
}
