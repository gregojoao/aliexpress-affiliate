using AliExpress.Affiliate.Application.Ports;
using AliExpress.Affiliate.Application.Requests;
using AliExpress.Affiliate.Configuration;
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
        AliExpressAffiliateLinkRequest request,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var openPlatformRequest = AliExpressOpenPlatformRequestFactory.BuildLinkGenerateRequest(
            sourceUrl,
            request,
            options,
            timestamp);
        var responseBody = await _gateway.SendAsync(openPlatformRequest, cancellationToken);
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
        AliExpressAffiliateLinksRequest request,
        AliExpressAffiliateOptions options,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var openPlatformRequest = AliExpressOpenPlatformRequestFactory.BuildApiRequest(
            "aliexpress.affiliate.link.generate",
            options,
            timestamp,
            new Dictionary<string, string>
            {
                ["promotion_link_type"] = OpenPlatformText.FirstNonEmpty(request.PromotionLinkType, options.DefaultPromotionLinkType, AliExpressAffiliateOptions.FallbackPromotionLinkType),
                ["source_values"] = string.Join(",", sourceUrls),
                ["tracking_id"] = OpenPlatformText.FirstNonEmpty(request.TrackingId, options.DefaultTrackingId)
            });
        var responseBody = await _gateway.SendAsync(openPlatformRequest, cancellationToken);

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
        return new OpenPlatformParameterBuilder()
            .Add("category_ids", query.CategoryIds)
            .Add("fields", query.Fields)
            .Add("keywords", query.Keywords)
            .Add("max_sale_price", query.MaxSalePrice)
            .Add("min_sale_price", query.MinSalePrice)
            .Add("page_no", query.PageNumber)
            .Add("page_size", query.PageSize)
            .Add("platform_product_type", query.PlatformProductType)
            .Add("sort", query.Sort)
            .AddTargeting(query.TargetCurrency, options.DefaultTargetCurrency, query.TargetLanguage, options.DefaultTargetLanguage)
            .AddTracking(query.TrackingId, options)
            .AddCountry("ship_to_country", query.ShipToCountryCode, options)
            .Add("delivery_days", query.DeliveryDays)
            .Build();
    }

    private static Dictionary<string, string> ToParameters(
        AliExpressHotProductDownloadQuery query,
        AliExpressAffiliateOptions options)
    {
        return new OpenPlatformParameterBuilder()
            .Add("category_id", query.CategoryId)
            .Add("fields", query.Fields)
            .Add("locale_site", query.Locale)
            .Add("page_no", query.PageNumber)
            .Add("page_size", query.PageSize)
            .AddTargeting(query.TargetCurrency, options.DefaultTargetCurrency, query.TargetLanguage, options.DefaultTargetLanguage)
            .AddTracking(query.TrackingId, options)
            .AddCountry("country", query.CountryCode, options)
            .Build();
    }

    private static Dictionary<string, string> ToParameters(
        AliExpressFeaturedPromoProductsQuery query,
        AliExpressAffiliateOptions options)
    {
        return new OpenPlatformParameterBuilder()
            .Add("category_id", query.CategoryId)
            .Add("fields", query.Fields)
            .Add("page_no", query.PageNumber)
            .Add("page_size", query.PageSize)
            .Add("promotion_end_time", query.PromotionEndTime)
            .Add("promotion_name", query.PromotionName)
            .Add("promotion_start_time", query.PromotionStartTime)
            .Add("sort", query.Sort)
            .AddTargeting(query.TargetCurrency, options.DefaultTargetCurrency, query.TargetLanguage, options.DefaultTargetLanguage)
            .AddTracking(query.TrackingId, options)
            .AddCountry("country", query.CountryCode, options)
            .Build();
    }

    private static Dictionary<string, string> ToParameters(
        AliExpressSmartMatchQuery query,
        AliExpressAffiliateOptions options)
    {
        return new OpenPlatformParameterBuilder()
            .Add("app", query.App)
            .Add("device", query.Device)
            .Add("device_id", query.DeviceId)
            .Add("fields", query.Fields)
            .Add("keywords", query.Keywords)
            .Add("product_id", query.ProductId)
            .Add("site", query.Site)
            .AddTargeting(query.TargetCurrency, options.DefaultTargetCurrency, query.TargetLanguage, options.DefaultTargetLanguage)
            .AddTracking(query.TrackingId, options)
            .Add("user", query.User)
            .Add("page_no", query.PageNumber)
            .AddCountry("country", query.CountryCode, options)
            .Build();
    }

    private static Dictionary<string, string> ToParameters(AliExpressOrderListQuery query)
    {
        return new OpenPlatformParameterBuilder()
            .Add("start_time", query.StartTime)
            .Add("end_time", query.EndTime)
            .Add("status", query.Status)
            .Add("locale_site", query.Locale)
            .Add("page_no", query.PageNumber)
            .Add("page_size", query.PageSize)
            .Add("fields", query.Fields)
            .Build();
    }

    private static Dictionary<string, string> ToParameters(AliExpressOrderDetailsQuery query)
    {
        return new OpenPlatformParameterBuilder()
            .Add("order_ids", string.Join(",", query.OrderIds.Where(orderId => !string.IsNullOrWhiteSpace(orderId)).Select(orderId => orderId.Trim())))
            .Add("fields", query.Fields)
            .Build();
    }

    private static Dictionary<string, string> ToParameters(AliExpressOrderListByIndexQuery query)
    {
        return new OpenPlatformParameterBuilder()
            .Add("start_time", query.StartTime)
            .Add("end_time", query.EndTime)
            .Add("status", query.Status)
            .Add("time_type", query.TimeType)
            .Add("start_query_index_id", query.StartQueryIndexId)
            .Add("locale_site", query.Locale)
            .Add("page_size", query.PageSize)
            .Add("fields", query.Fields)
            .Build();
    }
}
