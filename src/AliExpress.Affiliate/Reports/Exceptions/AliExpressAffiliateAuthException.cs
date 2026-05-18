namespace AliExpress.Affiliate.Reports.Exceptions;

/// <summary>
/// Thrown when AliExpress rejects the request for signature / credential reasons:
/// invalid sign, revoked app key, missing access token, expired token, etc.
/// </summary>
public sealed class AliExpressAffiliateAuthException : AliExpressAffiliateReportsException
{
    public AliExpressAffiliateAuthException(
        string message,
        string? code = null,
        string? subCode = null,
        string? requestId = null)
        : base(message, code, subCode, requestId)
    {
    }
}
