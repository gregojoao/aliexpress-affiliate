namespace AliExpress.Affiliate.Application.Requests;

public sealed record AliExpressHotProductDownloadQuery
{
    public string CategoryId { get; init; } = string.Empty;
    public string Fields { get; init; } = string.Empty;
    public string Locale { get; init; } = string.Empty;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public string TargetCurrency { get; init; } = string.Empty;
    public string TargetLanguage { get; init; } = string.Empty;
    public string TrackingId { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
}
