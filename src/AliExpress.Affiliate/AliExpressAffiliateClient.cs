using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AliExpress.Affiliate;

public sealed class AliExpressAffiliateClient
{
    private const string LinkGenerateMethod = "aliexpress.affiliate.link.generate";
    private const string ProductDetailMethod = "aliexpress.affiliate.productdetail.get";
    private const string JsonFormat = "json";
    private const string ApiVersion = "2.0";

    private readonly HttpClient _httpClient;
    private readonly Func<DateTimeOffset> _utcNow;

    public AliExpressAffiliateClient(
        HttpClient httpClient,
        Func<DateTimeOffset>? utcNow = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
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

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(Math.Max(options.TimeoutMilliseconds, 1)));

        var sourceUrl = NormalizeAliExpressUrl(productUrl);
        var linkRequest = BuildLinkGenerateRequest(sourceUrl, options, _utcNow());
        var linkResponseBody = await ExecuteAsync(linkRequest, timeoutCts.Token);
        var affiliateUrl = ExtractAffiliateUrl(linkResponseBody);

        if (string.IsNullOrWhiteSpace(affiliateUrl))
        {
            throw new AliExpressAffiliateLinkUnavailableException(
                sourceUrl,
                "The AliExpress API response did not contain promotion_link.",
                SummarizeLinkGenerateResponse(linkResponseBody));
        }

        AliExpressProductDetails? productDetails = null;
        if (options.IncludeProductDetails &&
            TryExtractProductId(sourceUrl, out var productId))
        {
            productDetails = await GetProductDetailsAsync(
                productId,
                options,
                timeoutCts.Token);
        }

        affiliateUrl = FirstNonEmpty(affiliateUrl, productDetails?.PromotionLink ?? string.Empty);

