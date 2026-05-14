namespace AliExpress.Affiliate;

public sealed record AliExpressSmartMatchQuery
{
    public string DeviceId { get; init; } = string.Empty;
    public string App { get; init; } = string.Empty;
    public string Device { get; init; } = string.Empty;
    public string Fields { get; init; } = string.Empty;
    public string Keywords { get; init; } = string.Empty;
    public string ProductId { get; init; } = string.Empty;
    public string Site { get; init; } = string.Empty;
    public string TargetCurrency { get; init; } = string.Empty;
    public string TargetLanguage { get; init; } = string.Empty;
    public string TrackingId { get; init; } = string.Empty;
    public string User { get; init; } = string.Empty;
    public int PageNo { get; init; } = 1;
    public string Country { get; init; } = string.Empty;
}
