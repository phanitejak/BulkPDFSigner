using System.Security.Cryptography.X509Certificates;
using Xunit;
using iText.Kernel.Pdf;
using BulkPdfSigner;

namespace BulkPdfSigner_Tests
{
    public class Form1Test
    {
        [Fact]
        public void SignPdfFile_ValidPdfAndCert_ReturnsTrue()
        {
            // Arrange
            var form = new Form1();
            string tempSource = Path.GetTempFileName();
            string tempDest = Path.GetTempFileName();

            // Create a dummy PDF file
            using (var writer = new PdfWriter(tempSource))
            {
                var pdf = new PdfDocument(writer);
                pdf.AddNewPage();
                pdf.Close();
            }

            // Create a self-signed certificate for testing
            var cert = CreateSelfSignedCertificate();

            // Act
            bool result = form.signPdfFile(tempSource, tempDest, cert);

            // Assert
            Xunit.Assert.True(result);
            Xunit.Assert.True(File.Exists(tempDest));

            // Cleanup
            File.Delete(tempSource);
            File.Delete(tempDest);
        }

        [Fact]
        public void SignPdfFile_InvalidSource_ThrowsExceptionAndReturnsFalse()
        {
            // Arrange
            var form = new Form1();
            string invalidSource = "nonexistent.pdf";
            string tempDest = Path.GetTempFileName();
            var cert = CreateSelfSignedCertificate();

            // Act
            bool result = form.signPdfFile(invalidSource, tempDest, cert);

            // Assert
            Assert.False(result);
            File.Delete(tempDest);
        }

        private X509Certificate2 CreateSelfSignedCertificate()
        {
            // For test purposes only: create a dummy self-signed certificate
            var ecdsa = System.Security.Cryptography.ECDsa.Create();
            var req = new CertificateRequest("cn=Test", ecdsa, System.Security.Cryptography.HashAlgorithmName.SHA256);
            return req.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
        }
    }
}