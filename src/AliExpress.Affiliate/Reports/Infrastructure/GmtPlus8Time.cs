using System.Globalization;

namespace AliExpress.Affiliate.Reports.Infrastructure;

/// <summary>
/// Helpers for converting <see cref="DateTimeOffset"/> values to the timezone and
/// string format expected by the AliExpress TOP gateway. AliExpress evaluates
/// <c>start_time</c>/<c>end_time</c> parameters in <c>GMT+8</c> using the
/// <c>yyyy-MM-dd HH:mm:ss</c> format.
/// </summary>
internal static class GmtPlus8Time
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(8);

    /// <summary>Formats <paramref name="value"/> in GMT+8 as <c>yyyy-MM-dd HH:mm:ss</c>.</summary>
    public static string Format(DateTimeOffset value)
    {
        return value.ToOffset(Offset).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }
}
