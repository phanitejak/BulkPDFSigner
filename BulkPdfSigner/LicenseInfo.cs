using System.Globalization;

namespace BulkPdfSigner;

public sealed record LicenseInfo(
    string Username,
    string UsbSerial,
    string Circle,
    string ValidTill,
    string LicType)
{
    public bool IsTrial => LicType.Equals("TRIAL", StringComparison.OrdinalIgnoreCase);

    public bool TryGetValidTillDate(out DateTime date) =>
        DateTime.TryParseExact(
            ValidTill,
            "dd-MM-yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);

    public bool IsExpired => TryGetValidTillDate(out var d) && DateTime.Now > d;

    public bool AllowsLastPageStamp =>
        LicType.Equals("ALL", StringComparison.OrdinalIgnoreCase) ||
        LicType.Equals("SACFA", StringComparison.OrdinalIgnoreCase);
}
