using AliExpress.Affiliate.OpenPlatform;
using AliExpress.Affiliate.Reports.Application.Requests;
using AliExpress.Affiliate.Reports.Configuration;
using AliExpress.Affiliate.Reports.Infrastructure.Signing;
using System.Globalization;

namespace AliExpress.Affiliate.Reports.Infrastructure;

/// <summary>
/// Builds signed TOP requests for the reporting endpoints.
/// </summary>
internal static class ReportsRequestFactory
{
    public const string OrderListMethod = "aliexpress.affiliate.order.list";
    public const string OrderGetMethod = "aliexpress.affiliate.order.get";
    public const string JsonFormat = "json";

    /// <summary>Maximum page size accepted by <c>aliexpress.affiliate.order.list</c>.</summary>
    public const int MaxPageSize = 50;

    /// <summary>Default page size applied when the caller passes a non-positive value.</summary>
    public const int DefaultPageSize = 50;

    /// <summary>
    /// Clamps a caller-supplied page size to the range accepted by AliExpress TOP.
    /// Non-positive values fall back to <see cref="DefaultPageSize"/>.
    /// </summary>
    public static int ClampPageSize(int requested)
    {
        if (requested <= 0)
        {
            return DefaultPageSize;
        }

        return requested > MaxPageSize ? MaxPageSize : requested;
    }

    public static AliExpressOpenPlatformRequest BuildOrderListRequest(
        ListConversionsRequest request,
        AliExpressAffiliateReportsOptions options,
        DateTimeOffset timestamp)
    {
        options.Validate();

        var parameters = BuildCommonParameters(OrderListMethod, options, timestamp);
        parameters["start_time"] = GmtPlus8Time.Format(request.From);
        parameters["end_time"] = GmtPlus8Time.Format(request.To);
        parameters["page_no"] = request.Page.ToString(CultureInfo.InvariantCulture);
        parameters["page_size"] = ClampPageSize(request.PageSize).ToString(CultureInfo.InvariantCulture);

        // AliExpress TOP requires `status` for aliexpress.affiliate.order.list — it has no
        // "all statuses" wildcard. See OrderStatusMap.ToTopStatusString for the All/null fallback.
        parameters["status"] = OrderStatusMap.ToTopStatusString(request.Status);

        AddIfNotEmpty(parameters, "order_ids", request.OrderId);
        AddIfNotEmpty(parameters, "tracking_id", request.TrackingId ?? options.TrackingId);

        return Finalize(parameters, options);
    }

    public static AliExpressOpenPlatformRequest BuildOrderGetRequest(
        string orderId,
        AliExpressAffiliateReportsOptions options,
        DateTimeOffset timestamp)
    {
        options.Validate();

        if (string.IsNullOrWhiteSpace(orderId))
        {
            throw new ArgumentException("Order id is required.", nameof(orderId));
        }

        var parameters = BuildCommonParameters(OrderGetMethod, options, timestamp);
        parameters["order_ids"] = orderId.Trim();
        AddIfNotEmpty(parameters, "tracking_id", options.TrackingId);

        return Finalize(parameters, options);
    }

    private static SortedDictionary<string, string> BuildCommonParameters(
        string method,
        AliExpressAffiliateReportsOptions options,
        DateTimeOffset timestamp)
    {
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["method"] = method,
            ["app_key"] = options.AppKey,
            ["timestamp"] = timestamp.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
            ["format"] = JsonFormat,
            ["sign_method"] = TopSignatureBuilder.Normalize(options.SignMethod),
            ["v"] = string.IsNullOrWhiteSpace(options.ApiVersion)
                ? AliExpressAffiliateReportsOptions.DefaultApiVersion
                : options.ApiVersion
        };

        if (!string.IsNullOrWhiteSpace(options.AccessToken))
        {
            parameters["session"] = options.AccessToken!;
        }

        return parameters;
    }

    private static AliExpressOpenPlatformRequest Finalize(
        SortedDictionary<string, string> parameters,
        AliExpressAffiliateReportsOptions options)
    {
        parameters["sign"] = TopSignatureBuilder.Build(parameters, options.AppSecret, parameters["sign_method"]);

        var endpoint = string.IsNullOrWhiteSpace(options.Endpoint)
            ? AliExpressAffiliateReportsOptions.DefaultEndpoint
            : options.Endpoint;
        var requestUri = new UriBuilder(new Uri(endpoint, UriKind.Absolute))
        {
            Query = string.Empty
        }.Uri;

        return new AliExpressOpenPlatformRequest(requestUri, parameters, parameters);
    }

    private static void AddIfNotEmpty(IDictionary<string, string> parameters, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parameters[key] = value!.Trim();
        }
    }

}
