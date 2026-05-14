namespace AliExpress.Affiliate.Application.Requests;

public sealed record AliExpressAffiliateLinksRequest
{
    public IReadOnlyList<string> SourceUrls { get; init; } = Array.Empty<string>();
    public string TrackingId { get; init; } = string.Empty;
    public string PromotionLinkType { get; init; } = string.Empty;
}