        return new AliExpressAffiliateLinkResult(
            AffiliateUrl: affiliateUrl,
            SourceUrl: sourceUrl,
            ProductUrl: productDetails?.ProductUrl ?? sourceUrl,
            FinalProductUrl: productDetails?.ProductUrl ?? sourceUrl,
            ProductTitle: productDetails?.ProductTitle ?? string.Empty,
            ProductPrice: productDetails?.ProductPrice ?? string.Empty,
            ProductOriginalPrice: productDetails?.ProductOriginalPrice ?? string.Empty,
            ProductImageUrl: productDetails?.ProductImageUrl ?? string.Empty,
            ProductDetails: productDetails);
    }

    public async Task<AliExpressProductDetails?> GetProductDetailsAsync(
        string productIdOrUrl,
        AliExpressAffiliateOptions options,
        CancellationToken cancellationToken = default)
    {
        options.Validate();

        var productId = TryExtractProductId(productIdOrUrl, out var extractedProductId)
            ? extractedProductId
            : productIdOrUrl.Trim();

        if (string.IsNullOrWhiteSpace(productId))
        {
            return null;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(Math.Max(options.TimeoutMilliseconds, 1)));

        var request = BuildProductDetailRequest(productId, options, _utcNow());
        var responseBody = await ExecuteAsync(request, timeoutCts.Token);
        return ExtractProductDetails(responseBody);
    }

    public static AliExpressOpenPlatformRequest BuildLinkGenerateRequest(
        string productUrl,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp)
    {
        options.Validate();

        var allParameters = BuildCommonParameters(LinkGenerateMethod, options, timestamp);
        allParameters["promotion_link_type"] = FirstNonEmpty(
            options.PromotionLinkType,
            AliExpressAffiliateOptions.DefaultPromotionLinkType);
        allParameters["source_values"] = productUrl;
        allParameters["tracking_id"] = options.TrackingId;

        AddIfNotEmpty(allParameters, "app_signature", options.AppSignature);

        allParameters["sign"] = CreateTopSignature(
            allParameters,
            options.AppSecret,
            allParameters["sign_method"]);

        return new AliExpressOpenPlatformRequest(
            BuildEndpointUri(options.Endpoint),
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
        allParameters["tracking_id"] = options.TrackingId;

        AddIfNotEmpty(allParameters, "target_currency", options.TargetCurrency);
        AddIfNotEmpty(allParameters, "target_language", options.TargetLanguage);
        AddIfNotEmpty(allParameters, "ship_to_country", options.ShipToCountry);
        AddIfNotEmpty(allParameters, "country", options.ShipToCountry);
        AddIfNotEmpty(allParameters, "app_signature", options.AppSignature);

        allParameters["sign"] = CreateTopSignature(
            allParameters,
            options.AppSecret,
            allParameters["sign_method"]);

        return new AliExpressOpenPlatformRequest(
            BuildEndpointUri(options.Endpoint),
            allParameters,
            allParameters);
    }

    public static string NormalizeAliExpressUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        var trimmed = url.Trim();

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
            HostMatches(uri.Host, "aliexpress.com"))
        {
            return TrimAfterHtml(trimmed);
        }

        return trimmed;
    }

    public static bool TryExtractProductId(
        string productUrl,
        out string productId)
    {
        productId = string.Empty;

        if (string.IsNullOrWhiteSpace(productUrl))
        {
            return false;
        }

        var match = Regex.Match(
            productUrl,
            @"/item/(?<id>\d+)\.html",
            RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            match = Regex.Match(
                Uri.UnescapeDataString(productUrl),
                @"(?:orig_item_id|orig_sl_item_id|x_object_id|productIds)[^\d]*(?<id>\d{10,})",
                RegexOptions.IgnoreCase);
        }

        productId = match.Success
            ? match.Groups["id"].Value
            : string.Empty;

        return !string.IsNullOrWhiteSpace(productId);
    }

    public static string CreateTopSignature(
        IReadOnlyDictionary<string, string> parameters,
        string appSecret,
        string signMethod)
    {
        if (string.IsNullOrWhiteSpace(appSecret))
        {
            throw new ArgumentException("App secret is required.", nameof(appSecret));
        }

        var baseString = BuildSignatureSourceString(parameters);

        return NormalizeSignMethod(signMethod) switch
        {
            "md5" => Convert.ToHexString(
                MD5.HashData(Encoding.UTF8.GetBytes(appSecret + baseString + appSecret))),
            "hmac" => ComputeHmacMd5(baseString, appSecret),
            "sha256" => ComputeHmacSha256(baseString, appSecret),
            _ => throw new ArgumentOutOfRangeException(nameof(signMethod), signMethod, "Invalid AliExpress sign method.")
        };
    }

    public static string BuildSignatureSourceString(
        IEnumerable<KeyValuePair<string, string>> parameters)
    {
        return string.Concat(
            parameters
                .Where(parameter =>
                    !string.Equals(parameter.Key, "sign", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(parameter.Value))
                .OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
                .Select(parameter => parameter.Key + parameter.Value));
    }

    public static string ExtractAffiliateUrl(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;

        ThrowIfTopError(root);

        var response = TryGetProperty(root, "aliexpress_affiliate_link_generate_response", out var responseElement)
            ? responseElement
            : root;

        var responseResult = TryGetProperty(response, "resp_result", out var responseResultElement)
            ? responseResultElement
            : response;

        ThrowIfBusinessError(responseResult);

        var result = TryGetProperty(responseResult, "result", out var resultElement)
            ? resultElement
            : responseResult;

        if (TryGetProperty(result, "promotion_links", out var promotionLinks) &&
            TryExtractPromotionLink(promotionLinks, out var affiliateUrl))
        {
            return affiliateUrl;
        }

        return TryExtractPromotionLink(result, out affiliateUrl)
            ? affiliateUrl
            : string.Empty;
    }

    public static AliExpressProductDetails? ExtractProductDetails(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;

        ThrowIfTopError(root);

        var response = TryGetProperty(root, "aliexpress_affiliate_productdetail_get_response", out var responseElement)
            ? responseElement
            : root;

        var responseResult = TryGetProperty(response, "resp_result", out var responseResultElement)
            ? responseResultElement
            : response;

        ThrowIfBusinessError(responseResult);

        var result = TryGetProperty(responseResult, "result", out var resultElement)
            ? resultElement
            : responseResult;

        if (!TryGetProperty(result, "products", out var productsElement) ||
            !TryGetFirstProduct(productsElement, out var product))
        {
            return null;
        }

        var currency = FirstNonEmpty(
            GetPropertyString(product, "target_sale_price_currency"),
            GetPropertyString(product, "target_app_sale_price_currency"),
            GetPropertyString(product, "target_original_price_currency"));
        var productPrice = FormatMoney(
            FirstNonEmpty(
                GetPropertyString(product, "target_sale_price"),
                GetPropertyString(product, "target_app_sale_price"),
                GetPropertyString(product, "sale_price"),
                GetPropertyString(product, "app_sale_price")),
            currency);
        var productOriginalPrice = FormatMoney(
            FirstNonEmpty(
                GetPropertyString(product, "target_original_price"),
                GetPropertyString(product, "original_price")),
            currency);

        return new AliExpressProductDetails(
            ProductTitle: GetPropertyString(product, "product_title"),
            ProductPrice: productPrice,
            ProductOriginalPrice: productOriginalPrice,
            ProductImageUrl: GetPropertyString(product, "product_main_image_url"),
            ProductUrl: GetPropertyString(product, "product_detail_url"),
            PromotionLink: GetPropertyString(product, "promotion_link"));
    }

    private async Task<string> ExecuteAsync(
        AliExpressOpenPlatformRequest request,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(request.BodyParameters);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded")
        {
            CharSet = "utf-8"
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, request.RequestUri)
        {
            Content = content
        };

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"AliExpress API returned HTTP {(int)response.StatusCode}: {responseBody}");
        }

        return responseBody;
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
            ["sign_method"] = NormalizeSignMethod(options.SignMethod),
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

    private static string SummarizeLinkGenerateResponse(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            var response = TryGetProperty(root, "aliexpress_affiliate_link_generate_response", out var responseElement)
                ? responseElement
                : root;

            var responseResult = TryGetProperty(response, "resp_result", out var responseResultElement)
                ? responseResultElement
                : response;

            var result = TryGetProperty(responseResult, "result", out var resultElement)
                ? resultElement
                : responseResult;

            var respCode = GetPropertyString(responseResult, "resp_code");
            var respMsg = GetPropertyString(responseResult, "resp_msg");
            var message = GetPropertyString(result, "message");
            var sourceValue = ExtractFirstSourceValue(result);

            return string.Join(
                "; ",
                new[]
                {
                    string.IsNullOrWhiteSpace(respCode) ? string.Empty : $"resp_code={respCode}",
                    string.IsNullOrWhiteSpace(respMsg) ? string.Empty : $"resp_msg={respMsg}",
                    string.IsNullOrWhiteSpace(message) ? string.Empty : $"message={message}",
                    string.IsNullOrWhiteSpace(sourceValue) ? string.Empty : $"source_value={sourceValue}",
                    "promotion_link=missing"
                }.Where(part => !string.IsNullOrWhiteSpace(part)));
        }
        catch (JsonException)
        {
            return "API response was not valid JSON; promotion_link=missing";
        }
    }

    private static string ExtractFirstSourceValue(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var value = ExtractFirstSourceValue(item);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        if (TryGetProperty(element, "source_value", out var sourceValue))
        {
            return GetScalarString(sourceValue);
        }

        foreach (var property in element.EnumerateObject())
        {
            var value = ExtractFirstSourceValue(property.Value);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static void ThrowIfTopError(JsonElement root)
    {
        if (TryGetProperty(root, "error_response", out var errorResponse))
        {
            throw new InvalidOperationException(BuildErrorMessage("AliExpress API", errorResponse));
        }
    }

    private static void ThrowIfBusinessError(JsonElement responseResult)
    {
        if (!TryGetProperty(responseResult, "resp_code", out var responseCodeElement))
        {
            return;
        }

        var responseCode = GetScalarString(responseCodeElement);
        if (string.IsNullOrWhiteSpace(responseCode) ||
            responseCode.Equals("200", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(BuildErrorMessage($"AliExpress API resp_code {responseCode}", responseResult));
    }

    private static string BuildErrorMessage(
        string prefix,
        JsonElement element)
    {
        var code = FirstNonEmpty(
            GetPropertyString(element, "code"),
            GetPropertyString(element, "sub_code"),
            GetPropertyString(element, "resp_code"));
        var message = FirstNonEmpty(
            GetPropertyString(element, "msg"),
            GetPropertyString(element, "sub_msg"),
            GetPropertyString(element, "message"),
            GetPropertyString(element, "resp_msg"));

        if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(message))
        {
            return $"{prefix} returned an error.";
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return $"{prefix}: {message}";
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return $"{prefix}: {code}";
        }

        return $"{prefix}: {code} - {message}";
    }

    private static bool TryExtractPromotionLink(
        JsonElement element,
        out string affiliateUrl)
    {
        affiliateUrl = string.Empty;

        if (element.ValueKind == JsonValueKind.String)
        {
            var value = element.GetString()?.Trim() ?? string.Empty;
            if (IsHttpUrl(value))
            {
                affiliateUrl = value;
                return true;
            }

            return false;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (TryExtractPromotionLink(item, out affiliateUrl))
                {
                    return true;
                }
            }

            return false;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var propertyName in new[]
                 {
                     "promotion_link",
                     "promotionLink",
                     "short_url",
                     "shortUrl"
                 })
        {
            if (TryGetProperty(element, propertyName, out var propertyValue) &&
                TryExtractPromotionLink(propertyValue, out affiliateUrl))
            {
                return true;
            }
        }

        foreach (var propertyName in new[]
                 {
                     "promotion_link_dto",
                     "promotionLinkDto",
                     "promotion_links",
                     "promotionLinks"
                 })
        {
            if (TryGetProperty(element, propertyName, out var propertyValue) &&
                TryExtractPromotionLink(propertyValue, out affiliateUrl))
            {
                return true;
            }
        }

        foreach (var property in element.EnumerateObject())
        {
            if (IsOriginalOrProductUrlField(property.Name))
            {
                continue;
            }

            if (TryExtractPromotionLink(property.Value, out affiliateUrl))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetFirstProduct(
        JsonElement productsElement,
        out JsonElement product)
    {
        product = default;

        if (productsElement.ValueKind == JsonValueKind.Array)
        {
            product = productsElement.EnumerateArray().FirstOrDefault();
            return product.ValueKind == JsonValueKind.Object;
        }

        if (productsElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (TryGetProperty(productsElement, "product", out var productElement))
        {
            if (productElement.ValueKind == JsonValueKind.Array)
            {
                product = productElement.EnumerateArray().FirstOrDefault();
                return product.ValueKind == JsonValueKind.Object;
            }

            if (productElement.ValueKind == JsonValueKind.Object)
            {
                product = productElement;
                return true;
            }
        }

        product = productsElement;
        return product.ValueKind == JsonValueKind.Object;
    }

    private static bool TryGetProperty(
        JsonElement element,
        string propertyName,
        out JsonElement propertyValue)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out propertyValue))
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    propertyValue = property.Value;
                    return true;
                }
            }
        }

        propertyValue = default;
        return false;
    }

    private static string GetPropertyString(
        JsonElement element,
        string propertyName)
    {
        return TryGetProperty(element, propertyName, out var propertyValue)
            ? GetScalarString(propertyValue)
            : string.Empty;
    }

    private static string GetScalarString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => string.Empty
        };
    }

    private static string FormatMoney(
        string value,
        string currency)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            return value;
        }

        return currency.Equals("BRL", StringComparison.OrdinalIgnoreCase)
            ? string.Format(CultureInfo.GetCultureInfo("pt-BR"), "R$ {0:N2}", amount)
            : string.IsNullOrWhiteSpace(currency)
                ? amount.ToString("N2", CultureInfo.InvariantCulture)
                : string.Format(CultureInfo.InvariantCulture, "{0} {1:N2}", currency, amount);
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

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }

    private static string NormalizeSignMethod(string signMethod)
    {
        var normalized = string.IsNullOrWhiteSpace(signMethod)
            ? AliExpressAffiliateOptions.DefaultSignMethod
            : signMethod.Trim().ToLowerInvariant();

        return normalized switch
        {
            "md5" => "md5",
            "hmac" => "hmac",
            "hmac-md5" => "hmac",
            "hmacsha256" => "sha256",
            "hmac-sha256" => "sha256",
            "sha256" => "sha256",
            _ => throw new InvalidOperationException("Invalid AliExpress sign method. Use hmac, md5 or sha256.")
        };
    }

    private static string ComputeHmacSha256(
        string value,
        string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private static string ComputeHmacMd5(
        string value,
        string secret)
    {
        using var hmac = new HMACMD5(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool IsOriginalOrProductUrlField(string propertyName)
    {
        return propertyName.Equals("source_value", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("sourceValue", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("product_detail_url", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("productDetailUrl", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("product_url", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("productUrl", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HostMatches(string candidateHost, string configuredHost)
    {
        return candidateHost.Equals(configuredHost, StringComparison.OrdinalIgnoreCase) ||
               candidateHost.EndsWith($".{configuredHost}", StringComparison.OrdinalIgnoreCase);
    }

    private static string TrimAfterHtml(string value)
    {
        var htmlIndex = value.IndexOf(".html", StringComparison.OrdinalIgnoreCase);

        return htmlIndex >= 0
            ? value[..(htmlIndex + ".html".Length)]
            : value;
    }
}
