using System.Net;

namespace AliExpress.Affiliate.Exceptions;

public sealed class AliExpressAffiliateHttpException : AliExpressAffiliateException
{
    public AliExpressAffiliateHttpException(HttpStatusCode statusCode, string responseBody)
        : base($"AliExpress API returned HTTP {(int)statusCode}: {responseBody}")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public HttpStatusCode StatusCode { get; }
    public string ResponseBody { get; }
}
