using AliExpress.Affiliate.Application.Requests;
using AliExpress.Affiliate.Configuration;
using AliExpress.Affiliate.Domain;

namespace AliExpress.Affiliate.Clients;

public interface IAliExpressAffiliateClient
{
    Task<AliExpressAffiliateLinkResult?> GenerateAffiliateLinkAsync(
        AliExpressAffiliateLinkRequest request,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default);

    Task<AliExpressAffiliateLinkResult?> GenerateAffiliateLinkAsync(
        AliExpressAffiliateLinkRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AliExpressAffiliateLink>> GenerateAffiliateLinksAsync(
        AliExpressAffiliateLinksRequest request,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AliExpressAffiliateLink>> GenerateAffiliateLinksAsync(
        AliExpressAffiliateLinksRequest request,
        CancellationToken cancellationToken = default);

    Task<AliExpressProductDetails?> GetProductDetailsAsync(
        string productIdOrUrl,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default);

    Task<AliExpressProductDetails?> GetProductDetailsAsync(
        string productIdOrUrl,
        CancellationToken cancellationToken = default);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> SearchProductsAsync(
        AliExpressProductQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> SearchProductsAsync(
        AliExpressProductQuery query,
        CancellationToken cancellationToken = default);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetHotProductsAsync(
        AliExpressProductQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetHotProductsAsync(
        AliExpressProductQuery query,
        CancellationToken cancellationToken = default);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetHotProductDownloadAsync(
        AliExpressHotProductDownloadQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetHotProductDownloadAsync(
        AliExpressHotProductDownloadQuery query,
        CancellationToken cancellationToken = default);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateCategory>> GetCategoriesAsync(
        AliExpressAffiliateOptions options,
        string fields = "",
        CancellationToken cancellationToken = default);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateCategory>> GetCategoriesAsync(
        string fields = "",
        CancellationToken cancellationToken = default);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateFeaturedPromo>> GetFeaturedPromosAsync(
        AliExpressAffiliateOptions options,
        string fields = "",
        CancellationToken cancellationToken = default);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateFeaturedPromo>> GetFeaturedPromosAsync(
        string fields = "",
        CancellationToken cancellationToken = default);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetFeaturedPromoProductsAsync(
        AliExpressFeaturedPromoProductsQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetFeaturedPromoProductsAsync(
        AliExpressFeaturedPromoProductsQuery query,
        CancellationToken cancellationToken = default);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetSmartMatchProductsAsync(
        AliExpressSmartMatchQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetSmartMatchProductsAsync(
        AliExpressSmartMatchQuery query,
        CancellationToken cancellationToken = default);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateOrder>> GetOrdersAsync(
        AliExpressOrderListQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateOrder>> GetOrdersAsync(
        AliExpressOrderListQuery query,
        CancellationToken cancellationToken = default);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateOrder>> GetOrderDetailsAsync(
        AliExpressOrderDetailsQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateOrder>> GetOrderDetailsAsync(
        AliExpressOrderDetailsQuery query,
        CancellationToken cancellationToken = default);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateOrder>> GetOrdersByIndexAsync(
        AliExpressOrderListByIndexQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default);

    Task<AliExpressAffiliateApiResult<AliExpressAffiliateOrder>> GetOrdersByIndexAsync(
        AliExpressOrderListByIndexQuery query,
        CancellationToken cancellationToken = default);
}
