using AliExpress.Affiliate.Exceptions;

namespace AliExpress.Affiliate.Reports.Configuration;

/// <summary>
/// Options used to call the AliExpress Affiliate reporting endpoints exposed by the
/// AliExpress Open Platform / TOP gateway.
/// </summary>
/// <remarks>
/// The reporting client signs every request with <see cref="AppKey"/> and
/// <see cref="AppSecret"/>. <see cref="AccessToken"/> is only required by endpoints
/// that the AliExpress Open Platform marks as needing OAuth (none of the
/// <c>aliexpress.affiliate.order.*</c> endpoints require it today). Provide it as a
/// forward-compatible knob.
/// </remarks>
public sealed class AliExpressAffiliateReportsOptions
{
    /// <summary>Default TOP gateway endpoint shared with the link-generation client.</summary>
    public const string DefaultEndpoint = "https://api-sg.aliexpress.com/sync";

    /// <summary>Default signing algorithm used by the reporting client. SHA-256 is the AliExpress recommendation.</summary>
    public const string DefaultSignMethod = "sha256";

    /// <summary>Default API version targeted by the reporting client.</summary>
    public const string DefaultApiVersion = "2.0";

    /// <summary>Default timeout applied to a single TOP HTTP call.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>App key issued by the AliExpress Open Platform.</summary>
    public string AppKey { get; set; } = string.Empty;

    /// <summary>App secret issued by the AliExpress Open Platform. Never logged.</summary>
    public string AppSecret { get; set; } = string.Empty;

    /// <summary>Optional OAuth access token for endpoints that require user authorization.</summary>
    public string? AccessToken { get; set; }

    /// <summary>Tracking id (PID) used to scope conversions and summaries by sub-affiliate.</summary>
    public string? TrackingId { get; set; }

    /// <summary>Base endpoint for the TOP gateway. Override only when targeting a regional gateway.</summary>
    public string Endpoint { get; set; } = DefaultEndpoint;

    /// <summary>
    /// Per-call timeout. Applied on top of the injected <see cref="HttpClient"/> timeout via a
    /// linked cancellation token, so the HttpClient can keep <see cref="System.Threading.Timeout.InfiniteTimeSpan"/>.
    /// </summary>
    public TimeSpan Timeout { get; set; } = DefaultTimeout;

    /// <summary>Signing algorithm. Accepted values: <c>sha256</c>, <c>md5</c>.</summary>
    public string SignMethod { get; set; } = DefaultSignMethod;

    /// <summary>TOP API version, defaults to <c>2.0</c>.</summary>
    public string ApiVersion { get; set; } = DefaultApiVersion;

    /// <summary>
    /// Throws <see cref="AliExpressAffiliateValidationException"/> if the required app key /
    /// app secret are not set.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AppKey))
        {
            throw new AliExpressAffiliateValidationException("AppKey is required to call AliExpress Affiliate report endpoints.");
        }

        if (string.IsNullOrWhiteSpace(AppSecret))
        {
            throw new AliExpressAffiliateValidationException("AppSecret is required to call AliExpress Affiliate report endpoints.");
        }

        if (string.IsNullOrWhiteSpace(Endpoint))
        {
            throw new AliExpressAffiliateValidationException("Endpoint is required to call AliExpress Affiliate report endpoints.");
        }
    }
}
