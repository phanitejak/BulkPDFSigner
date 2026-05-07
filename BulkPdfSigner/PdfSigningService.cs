using System.Security.Cryptography.X509Certificates;
using iText.Kernel.Pdf;
using iText.Signatures;
using Org.BouncyCastle.X509;

namespace BulkPdfSigner;

public static class PdfSigningService
{
    public static void Sign(
        string sourceDocument,
        string destinationPath,
        X509Certificate2 cert,
        bool lastPage)
    {
        var certParser = new X509CertificateParser();
        Org.BouncyCastle.X509.X509Certificate[] chain = [certParser.ReadCertificate(cert.RawData)];
        IExternalSignature externalSignature = new X509Certificate2Signature(cert, "SHA256");

        using var pdfReader = new PdfReader(sourceDocument);
        using var dest = new FileStream(destinationPath, FileMode.Create, FileAccess.ReadWrite);
        var pdfSigner = new PdfSigner(pdfReader, dest, new StampingProperties());

        var appearance = pdfSigner.GetSignatureAppearance();

        if (lastPage)
        {
            int lastPageNum = pdfSigner.GetDocument().GetNumberOfPages();
            var rect = new iText.Kernel.Geom.Rectangle(36, 648, 200, 100);
            appearance
                .SetReuseAppearance(false)
                .SetPageRect(rect)
                .SetPageNumber(lastPageNum);
            pdfSigner.SetFieldName("signature1");
        }
        else
        {
            pdfSigner.SetFieldName("Signature 1");
        }

        appearance.SetRenderingMode(PdfSignatureAppearance.RenderingMode.NAME_AND_DESCRIPTION);
        appearance.SetReason("");
        appearance.SetLocation("");
        pdfSigner.SignDetached(externalSignature, chain, null, null, null, 0, PdfSigner.CryptoStandard.CMS);
    }
}
