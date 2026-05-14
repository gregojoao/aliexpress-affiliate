using AliExpress.Affiliate.Domain;

namespace AliExpress.Affiliate.Infrastructure.OpenPlatform;

internal static class AliExpressOpenPlatformResponseParser
{
    public static string ExtractAffiliateUrl(string responseBody)
    {
        return AliExpressAffiliateLinkResponseMapper.ExtractAffiliateUrl(responseBody);
    }

    public static IReadOnlyList<AliExpressAffiliateLink> ExtractAffiliateLinks(string responseBody)
    {
        return AliExpressAffiliateLinkResponseMapper.ExtractAffiliateLinks(responseBody);
    }

    public static string SummarizeLinkGenerateResponse(string responseBody)
    {
        return AliExpressAffiliateLinkResponseMapper.SummarizeLinkGenerateResponse(responseBody);
    }

    public static AliExpressProductDetails? ExtractProductDetails(string responseBody)
    {
        return AliExpressProductResponseMapper.ExtractProductDetails(responseBody);
    }

    public static AliExpressAffiliateApiResult<AliExpressAffiliateProduct> ExtractProducts(string responseBody)
    {
        return AliExpressProductResponseMapper.ExtractProducts(responseBody);
    }

    public static AliExpressAffiliateApiResult<AliExpressAffiliateCategory> ExtractCategories(string responseBody)
    {
        return AliExpressCategoryResponseMapper.ExtractCategories(responseBody);
    }

    public static AliExpressAffiliateApiResult<AliExpressAffiliateFeaturedPromo> ExtractFeaturedPromos(string responseBody)
    {
        return AliExpressFeaturedPromoResponseMapper.ExtractFeaturedPromos(responseBody);
    }

    public static AliExpressAffiliateApiResult<AliExpressAffiliateOrder> ExtractOrders(string responseBody)
    {
        return AliExpressOrderResponseMapper.ExtractOrders(responseBody);
    }
}
