using AliExpress.Affiliate.Domain;
using System.Text.Json;

namespace AliExpress.Affiliate.Infrastructure.OpenPlatform;

internal static class AliExpressCategoryResponseMapper
{
    public static AliExpressAffiliateApiResult<AliExpressAffiliateCategory> ExtractCategories(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var result = OpenPlatformResponseEnvelope.ExtractResult(document.RootElement);

        var categories = OpenPlatformJsonReader.TryGetProperty(result, "categories", out var categoriesElement)
            ? OpenPlatformJsonReader.EnumerateItems(categoriesElement, "category")
                .Select(category => new AliExpressAffiliateCategory(
                    CategoryId: OpenPlatformJsonReader.GetPropertyString(category, "category_id"),
                    CategoryName: OpenPlatformJsonReader.GetPropertyString(category, "category_name"),
                    ParentCategoryId: OpenPlatformJsonReader.GetPropertyString(category, "parent_category_id"),
                    RawJson: category.GetRawText()))
                .ToArray()
            : Array.Empty<AliExpressAffiliateCategory>();

        return OpenPlatformApiResultFactory.Create(responseBody, result, categories);
    }
}
