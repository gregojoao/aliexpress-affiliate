using AliExpress.Affiliate.Domain;
using System.Text.Json;

namespace AliExpress.Affiliate.Infrastructure.OpenPlatform;

internal static class AliExpressFeaturedPromoResponseMapper
{
    public static AliExpressAffiliateApiResult<AliExpressAffiliateFeaturedPromo> ExtractFeaturedPromos(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var result = OpenPlatformResponseEnvelope.ExtractResult(document.RootElement);

        var promos = OpenPlatformJsonReader.TryGetProperty(result, "promos", out var promosElement)
            ? OpenPlatformJsonReader.EnumerateItems(promosElement, "promo")
                .Select(promo => new AliExpressAffiliateFeaturedPromo(
                    PromoName: OpenPlatformJsonReader.GetPropertyString(promo, "promo_name"),
                    PromoDescription: OpenPlatformJsonReader.GetPropertyString(promo, "promo_desc"),
                    ProductCount: OpenPlatformJsonReader.GetPropertyString(promo, "product_num"),
                    RawJson: promo.GetRawText()))
                .ToArray()
            : Array.Empty<AliExpressAffiliateFeaturedPromo>();

        return OpenPlatformApiResultFactory.Create(responseBody, result, promos);
    }
}
