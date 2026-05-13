using AliExpress.Affiliate.Application;
using AliExpress.Affiliate.Domain;
using AliExpress.Affiliate.Infrastructure.OpenPlatform;

namespace AliExpress.Affiliate;

public sealed class AliExpressAffiliateClient
{
    private readonly AliExpressAffiliateService _affiliateService;

    public AliExpressAffiliateClient(
        HttpClient httpClient,
        Func<DateTimeOffset>? utcNow = null)
    {
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

    public Task<AliExpressProductDetails?> GetProductDetailsAsync(
        string productIdOrUrl,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        return _affiliateService.GetProductDetailsAsync(productIdOrUrl, options, cancellationToken);
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
}
