namespace AliExpress.Affiliate.Reports.Exceptions;

/// <summary>
/// Thrown when AliExpress reports that the requested endpoint is not available for the
/// caller's account (e.g. the app has not been granted the report scope).
/// </summary>
public sealed class AliExpressAffiliateUnsupportedException : AliExpressAffiliateReportsException
{
    public AliExpressAffiliateUnsupportedException(
        string message,
        string? code = null,
        string? subCode = null,
        string? requestId = null)
        : base(message, code, subCode, requestId)
    {
    }
}
