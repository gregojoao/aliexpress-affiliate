using System.Text.Json;
using AliExpress.Affiliate.Domain;

namespace AliExpress.Affiliate.Infrastructure.OpenPlatform;

internal static class AliExpressOpenPlatformResponseParser
{
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
        var productPrice = ProductPriceFormatter.FormatMoney(
            FirstNonEmpty(
                GetPropertyString(product, "target_sale_price"),
                GetPropertyString(product, "target_app_sale_price"),
                GetPropertyString(product, "sale_price"),
                GetPropertyString(product, "app_sale_price")),
            currency);
        var productOriginalPrice = ProductPriceFormatter.FormatMoney(
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

    public static IReadOnlyList<AliExpressAffiliateLink> ExtractAffiliateLinks(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var result = ExtractResult(document.RootElement);

        if (!TryGetProperty(result, "promotion_links", out var promotionLinks))
        {
            return Array.Empty<AliExpressAffiliateLink>();
        }

        return EnumerateItems(promotionLinks, "promotion_link").Select(item =>
            new AliExpressAffiliateLink(
                SourceValue: GetPropertyString(item, "source_value"),
                PromotionLink: FirstNonEmpty(
                    GetPropertyString(item, "promotion_link"),
                    GetPropertyString(item, "short_url")),
                RawJson: item.GetRawText())).ToArray();
    }

    public static AliExpressAffiliateApiResult<AliExpressAffiliateProduct> ExtractProducts(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var result = ExtractResult(document.RootElement);

        var products = TryGetProperty(result, "products", out var productsElement)
            ? EnumerateItems(productsElement, "product")
                .Select(CreateProduct)
                .ToArray()
            : Array.Empty<AliExpressAffiliateProduct>();

        return CreateResult(responseBody, result, products);
    }

    public static AliExpressAffiliateApiResult<AliExpressAffiliateCategory> ExtractCategories(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var result = ExtractResult(document.RootElement);

        var categories = TryGetProperty(result, "categories", out var categoriesElement)
            ? EnumerateItems(categoriesElement, "category")
                .Select(category => new AliExpressAffiliateCategory(
                    CategoryId: GetPropertyString(category, "category_id"),
                    CategoryName: GetPropertyString(category, "category_name"),
                    ParentCategoryId: GetPropertyString(category, "parent_category_id"),
                    RawJson: category.GetRawText()))
                .ToArray()
            : Array.Empty<AliExpressAffiliateCategory>();

        return CreateResult(responseBody, result, categories);
    }

    public static AliExpressAffiliateApiResult<AliExpressAffiliateFeaturedPromo> ExtractFeaturedPromos(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var result = ExtractResult(document.RootElement);

        var promos = TryGetProperty(result, "promos", out var promosElement)
            ? EnumerateItems(promosElement, "promo")
                .Select(promo => new AliExpressAffiliateFeaturedPromo(
                    PromoName: GetPropertyString(promo, "promo_name"),
                    PromoDescription: GetPropertyString(promo, "promo_desc"),
                    ProductCount: GetPropertyString(promo, "product_num"),
                    RawJson: promo.GetRawText()))
                .ToArray()
            : Array.Empty<AliExpressAffiliateFeaturedPromo>();

        return CreateResult(responseBody, result, promos);
    }

    public static AliExpressAffiliateApiResult<AliExpressAffiliateOrder> ExtractOrders(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var result = ExtractResult(document.RootElement);

        var orders = TryGetProperty(result, "orders", out var ordersElement)
            ? EnumerateItems(ordersElement, "order")
                .Select(order => new AliExpressAffiliateOrder(
                    OrderId: GetPropertyString(order, "order_id"),
                    SubOrderId: GetPropertyString(order, "sub_order_id"),
                    OrderNumber: GetPropertyString(order, "order_number"),
                    OrderStatus: GetPropertyString(order, "order_status"),
                    TrackingId: GetPropertyString(order, "tracking_id"),
                    ProductId: GetPropertyString(order, "product_id"),
                    ProductTitle: GetPropertyString(order, "product_title"),
                    ProductDetailUrl: GetPropertyString(order, "product_detail_url"),
                    CommissionRate: GetPropertyString(order, "commission_rate"),
                    EstimatedCommission: FirstNonEmpty(
                        GetPropertyString(order, "estimated_commission"),
                        GetPropertyString(order, "estimated_finished_commission"),
                        GetPropertyString(order, "estimated_paid_commission")),
                    PaidCommission: FirstNonEmpty(
                        GetPropertyString(order, "paid_commission"),
                        GetPropertyString(order, "finished_commission")),
                    CreatedTime: GetPropertyString(order, "created_time"),
                    FinishedTime: GetPropertyString(order, "finished_time"),
                    RawJson: order.GetRawText()))
                .ToArray()
            : Array.Empty<AliExpressAffiliateOrder>();

        return CreateResult(responseBody, result, orders);
    }

    public static string SummarizeLinkGenerateResponse(string responseBody)
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

    private static JsonElement ExtractResult(JsonElement root)
    {
        ThrowIfTopError(root);

        var response = root;
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (property.Name.EndsWith("_response", StringComparison.OrdinalIgnoreCase) &&
                    !property.Name.Equals("error_response", StringComparison.OrdinalIgnoreCase))
                {
                    response = property.Value;
                    break;
                }
            }
        }

