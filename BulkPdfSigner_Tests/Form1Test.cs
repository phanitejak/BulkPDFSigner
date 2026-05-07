using System.Security.Cryptography.X509Certificates;
using Xunit;
using iText.Kernel.Pdf;
using BulkPdfSigner;

namespace BulkPdfSigner_Tests
{
    public class PdfSigningServiceTest
    {
        [Fact]
        public void Sign_ValidPdfAndCert_ProducesSignedFile()
        {
            string tempSource = Path.GetTempFileName();
            string tempDest = Path.GetTempFileName();

            using (var writer = new PdfWriter(tempSource))
            {
                var pdf = new PdfDocument(writer);
                pdf.AddNewPage();
                pdf.Close();
            }

            var cert = CreateSelfSignedRsaCertificate();

            PdfSigningService.Sign(tempSource, tempDest, cert, lastPage: false);

            Assert.True(File.Exists(tempDest));
            Assert.True(new FileInfo(tempDest).Length > 0);

            File.Delete(tempSource);
            File.Delete(tempDest);
        }

        [Fact]
        public void Sign_InvalidSource_Throws()
        {
            string invalidSource = "nonexistent.pdf";
            string tempDest = Path.GetTempFileName();
            var cert = CreateSelfSignedRsaCertificate();

            Assert.ThrowsAny<Exception>(() =>
                PdfSigningService.Sign(invalidSource, tempDest, cert, lastPage: false));

            if (File.Exists(tempDest)) File.Delete(tempDest);
        }

        private static X509Certificate2 CreateSelfSignedRsaCertificate()
        {
            using var rsa = System.Security.Cryptography.RSA.Create(2048);
            var req = new CertificateRequest(
                "cn=Test",
                rsa,
                System.Security.Cryptography.HashAlgorithmName.SHA256,
                System.Security.Cryptography.RSASignaturePadding.Pkcs1);
            return req.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
        }
    }
}
