using AliExpress.Affiliate.Application.Ports;
using AliExpress.Affiliate.Application.Requests;
using AliExpress.Affiliate.Configuration;
using AliExpress.Affiliate.Domain;
using AliExpress.Affiliate.Exceptions;

namespace AliExpress.Affiliate.Application;

internal sealed class AliExpressAffiliateService
{
    private readonly IAliExpressAffiliateProvider _affiliateProvider;
    private readonly Func<DateTimeOffset> _utcNow;

    public AliExpressAffiliateService(
        IAliExpressAffiliateProvider affiliateProvider,
        Func<DateTimeOffset>? utcNow = null)
    {
        _affiliateProvider = affiliateProvider ?? throw new ArgumentNullException(nameof(affiliateProvider));
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public async Task<AliExpressAffiliateLinkResult?> GenerateAffiliateLinkAsync(
        AliExpressAffiliateLinkRequest request,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProductUrl) ||
            !Uri.TryCreate(request.ProductUrl, UriKind.Absolute, out _))
        {
            return null;
        }

        var effectiveOptions = ApplyLinkRequestDefaults(request, options);
        effectiveOptions.Validate();

        using var timeoutCts = CreateTimeoutTokenSource(effectiveOptions, cancellationToken);

        var sourceUrl = AliExpressProductUrl.Normalize(request.ProductUrl);
        var linkLookup = await _affiliateProvider.GenerateAffiliateLinkAsync(
            sourceUrl,
            request,
            effectiveOptions,
            _utcNow(),
            timeoutCts.Token);

        if (string.IsNullOrWhiteSpace(linkLookup.AffiliateUrl))
        {
            throw new AliExpressAffiliateLinkUnavailableException(
                sourceUrl,
                "The AliExpress API response did not contain promotion_link.",
                linkLookup.MissingLinkSummary);
        }

        AliExpressProductDetails? productDetails = null;
        if (request.IncludeProductDetails &&
            AliExpressProductId.TryFromUrl(sourceUrl, out var productId))
        {
            productDetails = await GetProductDetailsCoreAsync(
                productId,
                effectiveOptions,
                timeoutCts.Token);
        }

        return AffiliateLinkResultFactory.Create(linkLookup.AffiliateUrl, sourceUrl, productDetails);
    }

    public async Task<AliExpressProductDetails?> GetProductDetailsAsync(
        string productIdOrUrl,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        options.Validate();

        if (!AliExpressProductId.TryFromIdOrUrl(productIdOrUrl, out var productId))
        {
            return null;
        }

        using var timeoutCts = CreateTimeoutTokenSource(options, cancellationToken);

        return await GetProductDetailsCoreAsync(productId, options, timeoutCts.Token);
    }

    public async Task<IReadOnlyList<AliExpressAffiliateLink>> GenerateAffiliateLinksAsync(
        AliExpressAffiliateLinksRequest request,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        options.Validate();

        var normalizedUrls = request.SourceUrls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(AliExpressProductUrl.Normalize)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedUrls.Length == 0)
        {
            return Array.Empty<AliExpressAffiliateLink>();
        }

        using var timeoutCts = CreateTimeoutTokenSource(options, cancellationToken);

        return await _affiliateProvider.GenerateAffiliateLinksAsync(
            normalizedUrls,
            request,
            options,
            _utcNow(),
            timeoutCts.Token);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> SearchProductsAsync(
        AliExpressProductQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return ExecuteWithTimeoutAsync(options, cancellationToken, token =>
            _affiliateProvider.SearchProductsAsync(query, options, _utcNow(), token));
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetHotProductsAsync(
        AliExpressProductQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return ExecuteWithTimeoutAsync(options, cancellationToken, token =>
            _affiliateProvider.GetHotProductsAsync(query, options, _utcNow(), token));
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetHotProductDownloadAsync(
        AliExpressHotProductDownloadQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return ExecuteWithTimeoutAsync(options, cancellationToken, token =>
            _affiliateProvider.GetHotProductDownloadAsync(query, options, _utcNow(), token));
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateCategory>> GetCategoriesAsync(
        string fields,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return ExecuteWithTimeoutAsync(options, cancellationToken, token =>
            _affiliateProvider.GetCategoriesAsync(fields, options, _utcNow(), token));
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateFeaturedPromo>> GetFeaturedPromosAsync(
        string fields,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return ExecuteWithTimeoutAsync(options, cancellationToken, token =>
            _affiliateProvider.GetFeaturedPromosAsync(fields, options, _utcNow(), token));
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetFeaturedPromoProductsAsync(
        AliExpressFeaturedPromoProductsQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return ExecuteWithTimeoutAsync(options, cancellationToken, token =>
            _affiliateProvider.GetFeaturedPromoProductsAsync(query, options, _utcNow(), token));
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetSmartMatchProductsAsync(
        AliExpressSmartMatchQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return ExecuteWithTimeoutAsync(options, cancellationToken, token =>
            _affiliateProvider.GetSmartMatchProductsAsync(query, options, _utcNow(), token));
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateOrder>> GetOrdersAsync(
        AliExpressOrderListQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return ExecuteWithTimeoutAsync(options, cancellationToken, token =>
            _affiliateProvider.GetOrdersAsync(query, options, _utcNow(), token));
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateOrder>> GetOrderDetailsAsync(
        AliExpressOrderDetailsQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return ExecuteWithTimeoutAsync(options, cancellationToken, token =>
            _affiliateProvider.GetOrderDetailsAsync(query, options, _utcNow(), token));
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateOrder>> GetOrdersByIndexAsync(
        AliExpressOrderListByIndexQuery query,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return ExecuteWithTimeoutAsync(options, cancellationToken, token =>
            _affiliateProvider.GetOrdersByIndexAsync(query, options, _utcNow(), token));
    }

    private async Task<AliExpressProductDetails?> GetProductDetailsCoreAsync(
        AliExpressProductId productId,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken)
    {
        return await _affiliateProvider.GetProductDetailsAsync(
            productId,
            options,
            _utcNow(),
            cancellationToken);
    }

    private static CancellationTokenSource CreateTimeoutTokenSource(
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken)
    {
        var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(Math.Max(options.TimeoutMilliseconds, 1)));
        return timeoutCts;
    }

    private static async Task<T> ExecuteWithTimeoutAsync<T>(
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken,
        Func<CancellationToken, Task<T>> execute)
    {
        options.Validate();

        using var timeoutCts = CreateTimeoutTokenSource(options, cancellationToken);
        return await execute(timeoutCts.Token);
    }

    private static AliExpressAffiliateOptions ApplyLinkRequestDefaults(
        AliExpressAffiliateLinkRequest request,
        AliExpressAffiliateOptions options)
    {
        return new AliExpressAffiliateOptions
        {
            ApiEndpoint = options.ApiEndpoint,
            AppKey = options.AppKey,
            AppSecret = options.AppSecret,
            DefaultTrackingId = FirstNonEmpty(request.TrackingId, options.DefaultTrackingId),
            AppSignature = options.AppSignature,
            SignMethod = options.SignMethod,
            DefaultPromotionLinkType = FirstNonEmpty(request.PromotionLinkType, options.DefaultPromotionLinkType),
            DefaultShipToCountry = FirstNonEmpty(request.ShipToCountryCode, options.DefaultShipToCountry),
            DefaultTargetCurrency = FirstNonEmpty(request.TargetCurrency, options.DefaultTargetCurrency),
            DefaultTargetLanguage = FirstNonEmpty(request.TargetLanguage, options.DefaultTargetLanguage),
            TimeoutMilliseconds = options.TimeoutMilliseconds
        };
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }
}
