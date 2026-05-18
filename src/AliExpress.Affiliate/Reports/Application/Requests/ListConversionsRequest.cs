namespace AliExpress.Affiliate.Reports.Application.Requests;

/// <summary>
/// Parameters for <see cref="AliExpress.Affiliate.Reports.Clients.IAliExpressAffiliateReportsClient.ListConversionsAsync"/>.
/// </summary>
/// <param name="From">Window start. Stored as <see cref="DateTimeOffset"/>; the SDK converts to GMT+8 before sending to AliExpress TOP.</param>
/// <param name="To">Window end (exclusive on the AliExpress side, but the SDK does not adjust it).</param>
/// <param name="Status">Optional status filter sent to <c>aliexpress.affiliate.order.list</c>'s <c>status</c> parameter.</param>
/// <param name="OrderId">Optional single-order filter. AliExpress treats this as a paginated query.</param>
/// <param name="Page">1-based page number. Defaults to 1.</param>
/// <param name="PageSize">Page size; AliExpress accepts up to 50.</param>
/// <param name="TrackingId">Optional tracking id override (defaults to <see cref="AliExpress.Affiliate.Reports.Configuration.AliExpressAffiliateReportsOptions.TrackingId"/>).</param>
public sealed record ListConversionsRequest(
    DateTimeOffset From,
    DateTimeOffset To,
    ConversionStatusFilter? Status = null,
    string? OrderId = null,
    int Page = 1,
    int PageSize = 50,
    string? TrackingId = null);
