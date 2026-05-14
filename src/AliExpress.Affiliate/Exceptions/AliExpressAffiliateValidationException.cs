namespace AliExpress.Affiliate.Exceptions;

public sealed class AliExpressAffiliateValidationException : AliExpressAffiliateException
{
    public AliExpressAffiliateValidationException(string message)
        : base(message)
    {
    }
}
