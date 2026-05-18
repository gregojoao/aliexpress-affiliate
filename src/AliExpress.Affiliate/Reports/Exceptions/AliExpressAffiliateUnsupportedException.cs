using AliExpress.Affiliate.Exceptions;

namespace AliExpress.Affiliate.Reports.Exceptions;

/// <summary>
/// Thrown when AliExpress reports that the requested endpoint is not available for the
/// caller's account (e.g. the app has not been granted the report scope).
/// </summary>
public sealed class AliExpressAffiliateUnsupportedException : AliExpressAffiliateException
{
    public AliExpressAffiliateUnsupportedException(
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
