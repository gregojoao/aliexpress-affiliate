namespace AliExpress.Affiliate.Exceptions;

public class AliExpressAffiliateException : Exception
{
    public AliExpressAffiliateException(string message)
        : base(message)
    {
    }

    public AliExpressAffiliateException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
