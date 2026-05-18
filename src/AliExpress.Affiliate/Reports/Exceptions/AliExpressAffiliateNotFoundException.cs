namespace AliExpress.Affiliate.Reports.Exceptions;

/// <summary>
/// Thrown when AliExpress reports that the requested resource (e.g. an order id) was
/// not found.
/// </summary>
public sealed class AliExpressAffiliateNotFoundException : AliExpressAffiliateReportsException
{
    public AliExpressAffiliateNotFoundException(
        string message,
        string? code = null,
        string? subCode = null,
        string? requestId = null)
        : base(message, code, subCode, requestId)
    {
    }
}
