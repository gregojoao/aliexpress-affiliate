using AliExpress.Affiliate.Exceptions;

namespace AliExpress.Affiliate.Reports.Exceptions;

/// <summary>
/// Thrown when AliExpress reports that the requested resource (e.g. an order id) was
/// not found.
/// </summary>
public sealed class AliExpressAffiliateNotFoundException : AliExpressAffiliateException
{
    public AliExpressAffiliateNotFoundException(
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
