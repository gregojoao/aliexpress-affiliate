using AliExpress.Affiliate.Domain;
using System.Text.Json;

namespace AliExpress.Affiliate.Infrastructure.OpenPlatform;

internal static class AliExpressAffiliateLinkResponseMapper
{
    private static readonly string[] PromotionLinkPropertyNames =
    {
        "promotion_link",
        "promotionLink",
        "short_url",
        "shortUrl"
    };

    private static readonly string[] PromotionLinkContainerPropertyNames =
    {
        "promotion_link_dto",
        "promotionLinkDto",
        "promotion_links",
        "promotionLinks"
    };

    public static string ExtractAffiliateUrl(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var result = OpenPlatformResponseEnvelope.ExtractNamedResult(
            document.RootElement,
            "aliexpress_affiliate_link_generate_response");

        if (OpenPlatformJsonReader.TryGetProperty(result, "promotion_links", out var promotionLinks) &&
            TryExtractPromotionLink(promotionLinks, out var affiliateUrl))
        {
            return affiliateUrl;
        }

        return TryExtractPromotionLink(result, out affiliateUrl)
            ? affiliateUrl
            : string.Empty;
    }

    public static IReadOnlyList<AliExpressAffiliateLink> ExtractAffiliateLinks(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var result = OpenPlatformResponseEnvelope.ExtractResult(document.RootElement);

        if (!OpenPlatformJsonReader.TryGetProperty(result, "promotion_links", out var promotionLinks))
        {
            return Array.Empty<AliExpressAffiliateLink>();
        }

        return OpenPlatformJsonReader.EnumerateItems(promotionLinks, "promotion_link").Select(item =>
            new AliExpressAffiliateLink(
                SourceValue: OpenPlatformJsonReader.GetPropertyString(item, "source_value"),
                PromotionLink: OpenPlatformText.FirstNonEmpty(
                    OpenPlatformJsonReader.GetPropertyString(item, "promotion_link"),
                    OpenPlatformJsonReader.GetPropertyString(item, "short_url")),
                RawJson: item.GetRawText())).ToArray();
    }

    public static string SummarizeLinkGenerateResponse(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;
            var response = OpenPlatformJsonReader.TryGetProperty(root, "aliexpress_affiliate_link_generate_response", out var responseElement)
                ? responseElement
                : root;
            var responseResult = OpenPlatformJsonReader.TryGetProperty(response, "resp_result", out var responseResultElement)
                ? responseResultElement
                : response;
            var result = OpenPlatformJsonReader.TryGetProperty(responseResult, "result", out var resultElement)
                ? resultElement
                : responseResult;

            var respCode = OpenPlatformJsonReader.GetPropertyString(responseResult, "resp_code");
            var respMsg = OpenPlatformJsonReader.GetPropertyString(responseResult, "resp_msg");
            var message = OpenPlatformJsonReader.GetPropertyString(result, "message");
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

        foreach (var propertyName in PromotionLinkPropertyNames)
        {
            if (OpenPlatformJsonReader.TryGetProperty(element, propertyName, out var propertyValue) &&
                TryExtractPromotionLink(propertyValue, out affiliateUrl))
            {
                return true;
            }
        }

        foreach (var propertyName in PromotionLinkContainerPropertyNames)
        {
            if (OpenPlatformJsonReader.TryGetProperty(element, propertyName, out var propertyValue) &&
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

        if (OpenPlatformJsonReader.TryGetProperty(element, "source_value", out var sourceValue))
        {
            return OpenPlatformJsonReader.GetScalarString(sourceValue);
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
}
