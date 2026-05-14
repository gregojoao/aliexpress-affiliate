using AliExpress.Affiliate.Application;
using AliExpress.Affiliate.Domain;
using AliExpress.Affiliate.Infrastructure.OpenPlatform;
using Microsoft.Extensions.Options;

namespace AliExpress.Affiliate;

public sealed class AliExpressAffiliateClient
{
    private readonly AliExpressAffiliateService _affiliateService;
    private readonly AliExpressAffiliateOptions? _defaultOptions;

    public AliExpressAffiliateClient(
        HttpClient httpClient,
        Func<DateTimeOffset>? utcNow = null)
        : this(httpClient, defaultOptions: null, utcNow)
    {
    }

    public AliExpressAffiliateClient(
        HttpClient httpClient,
        IOptions<AliExpressAffiliateOptions> options)
        : this(httpClient, options?.Value)
    {
    }

    public AliExpressAffiliateClient(
        HttpClient httpClient,
        AliExpressAffiliateOptions? defaultOptions,
        Func<DateTimeOffset>? utcNow = null)
    {
        _defaultOptions = defaultOptions;
        _affiliateService = new AliExpressAffiliateService(
            new AliExpressOpenPlatformAffiliateProvider(
                new AliExpressOpenPlatformGateway(httpClient)),
            utcNow);
    }

    public Task<AliExpressAffiliateLinkResult?> GenerateAffiliateLinkAsync(
        string productUrl,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return _affiliateService.GenerateAffiliateLinkAsync(productUrl, options, cancellationToken);
    }

    public Task<AliExpressAffiliateLinkResult?> GenerateAffiliateLinkAsync(
        string productUrl,
        CancellationToken cancellationToken = default)
    {
        return GenerateAffiliateLinkAsync(productUrl, GetDefaultOptions(), cancellationToken);
    }

    public Task<AliExpressProductDetails?> GetProductDetailsAsync(
        string productIdOrUrl,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return _affiliateService.GetProductDetailsAsync(productIdOrUrl, options, cancellationToken);
    }

    public Task<AliExpressProductDetails?> GetProductDetailsAsync(
        string productIdOrUrl,
        CancellationToken cancellationToken = default)
    {
        return GetProductDetailsAsync(productIdOrUrl, GetDefaultOptions(), cancellationToken);
    }

    public Task<IReadOnlyList<AliExpressAffiliateLink>> GenerateAffiliateLinksAsync(
        IEnumerable<string> sourceUrls,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return _affiliateService.GenerateAffiliateLinksAsync(sourceUrls, options, cancellationToken);
    }

