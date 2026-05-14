namespace AliExpress.Affiliate.Exceptions;

public sealed class AliExpressAffiliateApiException : AliExpressAffiliateException
{
    public AliExpressAffiliateApiException(string message)
        : base(message)
    {
    }
}
