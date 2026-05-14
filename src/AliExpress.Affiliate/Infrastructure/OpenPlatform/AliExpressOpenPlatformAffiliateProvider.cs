using AliExpress.Affiliate.Application.Ports;
using AliExpress.Affiliate.Domain;

namespace AliExpress.Affiliate.Infrastructure.OpenPlatform;

internal sealed class AliExpressOpenPlatformAffiliateProvider : IAliExpressAffiliateProvider
{
    private const string ProductQueryMethod = "aliexpress.affiliate.product.query";
    private const string HotProductQueryMethod = "aliexpress.affiliate.hotproduct.query";
    private const string HotProductDownloadMethod = "aliexpress.affiliate.hotproduct.download";
    private const string CategoryGetMethod = "aliexpress.affiliate.category.get";
    private const string FeaturedPromoGetMethod = "aliexpress.affiliate.featuredpromo.get";
    private const string FeaturedPromoProductsGetMethod = "aliexpress.affiliate.featuredpromo.products.get";
    private const string SmartMatchMethod = "aliexpress.affiliate.product.smartmatch";
    private const string OrderListMethod = "aliexpress.affiliate.order.list";
    private const string OrderGetMethod = "aliexpress.affiliate.order.get";
    private const string OrderListByIndexMethod = "aliexpress.affiliate.order.listbyindex";

    private readonly IAliExpressOpenPlatformGateway _gateway;

