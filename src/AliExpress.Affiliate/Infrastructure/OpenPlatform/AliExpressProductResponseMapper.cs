using AliExpress.Affiliate.Domain;
using System.Text.Json;

namespace AliExpress.Affiliate.Infrastructure.OpenPlatform;

internal static class AliExpressProductResponseMapper
{
    public static AliExpressProductDetails? ExtractProductDetails(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var result = OpenPlatformResponseEnvelope.ExtractNamedResult(
            document.RootElement,
            "aliexpress_affiliate_productdetail_get_response");

        if (!OpenPlatformJsonReader.TryGetProperty(result, "products", out var productsElement) ||
            !TryGetFirstProduct(productsElement, out var product))
        {
            return null;
        }

        var currency = OpenPlatformText.FirstNonEmpty(
            OpenPlatformJsonReader.GetPropertyString(product, "target_sale_price_currency"),
            OpenPlatformJsonReader.GetPropertyString(product, "target_app_sale_price_currency"),
            OpenPlatformJsonReader.GetPropertyString(product, "target_original_price_currency"));
        var productPrice = ProductPriceFormatter.FormatMoney(
            OpenPlatformText.FirstNonEmpty(
                OpenPlatformJsonReader.GetPropertyString(product, "target_sale_price"),
                OpenPlatformJsonReader.GetPropertyString(product, "target_app_sale_price"),
                OpenPlatformJsonReader.GetPropertyString(product, "sale_price"),
                OpenPlatformJsonReader.GetPropertyString(product, "app_sale_price")),
            currency);
        var productOriginalPrice = ProductPriceFormatter.FormatMoney(
            OpenPlatformText.FirstNonEmpty(
                OpenPlatformJsonReader.GetPropertyString(product, "target_original_price"),
                OpenPlatformJsonReader.GetPropertyString(product, "original_price")),
            currency);

        return new AliExpressProductDetails(
            ProductTitle: OpenPlatformJsonReader.GetPropertyString(product, "product_title"),
            ProductPrice: productPrice,
            ProductOriginalPrice: productOriginalPrice,
            ProductImageUrl: OpenPlatformJsonReader.GetPropertyString(product, "product_main_image_url"),
            ProductUrl: OpenPlatformJsonReader.GetPropertyString(product, "product_detail_url"),
            PromotionLink: OpenPlatformJsonReader.GetPropertyString(product, "promotion_link"));
    }

    public static AliExpressAffiliateApiResult<AliExpressAffiliateProduct> ExtractProducts(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var result = OpenPlatformResponseEnvelope.ExtractResult(document.RootElement);

        var products = OpenPlatformJsonReader.TryGetProperty(result, "products", out var productsElement)
            ? OpenPlatformJsonReader.EnumerateItems(productsElement, "product")
                .Select(CreateProduct)
                .ToArray()
            : Array.Empty<AliExpressAffiliateProduct>();

        return OpenPlatformApiResultFactory.Create(responseBody, result, products);
    }

    private static AliExpressAffiliateProduct CreateProduct(JsonElement product)
    {
        var currency = OpenPlatformText.FirstNonEmpty(
            OpenPlatformJsonReader.GetPropertyString(product, "target_sale_price_currency"),
            OpenPlatformJsonReader.GetPropertyString(product, "target_app_sale_price_currency"),
            OpenPlatformJsonReader.GetPropertyString(product, "sale_price_currency"),
            OpenPlatformJsonReader.GetPropertyString(product, "app_sale_price_currency"),
            OpenPlatformJsonReader.GetPropertyString(product, "target_original_price_currency"),
            OpenPlatformJsonReader.GetPropertyString(product, "original_price_currency"));
        var salePrice = OpenPlatformText.FirstNonEmpty(
            OpenPlatformJsonReader.GetPropertyString(product, "target_sale_price"),
            OpenPlatformJsonReader.GetPropertyString(product, "target_app_sale_price"),
            OpenPlatformJsonReader.GetPropertyString(product, "sale_price"),
            OpenPlatformJsonReader.GetPropertyString(product, "app_sale_price"));
        var originalPrice = OpenPlatformText.FirstNonEmpty(
            OpenPlatformJsonReader.GetPropertyString(product, "target_original_price"),
            OpenPlatformJsonReader.GetPropertyString(product, "original_price"));

        return new AliExpressAffiliateProduct(
            ProductId: OpenPlatformJsonReader.GetPropertyString(product, "product_id"),
            ProductTitle: OpenPlatformJsonReader.GetPropertyString(product, "product_title"),
            ProductUrl: OpenPlatformJsonReader.GetPropertyString(product, "product_detail_url"),
            PromotionLink: OpenPlatformJsonReader.GetPropertyString(product, "promotion_link"),
            ProductImageUrl: OpenPlatformJsonReader.GetPropertyString(product, "product_main_image_url"),
            ProductPrice: ProductPriceFormatter.FormatMoney(salePrice, currency),
            ProductOriginalPrice: ProductPriceFormatter.FormatMoney(originalPrice, currency),
            SalePrice: salePrice,
            OriginalPrice: originalPrice,
            Currency: currency,
            CommissionRate: OpenPlatformJsonReader.GetPropertyString(product, "commission_rate"),
            HotProductCommissionRate: OpenPlatformJsonReader.GetPropertyString(product, "hot_product_commission_rate"),
            Discount: OpenPlatformJsonReader.GetPropertyString(product, "discount"),
            EvaluateRate: OpenPlatformJsonReader.GetPropertyString(product, "evaluate_rate"),
            LatestVolume: OpenPlatformText.FirstNonEmpty(
                OpenPlatformJsonReader.GetPropertyString(product, "lastest_volume"),
                OpenPlatformJsonReader.GetPropertyString(product, "latest_volume")),
            FirstLevelCategoryId: OpenPlatformJsonReader.GetPropertyString(product, "first_level_category_id"),
            FirstLevelCategoryName: OpenPlatformJsonReader.GetPropertyString(product, "first_level_category_name"),
            SecondLevelCategoryId: OpenPlatformJsonReader.GetPropertyString(product, "second_level_category_id"),
            SecondLevelCategoryName: OpenPlatformJsonReader.GetPropertyString(product, "second_level_category_name"),
            ShopId: OpenPlatformJsonReader.GetPropertyString(product, "shop_id"),
            ShopUrl: OpenPlatformJsonReader.GetPropertyString(product, "shop_url"),
            PlatformProductType: OpenPlatformJsonReader.GetPropertyString(product, "platform_product_type"),
            RawJson: product.GetRawText());
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

        if (OpenPlatformJsonReader.TryGetProperty(productsElement, "product", out var productElement))
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
}
