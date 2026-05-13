namespace AliExpress.Affiliate.Domain;

internal static class AffiliateLinkResultFactory
{
    public static AliExpressAffiliateLinkResult Create(
        string affiliateUrl,
        string sourceUrl,
        AliExpressProductDetails? productDetails)
    {
        var productUrl = FirstNonEmpty(productDetails?.ProductUrl ?? string.Empty, sourceUrl);

        return new AliExpressAffiliateLinkResult(
            AffiliateUrl: FirstNonEmpty(affiliateUrl, productDetails?.PromotionLink ?? string.Empty),
            SourceUrl: sourceUrl,
            ProductUrl: productUrl,
            FinalProductUrl: productUrl,
            ProductTitle: productDetails?.ProductTitle ?? string.Empty,
            ProductPrice: productDetails?.ProductPrice ?? string.Empty,
            ProductOriginalPrice: productDetails?.ProductOriginalPrice ?? string.Empty,
            ProductImageUrl: productDetails?.ProductImageUrl ?? string.Empty,
            ProductDetails: productDetails);
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }
}
