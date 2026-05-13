using AliExpress.Affiliate.Application.Ports;
using AliExpress.Affiliate.Domain;

namespace AliExpress.Affiliate.Infrastructure.OpenPlatform;

internal sealed class AliExpressOpenPlatformAffiliateProvider : IAliExpressAffiliateProvider
{
    private readonly IAliExpressOpenPlatformGateway _gateway;

    public AliExpressOpenPlatformAffiliateProvider(IAliExpressOpenPlatformGateway gateway)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
    }

    public async Task<AffiliateLinkLookup> GenerateAffiliateLinkAsync(
        string sourceUrl,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var request = AliExpressOpenPlatformRequestFactory.BuildLinkGenerateRequest(sourceUrl, options, timestamp);
        var responseBody = await _gateway.SendAsync(request, cancellationToken);
        var affiliateUrl = AliExpressOpenPlatformResponseParser.ExtractAffiliateUrl(responseBody);
        var missingLinkSummary = string.IsNullOrWhiteSpace(affiliateUrl)
            ? AliExpressOpenPlatformResponseParser.SummarizeLinkGenerateResponse(responseBody)
            : string.Empty;

        return new AffiliateLinkLookup(affiliateUrl, missingLinkSummary);
    }

    public async Task<AliExpressProductDetails?> GetProductDetailsAsync(
        AliExpressProductId productId,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var request = AliExpressOpenPlatformRequestFactory.BuildProductDetailRequest(
            productId.Value,
            options,
            timestamp);
        var responseBody = await _gateway.SendAsync(request, cancellationToken);

        return AliExpressOpenPlatformResponseParser.ExtractProductDetails(responseBody);
    }
}
