namespace AliExpress.Affiliate.Reports.Application.Requests;

/// <summary>
/// Parameters for <see cref="AliExpress.Affiliate.Reports.Clients.IAliExpressAffiliateReportsClient.GetGeneratedLinkUsageAsync"/>.
/// </summary>
public sealed record LinkUsageRequest(
    DateTimeOffset From,
    DateTimeOffset To,
    string? TrackingId = null);
