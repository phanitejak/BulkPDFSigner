using iText.Signatures;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace BulkPdfSigner;

public sealed class X509Certificate2Signature : IExternalSignature
{
    private readonly string _hashAlgorithm;
    private readonly string _encryptionAlgorithm;
    private readonly RSA? _rsa;
    private readonly DSA? _dsa;

    public X509Certificate2Signature(X509Certificate2 certificate, string hashAlgorithm)
    {
        if (!certificate.HasPrivateKey)
            throw new ArgumentException("Certificate does not have a private key.");

        _hashAlgorithm = DigestAlgorithms.GetDigest(DigestAlgorithms.GetAllowedDigest(hashAlgorithm));

        _rsa = certificate.GetRSAPrivateKey();
        if (_rsa is not null)
        {
            _encryptionAlgorithm = "RSA";
            return;
        }

        _dsa = certificate.GetDSAPrivateKey();
        if (_dsa is not null)
        {
            _encryptionAlgorithm = "DSA";
            return;
        }

        throw new ArgumentException("Unsupported private key algorithm on certificate.");
    }

    public string GetEncryptionAlgorithm() => _encryptionAlgorithm;

    public string GetHashAlgorithm() => _hashAlgorithm;

    public byte[] Sign(byte[] message)
    {
        var hash = HashData(message, _hashAlgorithm);

        if (_rsa is not null)
            return _rsa.SignHash(hash, new HashAlgorithmName(_hashAlgorithm), RSASignaturePadding.Pkcs1);

        if (_dsa is not null)
            return _dsa.CreateSignature(hash);

        throw new CryptographicException("No private key available.");
    }

    private static byte[] HashData(byte[] message, string hashAlgorithm)
    {
        using HashAlgorithm hasher = hashAlgorithm switch
        {
            "SHA1"   => SHA1.Create(),
            "SHA256" => SHA256.Create(),
            "SHA384" => SHA384.Create(),
            "SHA512" => SHA512.Create(),
            "MD5"    => MD5.Create(),
            _ => throw new InvalidOperationException("Unsupported hash algorithm: " + hashAlgorithm)
        };
        return hasher.ComputeHash(message);
    }
}
