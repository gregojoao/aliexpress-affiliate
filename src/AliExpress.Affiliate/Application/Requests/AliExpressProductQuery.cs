namespace AliExpress.Affiliate.Application.Requests;

public sealed record AliExpressProductQuery
{
    public string CategoryIds { get; init; } = string.Empty;
    public string Fields { get; init; } = string.Empty;
    public string Keywords { get; init; } = string.Empty;
    public string MaxSalePrice { get; init; } = string.Empty;
    public string MinSalePrice { get; init; } = string.Empty;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public string PlatformProductType { get; init; } = string.Empty;
    public string Sort { get; init; } = string.Empty;
    public string TargetCurrency { get; init; } = string.Empty;
    public string TargetLanguage { get; init; } = string.Empty;
    public string TrackingId { get; init; } = string.Empty;
    public string ShipToCountryCode { get; init; } = string.Empty;
    public string DeliveryDays { get; init; } = string.Empty;
}
