namespace AliExpress.Affiliate;

public sealed record AliExpressHotProductDownloadQuery
{
    public string CategoryId { get; init; } = string.Empty;
    public string Fields { get; init; } = string.Empty;
    public string LocaleSite { get; init; } = string.Empty;
    public int PageNo { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public string TargetCurrency { get; init; } = string.Empty;
    public string TargetLanguage { get; init; } = string.Empty;
    public string TrackingId { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
}
