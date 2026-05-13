namespace AliExpress.Affiliate.Domain;

internal readonly record struct AliExpressProductId
{
    private AliExpressProductId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static bool TryFromUrl(
        string productUrl,
        out AliExpressProductId productId)
    {
        if (AliExpressProductUrl.TryExtractProductId(productUrl, out var extractedProductId))
        {
            productId = new AliExpressProductId(extractedProductId);
            return true;
        }

        productId = default;
        return false;
    }

    public static bool TryFromIdOrUrl(
        string productIdOrUrl,
        out AliExpressProductId productId)
    {
        if (TryFromUrl(productIdOrUrl, out productId))
        {
            return true;
        }

        var value = productIdOrUrl?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            productId = default;
            return false;
        }

        productId = new AliExpressProductId(value);
        return true;
    }

    public override string ToString()
    {
        return Value;
    }
}
