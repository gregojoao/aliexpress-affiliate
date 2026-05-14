using AliExpress.Affiliate.Exceptions;

namespace AliExpress.Affiliate.Configuration;

/// <summary>
/// Options used to call the AliExpress Open Platform affiliate APIs.
/// </summary>
public sealed class AliExpressAffiliateOptions
{
    public const string DefaultEndpoint = "https://api-sg.aliexpress.com/sync";
    public const string DefaultSignMethod = "md5";
    public const string FallbackPromotionLinkType = "0";
    public const int DefaultTimeoutMilliseconds = 90000;

    public string ApiEndpoint { get; set; } = DefaultEndpoint;
    public string AppKey { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
    public string DefaultTrackingId { get; set; } = string.Empty;
    public string AppSignature { get; set; } = string.Empty;
    public string SignMethod { get; set; } = DefaultSignMethod;
    public string DefaultPromotionLinkType { get; set; } = FallbackPromotionLinkType;
    public string DefaultShipToCountry { get; set; } = string.Empty;
    public string DefaultTargetCurrency { get; set; } = string.Empty;
    public string DefaultTargetLanguage { get; set; } = string.Empty;
    public int TimeoutMilliseconds { get; set; } = DefaultTimeoutMilliseconds;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AppKey))
        {
            throw new AliExpressAffiliateValidationException("AppKey is required to use the AliExpress API.");
        }

        if (string.IsNullOrWhiteSpace(AppSecret))
        {
            throw new AliExpressAffiliateValidationException("AppSecret is required to use the AliExpress API.");
        }

        if (string.IsNullOrWhiteSpace(DefaultTrackingId))
        {
            throw new AliExpressAffiliateValidationException("DefaultTrackingId is required to generate AliExpress affiliate links.");
        }
    }
}
