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
}
