using AliExpress.Affiliate.Exceptions;

namespace AliExpress.Affiliate.Reports.Exceptions;

/// <summary>
/// Thrown when AliExpress rejects the request for signature / credential reasons:
/// invalid sign, revoked app key, missing access token, expired token, etc.
/// </summary>
public sealed class AliExpressAffiliateAuthException : AliExpressAffiliateException
{
    public AliExpressAffiliateAuthException(
        string message,
        string? code = null,
        string? subCode = null,
        string? requestId = null)
        : base(message)
    {
        Code = code;
        SubCode = subCode;
        RequestId = requestId;
    }

    public string? Code { get; }
    public string? SubCode { get; }
    public string? RequestId { get; }
}