        var responseResult = TryGetProperty(response, "resp_result", out var responseResultElement)
            ? responseResultElement
            : response;

        ThrowIfBusinessError(responseResult);

        return TryGetProperty(responseResult, "result", out var resultElement)
            ? resultElement
            : responseResult;
    }

    private static AliExpressAffiliateApiResult<T> CreateResult<T>(
        string responseBody,
        JsonElement result,
        IReadOnlyList<T> items)
    {
        return new AliExpressAffiliateApiResult<T>(
            Items: items,
            CurrentPageNo: GetPropertyInt(result, "current_page_no"),
            CurrentRecordCount: GetPropertyInt(result, "current_record_count"),
            TotalPageNo: GetPropertyInt(result, "total_page_no"),
            TotalRecordCount: FirstNonZero(
                GetPropertyInt(result, "total_record_count"),
                GetPropertyInt(result, "total_result_count")),
            IsFinished: GetPropertyBool(result, "is_finished"),
            RawJson: responseBody);
    }

    private static AliExpressAffiliateProduct CreateProduct(JsonElement product)
    {
        var currency = FirstNonEmpty(
            GetPropertyString(product, "target_sale_price_currency"),
            GetPropertyString(product, "target_app_sale_price_currency"),
            GetPropertyString(product, "sale_price_currency"),
            GetPropertyString(product, "app_sale_price_currency"),
            GetPropertyString(product, "target_original_price_currency"),
            GetPropertyString(product, "original_price_currency"));
        var salePrice = FirstNonEmpty(
            GetPropertyString(product, "target_sale_price"),
            GetPropertyString(product, "target_app_sale_price"),
            GetPropertyString(product, "sale_price"),
            GetPropertyString(product, "app_sale_price"));
        var originalPrice = FirstNonEmpty(
            GetPropertyString(product, "target_original_price"),
            GetPropertyString(product, "original_price"));

        return new AliExpressAffiliateProduct(
            ProductId: GetPropertyString(product, "product_id"),
            ProductTitle: GetPropertyString(product, "product_title"),
            ProductUrl: GetPropertyString(product, "product_detail_url"),
            PromotionLink: GetPropertyString(product, "promotion_link"),
            ProductImageUrl: GetPropertyString(product, "product_main_image_url"),
            ProductPrice: ProductPriceFormatter.FormatMoney(salePrice, currency),
            ProductOriginalPrice: ProductPriceFormatter.FormatMoney(originalPrice, currency),
            SalePrice: salePrice,
            OriginalPrice: originalPrice,
            Currency: currency,
            CommissionRate: GetPropertyString(product, "commission_rate"),
            HotProductCommissionRate: GetPropertyString(product, "hot_product_commission_rate"),
            Discount: GetPropertyString(product, "discount"),
            EvaluateRate: GetPropertyString(product, "evaluate_rate"),
            LatestVolume: FirstNonEmpty(
                GetPropertyString(product, "lastest_volume"),
                GetPropertyString(product, "latest_volume")),
            FirstLevelCategoryId: GetPropertyString(product, "first_level_category_id"),
            FirstLevelCategoryName: GetPropertyString(product, "first_level_category_name"),
            SecondLevelCategoryId: GetPropertyString(product, "second_level_category_id"),
            SecondLevelCategoryName: GetPropertyString(product, "second_level_category_name"),
            ShopId: GetPropertyString(product, "shop_id"),
            ShopUrl: GetPropertyString(product, "shop_url"),
            PlatformProductType: GetPropertyString(product, "platform_product_type"),
            RawJson: product.GetRawText());
    }

    private static IEnumerable<JsonElement> EnumerateItems(
        JsonElement element,
        string itemPropertyName)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object)
                {
                    yield return item;
                }
            }

            yield break;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        if (TryGetProperty(element, itemPropertyName, out var items))
        {
            foreach (var item in EnumerateItems(items, itemPropertyName))
            {
                yield return item;
            }

            yield break;
        }

        yield return element;
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

    private static int GetPropertyInt(
        JsonElement element,
        string propertyName)
    {
        var value = GetPropertyString(element, propertyName);

        return int.TryParse(value, out var parsed)
            ? parsed
            : 0;
    }

    private static bool GetPropertyBool(
        JsonElement element,
        string propertyName)
    {
        if (!TryGetProperty(element, propertyName, out var propertyValue))
        {
            return false;
        }

        return propertyValue.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => propertyValue.GetString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true,
            _ => false
        };
    }

    private static int FirstNonZero(params int[] values)
    {
        return values.FirstOrDefault(value => value != 0);
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

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }
}
