using AliExpress.Affiliate.Application.Requests;
using AliExpress.Affiliate.Configuration;
using AliExpress.Affiliate.OpenPlatform;
using System.Globalization;

namespace AliExpress.Affiliate.Infrastructure.OpenPlatform;

internal static class AliExpressOpenPlatformRequestFactory
{
    private const string LinkGenerateMethod = "aliexpress.affiliate.link.generate";
    private const string ProductDetailMethod = "aliexpress.affiliate.productdetail.get";
    private const string JsonFormat = "json";
    private const string ApiVersion = "2.0";

    public static AliExpressOpenPlatformRequest BuildLinkGenerateRequest(
        string productUrl,
        AliExpressAffiliateLinkRequest request,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp)
    {
        options.Validate();

        var allParameters = BuildCommonParameters(LinkGenerateMethod, options, timestamp);
        allParameters["promotion_link_type"] = OpenPlatformText.FirstNonEmpty(
            request.PromotionLinkType,
            options.DefaultPromotionLinkType,
            AliExpressAffiliateOptions.FallbackPromotionLinkType);
        allParameters["source_values"] = productUrl;
        allParameters["tracking_id"] = OpenPlatformText.FirstNonEmpty(request.TrackingId, options.DefaultTrackingId);

        AddIfNotEmpty(allParameters, "app_signature", options.AppSignature);

        allParameters["sign"] = AliExpressOpenPlatformSigner.CreateTopSignature(
            allParameters,
            options.AppSecret,
            allParameters["sign_method"]);

        return new AliExpressOpenPlatformRequest(
            BuildEndpointUri(options.ApiEndpoint),
            allParameters,
            allParameters);
    }

    public static AliExpressOpenPlatformRequest BuildProductDetailRequest(
        string productId,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp)
    {
        options.Validate();

        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException("Product id is required.", nameof(productId));
        }

        var allParameters = BuildCommonParameters(ProductDetailMethod, options, timestamp);
        allParameters["product_ids"] = productId.Trim();
        allParameters["tracking_id"] = options.DefaultTrackingId;

        AddIfNotEmpty(allParameters, "target_currency", options.DefaultTargetCurrency);
        AddIfNotEmpty(allParameters, "target_language", options.DefaultTargetLanguage);
        AddIfNotEmpty(allParameters, "ship_to_country", options.DefaultShipToCountry);
        AddIfNotEmpty(allParameters, "country", options.DefaultShipToCountry);
        AddIfNotEmpty(allParameters, "app_signature", options.AppSignature);

        allParameters["sign"] = AliExpressOpenPlatformSigner.CreateTopSignature(
            allParameters,
            options.AppSecret,
            allParameters["sign_method"]);

        return new AliExpressOpenPlatformRequest(
            BuildEndpointUri(options.ApiEndpoint),
            allParameters,
            allParameters);
    }

    public static AliExpressOpenPlatformRequest BuildApiRequest(
        string method,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        IReadOnlyDictionary<string, string>? apiParameters = null)
    {
        options.Validate();

        if (string.IsNullOrWhiteSpace(method))
        {
            throw new ArgumentException("API method is required.", nameof(method));
        }

        var allParameters = BuildCommonParameters(method.Trim(), options, timestamp);

        if (apiParameters != null)
        {
            foreach (var parameter in apiParameters)
            {
                AddIfNotEmpty(allParameters, parameter.Key, parameter.Value);
            }
        }

        AddIfNotEmpty(allParameters, "app_signature", options.AppSignature);

        allParameters["sign"] = AliExpressOpenPlatformSigner.CreateTopSignature(
            allParameters,
            options.AppSecret,
            allParameters["sign_method"]);

        return new AliExpressOpenPlatformRequest(
            BuildEndpointUri(options.ApiEndpoint),
            allParameters,
            allParameters);
    }

    private static SortedDictionary<string, string> BuildCommonParameters(
        string method,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp)
    {
        return new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["method"] = method,
            ["app_key"] = options.AppKey,
            ["timestamp"] = timestamp.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
            ["format"] = JsonFormat,
            ["sign_method"] = AliExpressOpenPlatformSigner.NormalizeSignMethod(options.SignMethod),
            ["v"] = ApiVersion
        };
    }

    private static Uri BuildEndpointUri(string endpoint)
    {
        var endpointUri = Uri.TryCreate(endpoint, UriKind.Absolute, out var parsedEndpoint)
            ? parsedEndpoint
            : new Uri(AliExpressAffiliateOptions.DefaultEndpoint);

        return new UriBuilder(endpointUri)
        {
            Query = string.Empty
        }.Uri;
    }

    private static void AddIfNotEmpty(
        IDictionary<string, string> parameters,
        string key,
        string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parameters[key] = value.Trim();
        }
    }

}
