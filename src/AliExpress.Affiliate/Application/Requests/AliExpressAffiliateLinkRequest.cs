namespace AliExpress.Affiliate.Application.Requests;

public sealed record AliExpressAffiliateLinkRequest
{
    public string ProductUrl { get; init; } = string.Empty;
    public string TrackingId { get; init; } = string.Empty;
    public string PromotionLinkType { get; init; } = string.Empty;
    public string ShipToCountryCode { get; init; } = string.Empty;
    public string TargetCurrency { get; init; } = string.Empty;
    public string TargetLanguage { get; init; } = string.Empty;
    public bool IncludeProductDetails { get; init; }
}