    public Task<IReadOnlyList<AliExpressAffiliateLink>> GenerateAffiliateLinksAsync(
        IEnumerable<string> sourceUrls,
        CancellationToken cancellationToken = default)
    {
        return GenerateAffiliateLinksAsync(sourceUrls, GetDefaultOptions(), cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> SearchProductsAsync(
        AliExpressProductQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return _affiliateService.SearchProductsAsync(query, options, cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> SearchProductsAsync(
        AliExpressProductQuery query,
        CancellationToken cancellationToken = default)
    {
        return SearchProductsAsync(query, GetDefaultOptions(), cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetHotProductsAsync(
        AliExpressProductQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return _affiliateService.GetHotProductsAsync(query, options, cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetHotProductsAsync(
        AliExpressProductQuery query,
        CancellationToken cancellationToken = default)
    {
        return GetHotProductsAsync(query, GetDefaultOptions(), cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetHotProductDownloadAsync(
        AliExpressHotProductDownloadQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return _affiliateService.GetHotProductDownloadAsync(query, options, cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetHotProductDownloadAsync(
        AliExpressHotProductDownloadQuery query,
        CancellationToken cancellationToken = default)
    {
        return GetHotProductDownloadAsync(query, GetDefaultOptions(), cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateCategory>> GetCategoriesAsync(
        AliExpressAffiliateOptions options,
        string fields = "",
        CancellationToken cancellationToken = default)
    {
        return _affiliateService.GetCategoriesAsync(fields, options, cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateCategory>> GetCategoriesAsync(
        string fields = "",
        CancellationToken cancellationToken = default)
    {
        return GetCategoriesAsync(GetDefaultOptions(), fields, cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateFeaturedPromo>> GetFeaturedPromosAsync(
        AliExpressAffiliateOptions options,
        string fields = "",
        CancellationToken cancellationToken = default)
    {
        return _affiliateService.GetFeaturedPromosAsync(fields, options, cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateFeaturedPromo>> GetFeaturedPromosAsync(
        string fields = "",
        CancellationToken cancellationToken = default)
    {
        return GetFeaturedPromosAsync(GetDefaultOptions(), fields, cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetFeaturedPromoProductsAsync(
        AliExpressFeaturedPromoProductsQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return _affiliateService.GetFeaturedPromoProductsAsync(query, options, cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetFeaturedPromoProductsAsync(
        AliExpressFeaturedPromoProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        return GetFeaturedPromoProductsAsync(query, GetDefaultOptions(), cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetSmartMatchProductsAsync(
        AliExpressSmartMatchQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return _affiliateService.GetSmartMatchProductsAsync(query, options, cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetSmartMatchProductsAsync(
        AliExpressSmartMatchQuery query,
        CancellationToken cancellationToken = default)
    {
        return GetSmartMatchProductsAsync(query, GetDefaultOptions(), cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateOrder>> GetOrdersAsync(
        AliExpressOrderListQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return _affiliateService.GetOrdersAsync(query, options, cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateOrder>> GetOrdersAsync(
        AliExpressOrderListQuery query,
        CancellationToken cancellationToken = default)
    {
        return GetOrdersAsync(query, GetDefaultOptions(), cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateOrder>> GetOrderDetailsAsync(
        AliExpressOrderDetailsQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return _affiliateService.GetOrderDetailsAsync(query, options, cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateOrder>> GetOrderDetailsAsync(
        AliExpressOrderDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        return GetOrderDetailsAsync(query, GetDefaultOptions(), cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateOrder>> GetOrdersByIndexAsync(
        AliExpressOrderListByIndexQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return _affiliateService.GetOrdersByIndexAsync(query, options, cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateOrder>> GetOrdersByIndexAsync(
        AliExpressOrderListByIndexQuery query,
        CancellationToken cancellationToken = default)
    {
        return GetOrdersByIndexAsync(query, GetDefaultOptions(), cancellationToken);
    }

    public static AliExpressOpenPlatformRequest BuildLinkGenerateRequest(
        string productUrl,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp)
    {
        return AliExpressOpenPlatformRequestFactory.BuildLinkGenerateRequest(productUrl, options, timestamp);
    }

    public static AliExpressOpenPlatformRequest BuildProductDetailRequest(
        string productId,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp)
    {
        return AliExpressOpenPlatformRequestFactory.BuildProductDetailRequest(productId, options, timestamp);
    }

    public static AliExpressOpenPlatformRequest BuildApiRequest(
        string method,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        IReadOnlyDictionary<string, string>? apiParameters = null)
    {
        return AliExpressOpenPlatformRequestFactory.BuildApiRequest(method, options, timestamp, apiParameters);
    }

    public static string NormalizeAliExpressUrl(string url)
    {
        return AliExpressProductUrl.Normalize(url);
    }

    public static bool TryExtractProductId(
        string productUrl,
        out string productId)
    {
        return AliExpressProductUrl.TryExtractProductId(productUrl, out productId);
    }

    public static string CreateTopSignature(
        IReadOnlyDictionary<string, string> parameters,
        string appSecret,
        string signMethod)
    {
        return AliExpressOpenPlatformSigner.CreateTopSignature(parameters, appSecret, signMethod);
    }

    public static string BuildSignatureSourceString(
        IEnumerable<KeyValuePair<string, string>> parameters)
    {
        return AliExpressOpenPlatformSigner.BuildSignatureSourceString(parameters);
    }

    public static string ExtractAffiliateUrl(string responseBody)
    {
        return AliExpressOpenPlatformResponseParser.ExtractAffiliateUrl(responseBody);
    }

    public static AliExpressProductDetails? ExtractProductDetails(string responseBody)
    {
        return AliExpressOpenPlatformResponseParser.ExtractProductDetails(responseBody);
    }

    private AliExpressAffiliateOptions GetDefaultOptions()
    {
        return _defaultOptions ?? throw new InvalidOperationException(
            "No default AliExpressAffiliateOptions were configured. Pass options to this method or register the client with AddAliExpressAffiliate.");
    }
}