    public AliExpressOpenPlatformAffiliateProvider(IAliExpressOpenPlatformGateway gateway)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
    }

    public async Task<AffiliateLinkLookup> GenerateAffiliateLinkAsync(
        string sourceUrl,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var request = AliExpressOpenPlatformRequestFactory.BuildLinkGenerateRequest(sourceUrl, options, timestamp);
        var responseBody = await _gateway.SendAsync(request, cancellationToken);
        var affiliateUrl = AliExpressOpenPlatformResponseParser.ExtractAffiliateUrl(responseBody);
        var missingLinkSummary = string.IsNullOrWhiteSpace(affiliateUrl)
            ? AliExpressOpenPlatformResponseParser.SummarizeLinkGenerateResponse(responseBody)
            : string.Empty;

        return new AffiliateLinkLookup(affiliateUrl, missingLinkSummary);
    }

    public async Task<AliExpressProductDetails?> GetProductDetailsAsync(
        AliExpressProductId productId,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var request = AliExpressOpenPlatformRequestFactory.BuildProductDetailRequest(
            productId.Value,
            options,
            timestamp);
        var responseBody = await _gateway.SendAsync(request, cancellationToken);

        return AliExpressOpenPlatformResponseParser.ExtractProductDetails(responseBody);
    }

    public async Task<IReadOnlyList<AliExpressAffiliateLink>> GenerateAffiliateLinksAsync(
        IReadOnlyList<string> sourceUrls,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var request = AliExpressOpenPlatformRequestFactory.BuildApiRequest(
            "aliexpress.affiliate.link.generate",
            options,
            timestamp,
            new Dictionary<string, string>
            {
                ["promotion_link_type"] = FirstNonEmpty(options.PromotionLinkType, AliExpressAffiliateOptions.DefaultPromotionLinkType),
                ["source_values"] = string.Join(",", sourceUrls),
                ["tracking_id"] = options.TrackingId
            });
        var responseBody = await _gateway.SendAsync(request, cancellationToken);

        return AliExpressOpenPlatformResponseParser.ExtractAffiliateLinks(responseBody);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> SearchProductsAsync(
        AliExpressProductQuery query,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        return ExecuteProductQueryAsync(ProductQueryMethod, ToParameters(query, options), options, timestamp, cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetHotProductsAsync(
        AliExpressProductQuery query,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        return ExecuteProductQueryAsync(HotProductQueryMethod, ToParameters(query, options), options, timestamp, cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetHotProductDownloadAsync(
        AliExpressHotProductDownloadQuery query,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        return ExecuteProductQueryAsync(HotProductDownloadMethod, ToParameters(query, options), options, timestamp, cancellationToken);
    }

    public async Task<AliExpressAffiliateApiResult<AliExpressAffiliateCategory>> GetCategoriesAsync(
        string fields,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var responseBody = await ExecuteApiAsync(
            CategoryGetMethod,
            new Dictionary<string, string> { ["fields"] = fields },
            options,
            timestamp,
            cancellationToken);

        return AliExpressOpenPlatformResponseParser.ExtractCategories(responseBody);
    }

    public async Task<AliExpressAffiliateApiResult<AliExpressAffiliateFeaturedPromo>> GetFeaturedPromosAsync(
        string fields,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var responseBody = await ExecuteApiAsync(
            FeaturedPromoGetMethod,
            new Dictionary<string, string> { ["fields"] = fields },
            options,
            timestamp,
            cancellationToken);

        return AliExpressOpenPlatformResponseParser.ExtractFeaturedPromos(responseBody);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetFeaturedPromoProductsAsync(
        AliExpressFeaturedPromoProductsQuery query,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        return ExecuteProductQueryAsync(FeaturedPromoProductsGetMethod, ToParameters(query, options), options, timestamp, cancellationToken);
    }

    public Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> GetSmartMatchProductsAsync(
        AliExpressSmartMatchQuery query,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.DeviceId))
        {
            throw new ArgumentException("DeviceId is required for smart match queries.", nameof(query));
        }

        return ExecuteProductQueryAsync(SmartMatchMethod, ToParameters(query, options), options, timestamp, cancellationToken);
    }

    public async Task<AliExpressAffiliateApiResult<AliExpressAffiliateOrder>> GetOrdersAsync(
        AliExpressOrderListQuery query,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var responseBody = await ExecuteApiAsync(OrderListMethod, ToParameters(query), options, timestamp, cancellationToken);
        return AliExpressOpenPlatformResponseParser.ExtractOrders(responseBody);
    }

    public async Task<AliExpressAffiliateApiResult<AliExpressAffiliateOrder>> GetOrderDetailsAsync(
        AliExpressOrderDetailsQuery query,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var responseBody = await ExecuteApiAsync(OrderGetMethod, ToParameters(query), options, timestamp, cancellationToken);
        return AliExpressOpenPlatformResponseParser.ExtractOrders(responseBody);
    }

    public async Task<AliExpressAffiliateApiResult<AliExpressAffiliateOrder>> GetOrdersByIndexAsync(
        AliExpressOrderListByIndexQuery query,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var responseBody = await ExecuteApiAsync(OrderListByIndexMethod, ToParameters(query), options, timestamp, cancellationToken);
        return AliExpressOpenPlatformResponseParser.ExtractOrders(responseBody);
    }

    private async Task<AliExpressAffiliateApiResult<AliExpressAffiliateProduct>> ExecuteProductQueryAsync(
        string method,
        IReadOnlyDictionary<string, string> parameters,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var responseBody = await ExecuteApiAsync(method, parameters, options, timestamp, cancellationToken);
        return AliExpressOpenPlatformResponseParser.ExtractProducts(responseBody);
    }

    private async Task<string> ExecuteApiAsync(
        string method,
        IReadOnlyDictionary<string, string> parameters,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var request = AliExpressOpenPlatformRequestFactory.BuildApiRequest(method, options, timestamp, parameters);
        return await _gateway.SendAsync(request, cancellationToken);
    }

    private static Dictionary<string, string> ToParameters(
        AliExpressProductQuery query,
        AliExpressAffiliateOptions options)
    {
        return new Dictionary<string, string>
        {
            ["category_ids"] = query.CategoryIds,
            ["fields"] = query.Fields,
            ["keywords"] = query.Keywords,
            ["max_sale_price"] = query.MaxSalePrice,
            ["min_sale_price"] = query.MinSalePrice,
            ["page_no"] = query.PageNo.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["page_size"] = query.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["platform_product_type"] = query.PlatformProductType,
            ["sort"] = query.Sort,
            ["target_currency"] = FirstNonEmpty(query.TargetCurrency, options.TargetCurrency),
            ["target_language"] = FirstNonEmpty(query.TargetLanguage, options.TargetLanguage),
            ["tracking_id"] = FirstNonEmpty(query.TrackingId, options.TrackingId),
            ["ship_to_country"] = FirstNonEmpty(query.ShipToCountry, options.ShipToCountry),
            ["delivery_days"] = query.DeliveryDays
        };
    }

    private static Dictionary<string, string> ToParameters(
        AliExpressHotProductDownloadQuery query,
        AliExpressAffiliateOptions options)
    {
        return new Dictionary<string, string>
        {
            ["category_id"] = query.CategoryId,
            ["fields"] = query.Fields,
            ["locale_site"] = query.LocaleSite,
            ["page_no"] = query.PageNo.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["page_size"] = query.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["target_currency"] = FirstNonEmpty(query.TargetCurrency, options.TargetCurrency),
            ["target_language"] = FirstNonEmpty(query.TargetLanguage, options.TargetLanguage),
            ["tracking_id"] = FirstNonEmpty(query.TrackingId, options.TrackingId),
            ["country"] = FirstNonEmpty(query.Country, options.ShipToCountry)
        };
    }

    private static Dictionary<string, string> ToParameters(
        AliExpressFeaturedPromoProductsQuery query,
        AliExpressAffiliateOptions options)
    {
        return new Dictionary<string, string>
        {
            ["category_id"] = query.CategoryId,
            ["fields"] = query.Fields,
            ["page_no"] = query.PageNo.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["page_size"] = query.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["promotion_end_time"] = query.PromotionEndTime,
            ["promotion_name"] = query.PromotionName,
            ["promotion_start_time"] = query.PromotionStartTime,
            ["sort"] = query.Sort,
            ["target_currency"] = FirstNonEmpty(query.TargetCurrency, options.TargetCurrency),
            ["target_language"] = FirstNonEmpty(query.TargetLanguage, options.TargetLanguage),
            ["tracking_id"] = FirstNonEmpty(query.TrackingId, options.TrackingId),
            ["country"] = FirstNonEmpty(query.Country, options.ShipToCountry)
        };
    }

    private static Dictionary<string, string> ToParameters(
        AliExpressSmartMatchQuery query,
        AliExpressAffiliateOptions options)
    {
        return new Dictionary<string, string>
        {
            ["app"] = query.App,
            ["device"] = query.Device,
            ["device_id"] = query.DeviceId,
            ["fields"] = query.Fields,
            ["keywords"] = query.Keywords,
            ["product_id"] = query.ProductId,
            ["site"] = query.Site,
            ["target_currency"] = FirstNonEmpty(query.TargetCurrency, options.TargetCurrency),
            ["target_language"] = FirstNonEmpty(query.TargetLanguage, options.TargetLanguage),
            ["tracking_id"] = FirstNonEmpty(query.TrackingId, options.TrackingId),
            ["user"] = query.User,
            ["page_no"] = query.PageNo.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["country"] = FirstNonEmpty(query.Country, options.ShipToCountry)
        };
    }

    private static Dictionary<string, string> ToParameters(AliExpressOrderListQuery query)
    {
        return new Dictionary<string, string>
        {
            ["start_time"] = query.StartTime,
            ["end_time"] = query.EndTime,
            ["status"] = query.Status,
            ["locale_site"] = query.LocaleSite,
            ["page_no"] = query.PageNo.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["page_size"] = query.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["fields"] = query.Fields
        };
    }

    private static Dictionary<string, string> ToParameters(AliExpressOrderDetailsQuery query)
    {
        return new Dictionary<string, string>
        {
            ["order_ids"] = query.OrderIds,
            ["fields"] = query.Fields
        };
    }

    private static Dictionary<string, string> ToParameters(AliExpressOrderListByIndexQuery query)
    {
        return new Dictionary<string, string>
        {
            ["start_time"] = query.StartTime,
            ["end_time"] = query.EndTime,
            ["status"] = query.Status,
            ["time_type"] = query.TimeType,
            ["start_query_index_id"] = query.StartQueryIndexId,
            ["locale_site"] = query.LocaleSite,
            ["page_size"] = query.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["fields"] = query.Fields
        };
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }
}
