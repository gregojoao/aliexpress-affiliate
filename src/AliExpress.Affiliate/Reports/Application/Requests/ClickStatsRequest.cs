namespace AliExpress.Affiliate.Reports.Application.Requests;

/// <summary>
/// Parameters for <see cref="AliExpress.Affiliate.Reports.Clients.IAliExpressAffiliateReportsClient.GetClickStatsAsync"/>.
/// </summary>
public sealed record ClickStatsRequest(
    DateTimeOffset From,
    DateTimeOffset To,
    ReportGranularity Granularity = ReportGranularity.Day,
    string? TrackingId = null);
