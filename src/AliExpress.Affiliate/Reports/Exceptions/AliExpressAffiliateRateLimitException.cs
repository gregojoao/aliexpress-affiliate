namespace AliExpress.Affiliate.Reports.Exceptions;

/// <summary>
/// Thrown when AliExpress signals that the caller is being rate-limited
/// (HTTP 429 or a TOP error code such as <c>isv.api-flow-limit</c> /
/// <c>HTTP_INVOKE_LIMITED</c>).
/// </summary>
public sealed class AliExpressAffiliateRateLimitException : AliExpressAffiliateReportsException
{
    public AliExpressAffiliateRateLimitException(
        string message,
        string? code = null,
        string? subCode = null,
        string? requestId = null)
        : base(message, code, subCode, requestId)
    {
    }
}
