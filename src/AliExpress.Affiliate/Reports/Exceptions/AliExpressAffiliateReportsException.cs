using AliExpress.Affiliate.Exceptions;

namespace AliExpress.Affiliate.Reports.Exceptions;

/// <summary>
/// Base class for AliExpress Affiliate Reports exceptions. Carries the AliExpress
/// error code triple (<see cref="Code"/>, <see cref="SubCode"/>, <see cref="RequestId"/>)
/// for support correlation. Catch this type to handle every report-specific failure;
/// catch <see cref="AliExpressAffiliateException"/> to handle every SDK failure.
/// </summary>
public abstract class AliExpressAffiliateReportsException : AliExpressAffiliateException
{
    protected AliExpressAffiliateReportsException(
        string message,
        string? code,
        string? subCode,
        string? requestId)
        : base(message)
    {
        Code = code;
        SubCode = subCode;
        RequestId = requestId;
    }

    /// <summary>Top-level error code reported by AliExpress (e.g. <c>isv.invalid-signature</c>).</summary>
    public string? Code { get; }

    /// <summary>Sub-code returned alongside <see cref="Code"/>, when available.</summary>
    public string? SubCode { get; }

    /// <summary>AliExpress request id (response <c>request_id</c> or header <c>x-ca-request-id</c>) for support correlation.</summary>
    public string? RequestId { get; }
}
