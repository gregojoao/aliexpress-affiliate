using AliExpress.Affiliate.Application.Requests;
using AliExpress.Affiliate.Configuration;
using AliExpress.Affiliate.Domain;
using AliExpress.Affiliate.Infrastructure.OpenPlatform;

namespace AliExpress.Affiliate.OpenPlatform;

public static class AliExpressOpenPlatformDiagnostics
{
    public static AliExpressOpenPlatformRequest BuildLinkGenerateRequest(
        string productUrl,
        AliExpressAffiliateLinkRequest request,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp)
    {
        return AliExpressOpenPlatformRequestFactory.BuildLinkGenerateRequest(productUrl, request, options, timestamp);
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
}
