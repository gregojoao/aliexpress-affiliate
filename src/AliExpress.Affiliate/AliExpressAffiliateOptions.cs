namespace AliExpress.Affiliate;

/// <summary>
/// Options used to call the AliExpress Open Platform affiliate APIs.
/// </summary>
public sealed class AliExpressAffiliateOptions
{
    public const string DefaultEndpoint = "https://api-sg.aliexpress.com/sync";
    public const string DefaultSignMethod = "md5";
    public const string DefaultPromotionLinkType = "0";
    public const int DefaultTimeoutMilliseconds = 90000;

    public string Endpoint { get; set; } = DefaultEndpoint;
    public string AppKey { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string TrackingId { get; set; } = string.Empty;
    public string AppSignature { get; set; } = string.Empty;
    public string SignMethod { get; set; } = DefaultSignMethod;
    public string PromotionLinkType { get; set; } = DefaultPromotionLinkType;
    public string ShipToCountry { get; set; } = string.Empty;
    public string TargetCurrency { get; set; } = string.Empty;
    public string TargetLanguage { get; set; } = string.Empty;
    public bool IncludeProductDetails { get; set; }
    public int TimeoutMilliseconds { get; set; } = DefaultTimeoutMilliseconds;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AppKey))
        {
            throw new InvalidOperationException("AppKey is required to use the AliExpress API.");
        }

        if (string.IsNullOrWhiteSpace(AppSecret))
        {
            throw new InvalidOperationException("AppSecret is required to use the AliExpress API.");
        }

        if (string.IsNullOrWhiteSpace(TrackingId))
        {
            throw new InvalidOperationException("TrackingId is required to generate AliExpress affiliate links.");
        }
    }
}
