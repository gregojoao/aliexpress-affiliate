using AliExpress.Affiliate.Reports.Application;

namespace AliExpress.Affiliate.Reports.Domain;

/// <summary>
/// Time-series click statistics. AliExpress' public TOP gateway does not expose click
/// counts to affiliates; <see cref="Supported"/> is <c>false</c> by default and
/// <see cref="Points"/> is empty. Callers should branch on <see cref="Supported"/>
/// and fall back to manual reports (CSV export from the affiliate portal).
/// </summary>
public sealed record AliExpressClickStats(
    ReportGranularity Granularity,
    IReadOnlyList<AliExpressClickPoint> Points,
    bool Supported,
    string? UnsupportedReason);
