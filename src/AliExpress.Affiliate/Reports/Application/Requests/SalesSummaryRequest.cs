namespace AliExpress.Affiliate.Reports.Application.Requests;

/// <summary>
/// Parameters for <see cref="AliExpress.Affiliate.Reports.Clients.IAliExpressAffiliateReportsClient.GetSalesSummaryAsync"/>.
/// </summary>
public sealed record SalesSummaryRequest(
    DateTimeOffset From,
    DateTimeOffset To,
    string? TrackingId = null);
