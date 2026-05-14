using AliExpress.Affiliate.Domain;

namespace AliExpress.Affiliate.Application.Ports;

internal interface IAliExpressAffiliateProvider
{
    Task<AffiliateLinkLookup> GenerateAffiliateLinkAsync(
        string sourceUrl,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken);

    Task<AliExpressProductDetails?> GetProductDetailsAsync(
        AliExpressProductId productId,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AliExpressAffiliateLink>> GenerateAffiliateLinksAsync(
        IReadOnlyList<string> sourceUrls,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> SearchProductsAsync(
        AliExpressProductQuery query,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetHotProductsAsync(
        AliExpressProductQuery query,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetHotProductDownloadAsync(
        AliExpressHotProductDownloadQuery query,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateCategory>> GetCategoriesAsync(
        string fields,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateFeaturedPromo>> GetFeaturedPromosAsync(
        string fields,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetFeaturedPromoProductsAsync(
        AliExpressFeaturedPromoProductsQuery query,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetSmartMatchProductsAsync(
        AliExpressSmartMatchQuery query,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateOrder>> GetOrdersAsync(
        AliExpressOrderListQuery query,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateOrder>> GetOrderDetailsAsync(
        AliExpressOrderDetailsQuery query,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateOrder>> GetOrdersByIndexAsync(
        AliExpressOrderListByIndexQuery query,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken);
}
