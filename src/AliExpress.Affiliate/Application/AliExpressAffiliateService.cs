using AliExpress.Affiliate.Domain;
using AliExpress.Affiliate.Application.Ports;

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
        string productUrl,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productUrl) ||
            !Uri.TryCreate(productUrl, UriKind.Absolute, out _))
        {
            return null;
        }

        options.Validate();

        using var timeoutCts = CreateTimeoutTokenSource(options, cancellationToken);

        var sourceUrl = AliExpressProductUrl.Normalize(productUrl);
        var linkLookup = await _affiliateProvider.GenerateAffiliateLinkAsync(
            sourceUrl,
            options,
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
        if (options.IncludeProductDetails &&
            AliExpressProductId.TryFromUrl(sourceUrl, out var productId))
        {
            productDetails = await GetProductDetailsCoreAsync(
                productId,
                options,
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
}
