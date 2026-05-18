namespace AliExpress.Affiliate.Exceptions;

public sealed class AliExpressAffiliateApiException : AliExpressAffiliateException
{
    public AliExpressAffiliateApiException(string message)
        : base(message)
    {
    }

    public AliExpressAffiliateApiException(
        string message,
        string? code,
        string? subCode = null,
        string? requestId = null)
        : base(message)
    {
        Code = code;
        SubCode = subCode;
        RequestId = requestId;
    }

    /// <summary>Top-level error code reported by AliExpress (e.g. <c>isv.invalid-parameter</c>).</summary>
    public string? Code { get; }

    /// <summary>Sub-code returned alongside <see cref="Code"/>, when available.</summary>
    public string? SubCode { get; }

    /// <summary>AliExpress request id (header <c>x-ca-request-id</c> or response <c>request_id</c> field) for support correlation.</summary>
    public string? RequestId { get; }
}
